using Taskmanagement_API.Models;
using Taskmanagement_API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Taskmanagement_API.Data
{
    public class MM_Daily_StatusDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_Daily_StatusDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // GetDailyStatusByDate
        public async Task<List<MM_Daily_StatusDto>> GetDailyStatusByDate(string date, decimal employeeId)
        {
            var Tasks = new List<MM_Daily_StatusDto>();

            try
            {
                const string query = @"
                  SELECT Status_ID, Employee_ID, Date, In_Time, Out_Time, Status, Remark, Is_Working, Inserted_Host, Inserted_User_ID, Updated_Host, Updated_User_ID, Plant_Code
                    FROM MM_Daily_Status
                    WHERE  Employee_ID = @Employee_ID AND CAST(Date AS DATE) = @Date";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Employee_ID", employeeId);
                command.Parameters.AddWithValue("@Date", date);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var inTimeOrdinal = reader.GetOrdinal("In_Time");
                    var outTimeOrdinal = reader.GetOrdinal("Out_Time");
                    var statusOrdinal = reader.GetOrdinal("Status");
                    var remarkOrdinal = reader.GetOrdinal("Remark");
                    var isWorkingOrdinal = reader.GetOrdinal("Is_Working");
                    var insertedHostOrdinal = reader.GetOrdinal("Inserted_Host");
                    var insertedUserIdOrdinal = reader.GetOrdinal("Inserted_User_ID");
                    var updatedHostOrdinal = reader.GetOrdinal("Updated_Host");
                    var updatedUserIdOrdinal = reader.GetOrdinal("Updated_User_ID");
                    var plantCodeOrdinal = reader.GetOrdinal("Plant_Code");

                    Tasks.Add(new MM_Daily_StatusDto
                    {
                        Status_ID = reader.GetDecimal("Status_ID"),
                        Employee_ID = reader.GetDecimal("Employee_ID"),
                        Date = reader.GetDateTime("Date").ToString("yyyy-MM-dd"),
                        In_Time = reader.IsDBNull(inTimeOrdinal) ? string.Empty : reader.GetDateTime(inTimeOrdinal).ToString(),
                        Out_Time = reader.IsDBNull(outTimeOrdinal) ? string.Empty : reader.GetDateTime(outTimeOrdinal).ToString(),
                        Status = reader.IsDBNull(statusOrdinal) ? string.Empty : reader.GetString(statusOrdinal),
                        Remark = reader.IsDBNull(remarkOrdinal) ? string.Empty : reader.GetString(remarkOrdinal),
                        Is_Working = !reader.IsDBNull(isWorkingOrdinal) && reader.GetBoolean(isWorkingOrdinal),
                        Inserted_Host = reader.IsDBNull(insertedHostOrdinal) ? string.Empty : reader.GetString(insertedHostOrdinal),
                        Inserted_User_ID = reader.IsDBNull(insertedUserIdOrdinal) ? 0 : reader.GetDecimal(insertedUserIdOrdinal),
                        Updated_Host = reader.IsDBNull(updatedHostOrdinal) ? string.Empty : reader.GetString(updatedHostOrdinal),
                        Updated_User_ID = reader.IsDBNull(updatedUserIdOrdinal) ? 0 : reader.GetDecimal(updatedUserIdOrdinal),
                        Plant_Code = reader.IsDBNull(plantCodeOrdinal) ? string.Empty : reader.GetString(plantCodeOrdinal)
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return Tasks;
        }


        // CreateDailyStatus
        public async Task<MM_Daily_StatusDto> CreateDailyStatus(MM_Daily_StatusDto dailyStatus)
        {
            try
            {
                const string query = @"
                        INSERT INTO MM_Daily_Status (Employee_ID, Date, In_Time, Out_Time, Status, Remark, Is_Working, Inserted_Host, Inserted_User_ID,Inserted_Date, Plant_Code)
                        VALUES (@Employee_ID, @Date, @In_Time, @Out_Time, @Status, @Remark, @Is_Working, @Inserted_Host, @Inserted_User_ID, @Inserted_Date, @Plant_Code);
                        SELECT SCOPE_IDENTITY();";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Employee_ID", dailyStatus.Employee_ID);
                command.Parameters.AddWithValue("@Date", dailyStatus.Date);
                command.Parameters.AddWithValue("@In_Time", dailyStatus.In_Time);
                command.Parameters.AddWithValue("@Out_Time", dailyStatus.Out_Time);
                command.Parameters.AddWithValue("@Status", dailyStatus.Status);
                command.Parameters.AddWithValue("@Remark", dailyStatus.Remark);
                command.Parameters.AddWithValue("@Is_Working", dailyStatus.Is_Working);
                command.Parameters.AddWithValue("@Inserted_Host", dailyStatus.Inserted_Host);
                command.Parameters.AddWithValue("@Inserted_User_ID", dailyStatus.Inserted_User_ID);
                command.Parameters.AddWithValue("@Inserted_Date", DateTime.Now);
                command.Parameters.AddWithValue("@Plant_Code", dailyStatus.Plant_Code);

                var newId = await command.ExecuteScalarAsync();
                if (newId != null)
                {
                    dailyStatus.Status_ID = Convert.ToDecimal(newId);
                    return dailyStatus;
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

        // UpdateDailyStatus
        public async Task<MM_Daily_StatusDto> UpdateDailyStatus(MM_Daily_StatusDto dailyStatus)
        {
            try
            {
                const string query = @"
                        UPDATE MM_Daily_Status
                        SET Employee_ID = @Employee_ID,
                            Date = @Date,
                            In_Time = @In_Time,
                            Out_Time = @Out_Time,
                            Status = @Status,
                            Remark = @Remark,
                            Is_Working = @Is_Working,
                            Updated_Host = @Updated_Host,
                            Updated_User_ID = @Updated_User_ID,
                            Updated_Date = @Updated_Date,
                            Plant_Code = @Plant_Code
                        WHERE Status_ID = @Status_ID";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Employee_ID", dailyStatus.Employee_ID);
                command.Parameters.AddWithValue("@Date", dailyStatus.Date);
                command.Parameters.AddWithValue("@In_Time", dailyStatus.In_Time);
                command.Parameters.AddWithValue("@Out_Time", dailyStatus.Out_Time);
                command.Parameters.AddWithValue("@Status", dailyStatus.Status);
                command.Parameters.AddWithValue("@Remark", dailyStatus.Remark);
                command.Parameters.AddWithValue("@Is_Working", dailyStatus.Is_Working);
                command.Parameters.AddWithValue("@Updated_Host", dailyStatus.Updated_Host);
                command.Parameters.AddWithValue("@Updated_User_ID", dailyStatus.Updated_User_ID);
                command.Parameters.AddWithValue("@Updated_Date", DateTime.Now);
                command.Parameters.AddWithValue("@Plant_Code", dailyStatus.Plant_Code);
                command.Parameters.AddWithValue("@Status_ID", dailyStatus.Status_ID);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return dailyStatus;
                }
                else
                {
                    throw new Exception("No records were updated. Please check the provided Status ID.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}