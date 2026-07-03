using Taskmanagement_API.Models;
using Taskmanagement_API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Taskmanagement_API.Data
{
    public class MM_TaskDetailsService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;

        public MM_TaskDetailsService(IDbConnectionFactory connectionFactory, IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
        }

       

        public async Task<MM_Task_DetailsCreateDto> CreateTaskDetailAsync(MM_Task_DetailsCreateDto TaskDetailDto)
        {
            try
            {
                const string query = @"
                    INSERT INTO MM_Task_Details (Task_Description, Employee_ID, Due_Date, Priority,Status,  Inserted_Host, Plant_Code, Inserted_User_ID, Inserted_Date)
                    VALUES (@Description, @Employee_ID, @Due_Date, @Priority, @Status, @Inserted_Host, @Plant_Code, @Inserted_User_ID,getdate());
                    SELECT SCOPE_IDENTITY();";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Description", TaskDetailDto.Task_Description);
                command.Parameters.AddWithValue("@Employee_ID", TaskDetailDto.Employee_ID);
                command.Parameters.AddWithValue("@Due_Date", TaskDetailDto.Due_Date);
                command.Parameters.AddWithValue("@Priority", (object?)TaskDetailDto.Priority ?? DBNull.Value);
                command.Parameters.AddWithValue("@Status", (object?)TaskDetailDto.Status ?? DBNull.Value);

                command.Parameters.AddWithValue("@Inserted_Host", (object?)TaskDetailDto.Inserted_Host ?? DBNull.Value);
                command.Parameters.AddWithValue("@Plant_Code", (object?)TaskDetailDto.Plant_Code ?? DBNull.Value);
                command.Parameters.AddWithValue("@Inserted_User_ID", (object?)TaskDetailDto.Inserted_User_ID ?? DBNull.Value);

                var newId = await command.ExecuteScalarAsync();
                if (newId != null)
                {
                    TaskDetailDto.Task_ID = Convert.ToDecimal(newId);
                    return TaskDetailDto;
                }
                else
                {
                    throw new Exception("Failed to retrieve the new ID after inserting the Task detail.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<MM_Task_DetailsListDto>> GetTasksByEmployee(decimal employeeId, string? mode, decimal? teamId = null)
        {
            var Tasks = new List<MM_Task_DetailsListDto>();

            try
            {
                var managerModeValue = _configuration["Modes:manager"];
                var leadModeValue = _configuration["Modes:lead"];
                var teamFilter = teamId ?? 0; // 0 = all teams the manager belongs to

                var isManagerMode = string.Equals(mode, "manager", StringComparison.OrdinalIgnoreCase)
                                    || (!string.IsNullOrWhiteSpace(managerModeValue)
                                        && string.Equals(mode, managerModeValue, StringComparison.OrdinalIgnoreCase));

                var isLeadMode = string.Equals(mode, "lead", StringComparison.OrdinalIgnoreCase)
                                 || (!string.IsNullOrWhiteSpace(leadModeValue)
                                     && string.Equals(mode, leadModeValue, StringComparison.OrdinalIgnoreCase));

                string query = @"
                    SELECT
                        details.Due_Date,
                        details.Task_ID,
                        details.Employee_ID,
                        details.Task_Description,
                        details.Priority,
                        details.Status,
                        emp.Employee_Name as Responsibility,
                        details.Manager_Remark,
                        details.Remark_Updated_By,
                        ISNULL(rem.Employee_Name, '') AS Remark_Updated_By_Name,
                        details.Remark_Updated_Date
                    FROM MM_Task_Details details
                    INNER JOIN MM_Employee emp ON emp.Employee_ID = details.Employee_ID
                    LEFT JOIN MM_Employee rem ON rem.Employee_ID = details.Remark_Updated_By
                    WHERE details.Employee_ID = @Employee_ID
                    ORDER BY details.Due_Date ASC";

                if (isManagerMode)
                {
                    // Team-based visibility: tasks of every member of the manager's team(s).
                    // @Team_ID = 0 means all teams the manager belongs to; a specific
                    // @Team_ID narrows to that team. Self is always included.
                    query = @"
                    SELECT
                        details.Due_Date,
                        details.Task_ID,
                        details.Employee_ID,
                        details.Task_Description,
                        details.Priority,
                        details.Status,
                        emp.Employee_Name as Responsibility,
                        details.Manager_Remark,
                        details.Remark_Updated_By,
                        ISNULL(rem.Employee_Name, '') AS Remark_Updated_By_Name,
                        details.Remark_Updated_Date
                    FROM MM_Task_Details details
                    INNER JOIN MM_Employee emp ON emp.Employee_ID = details.Employee_ID
                    LEFT JOIN MM_Employee rem ON rem.Employee_ID = details.Remark_Updated_By
                    WHERE (emp.Is_Deleted IS NULL OR emp.Is_Deleted = 0)
                      AND (
                        details.Employee_ID = @Employee_ID
                        OR details.Employee_ID IN (
                            SELECT et.Employee_ID
                            FROM MM_Employee_Team et
                            WHERE et.Team_ID IN (
                                SELECT mt.Team_ID
                                FROM MM_Employee_Team mt
                                WHERE mt.Employee_ID = @Employee_ID
                                  AND (@Team_ID = 0 OR mt.Team_ID = @Team_ID)
                            )
                        )
                      )
                    ORDER BY details.Due_Date ASC";
                }
                else if (isLeadMode)
                {
                    // Self + employees whose Team_Lead_ID points to the lead.
                    query = @"
                    SELECT
                        details.Due_Date,
                        details.Task_ID,
                        details.Employee_ID,
                        details.Task_Description,
                        details.Priority,
                        details.Status,
                        emp.Employee_Name as Responsibility,
                        details.Manager_Remark,
                        details.Remark_Updated_By,
                        ISNULL(rem.Employee_Name, '') AS Remark_Updated_By_Name,
                        details.Remark_Updated_Date
                    FROM MM_Task_Details details
                    INNER JOIN MM_Employee emp ON emp.Employee_ID = details.Employee_ID
                    LEFT JOIN MM_Employee rem ON rem.Employee_ID = details.Remark_Updated_By
                    WHERE details.Employee_ID IN
                    (
                        SELECT e.Employee_ID
                        FROM MM_Employee e
                        WHERE (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
                          AND (e.Employee_ID = @Employee_ID OR e.Team_Lead_ID = @Employee_ID)
                    )
                    ORDER BY details.Due_Date ASC";
                }

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Employee_ID", employeeId);
                if (isManagerMode)
                {
                    command.Parameters.AddWithValue("@Team_ID", teamFilter);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Tasks.Add(new MM_Task_DetailsListDto
                    {
                        Task_ID = reader.GetDecimal("Task_ID"),
                        Employee_ID = reader.IsDBNull("Employee_ID") ? 0 : reader.GetDecimal("Employee_ID"),
                        Task_Description = reader.GetString("Task_Description"),
                        Due_Date = reader.GetDateTime("Due_Date").ToString("yyyy-MM-dd"),
                        Priority = reader.GetString("Priority"),
                        Status = reader.GetString("Status"),
                        Responsibility = reader.GetString("Responsibility"),
                        Manager_Remark = reader.IsDBNull("Manager_Remark") ? null : reader.GetString("Manager_Remark"),
                        Remark_Updated_By = reader.IsDBNull("Remark_Updated_By") ? null : reader.GetDecimal("Remark_Updated_By"),
                        Remark_Updated_By_Name = reader.GetString("Remark_Updated_By_Name"),
                        Remark_Updated_Date = reader.IsDBNull("Remark_Updated_Date")
                            ? null
                            : reader.GetDateTime("Remark_Updated_Date").ToString("yyyy-MM-ddTHH:mm:ss"),
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return Tasks;
        }

        public async Task<bool> DeleteTaskDetailAsync(decimal TaskId)
        {
            try
            {
                const string query = "DELETE FROM MM_Task_Details WHERE Task_ID = @Task_ID";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Task_ID", TaskId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        

        /// <summary>
        /// Updates the Manager_Remark for a task. Throws UnauthorizedAccessException when
        /// the supplied Updated_User_ID is not a manager (Designation_ID = 1) so the UI's
        /// manager-only affordance is also enforced on the server side.
        /// </summary>
        public async Task<bool> UpdateManagerRemarkAsync(decimal taskId, MM_Task_RemarkUpdateDto dto)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // Permission gate: caller must be a manager.
                const string designationQuery = @"
                    SELECT Designation_ID
                    FROM MM_Employee
                    WHERE Employee_ID = @Employee_ID
                      AND (Is_Deleted IS NULL OR Is_Deleted = 0)";

                decimal callerDesignation = 0;
                using (var designationCommand = new SqlCommand(designationQuery, connection))
                {
                    designationCommand.Parameters.AddWithValue("@Employee_ID", dto.Updated_User_ID);
                    var result = await designationCommand.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        callerDesignation = Convert.ToDecimal(result);
                    }
                }

                if (callerDesignation != 1m)
                {
                    throw new UnauthorizedAccessException("Only managers can update the manager remark.");
                }

                const string updateQuery = @"
                    UPDATE MM_Task_Details
                    SET Manager_Remark      = @Manager_Remark,
                        Remark_Updated_By   = @Remark_Updated_By,
                        Remark_Updated_Date = GETDATE(),
                        Updated_Host        = @Updated_Host,
                        Updated_User_ID     = @Updated_User_ID,
                        Updated_Date        = GETDATE()
                    WHERE Task_ID = @Task_ID";

                using var command = new SqlCommand(updateQuery, connection);
                var remark = string.IsNullOrWhiteSpace(dto.Manager_Remark) ? (object)DBNull.Value : dto.Manager_Remark.Trim();
                command.Parameters.AddWithValue("@Manager_Remark", remark);
                command.Parameters.AddWithValue("@Remark_Updated_By",
                    remark is string ? (object)dto.Updated_User_ID : DBNull.Value);
                command.Parameters.AddWithValue("@Updated_Host", (object?)dto.Updated_Host ?? DBNull.Value);
                command.Parameters.AddWithValue("@Updated_User_ID", dto.Updated_User_ID);
                command.Parameters.AddWithValue("@Task_ID", taskId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<MM_Task_DetailsUpdateDto> UpdateTaskDetailAsync(MM_Task_DetailsUpdateDto TaskDetailDto)
        {
            try
            {
                const string query = @"
                    UPDATE MM_Task_Details
                    SET 
                        Task_Description = @Task_Description,
                        Employee_ID = @Employee_ID,
                        Due_Date = @Due_Date,
                        Priority = @Priority,
                        Status = @Status,
                        Updated_Host = @Updated_Host,
                        Updated_User_ID = @Updated_User_ID,
                        Updated_Date = getdate()
                    WHERE Task_ID = @Task_ID";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Task_Description", TaskDetailDto.Task_Description);
                command.Parameters.AddWithValue("@Description", TaskDetailDto.Task_Description);
                command.Parameters.AddWithValue("@Employee_ID", TaskDetailDto.Employee_ID);
                command.Parameters.AddWithValue("@Due_Date", TaskDetailDto.Due_Date);
                command.Parameters.AddWithValue("@Priority", TaskDetailDto.Priority);
                command.Parameters.AddWithValue("@Status", TaskDetailDto.Status);
                command.Parameters.AddWithValue("@Updated_Host", (object?)TaskDetailDto.Updated_Host ?? DBNull.Value);
                command.Parameters.AddWithValue("@Updated_User_ID", (object?)TaskDetailDto.Updated_User_ID ?? DBNull.Value);
                command.Parameters.AddWithValue("@Task_ID", TaskDetailDto.Task_ID);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return TaskDetailDto;
                }
                else
                {
                    throw new Exception("No records were updated. Please check the provided Task ID.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}