using Taskmanagement_API.Models;
using Taskmanagement_API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Taskmanagement_API.Data
{
    public class MM_EmployeeDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_EmployeeDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<MM_EmployeeListDto>> GetAllEmployeesAsync()
        {
            var employees = new List<MM_EmployeeListDto>();

            try
            {
                const string query = @"
                    SELECT
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No,
                        ISNULL(e.Email_Address, '') as Email_Address,
                        e.Designation_ID,
                        ISNULL(d.Designation_Name, '') as Designation_Name,
                        e.Reporting_Manager_ID,
                        ISNULL(rm.Employee_Name, '') as Reporting_Manager_Name,
                        e.Team_Lead_ID,
                        ISNULL(tl.Employee_Name, '') as Team_Lead_Name,
                        e.Audit_Type_Id,
                        e.Plant_ID
                    FROM MM_Employee e
                    LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                    LEFT JOIN MM_Employee rm ON e.Reporting_Manager_ID = rm.Employee_ID
                    LEFT JOIN MM_Employee tl ON e.Team_Lead_ID = tl.Employee_ID
                    WHERE e.Is_Deleted IS NULL OR e.Is_Deleted = 0
                    ORDER BY e.Employee_Name";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    employees.Add(new MM_EmployeeListDto
                    {
                        Employee_ID = reader.GetDecimal("Employee_ID"),
                        Employee_Name = reader.GetString("Employee_Name"),
                        Employee_No = reader.GetString("Employee_No"),
                        Email_Address = reader.GetString("Email_Address"),
                        Designation_ID = reader.GetDecimal("Designation_ID"),
                        Designation_Name = reader.GetString("Designation_Name"),
                        Reporting_Manager_ID = reader.IsDBNull("Reporting_Manager_ID") ? null : reader.GetDecimal("Reporting_Manager_ID"),
                        Reporting_Manager_Name = reader.GetString("Reporting_Manager_Name"),
                        Team_Lead_ID = reader.IsDBNull("Team_Lead_ID") ? null : reader.GetDecimal("Team_Lead_ID"),
                        Team_Lead_Name = reader.GetString("Team_Lead_Name"),
                        Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                        Plant_ID = reader.GetDecimal("Plant_ID")
                    });
                }

                reader.Close();
                await AttachTeamsAsync(connection, employees);
            }
            catch (Exception)
            {
                throw;
            }

            return employees;
        }

        // Fills Team_IDs / Team_Names on every employee row from MM_Employee_Team.
        private static async Task AttachTeamsAsync(SqlConnection connection, List<MM_EmployeeListDto> employees)
        {
            if (employees.Count == 0)
            {
                return;
            }

            const string query = @"
                SELECT et.Employee_ID, et.Team_ID, t.Team_Name
                FROM MM_Employee_Team et
                INNER JOIN MM_Team_List t ON t.Team_ID = et.Team_ID
                ORDER BY et.Employee_ID, t.SORTORDER";

            var teamsByEmployee = new Dictionary<decimal, List<MM_TeamDto>>();

            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var employeeId = reader.GetDecimal("Employee_ID");
                    if (!teamsByEmployee.TryGetValue(employeeId, out var teams))
                    {
                        teams = new List<MM_TeamDto>();
                        teamsByEmployee[employeeId] = teams;
                    }

                    teams.Add(new MM_TeamDto
                    {
                        Team_ID = reader.GetDecimal("Team_ID"),
                        Team_Name = reader.GetString("Team_Name")
                    });
                }
            }

            foreach (var employee in employees)
            {
                if (teamsByEmployee.TryGetValue(employee.Employee_ID, out var teams))
                {
                    employee.Team_IDs = teams.Select(t => t.Team_ID).ToList();
                    employee.Team_Names = string.Join(", ", teams.Select(t => t.Team_Name));
                }
            }
        }

        public async Task<List<MM_TeamDto>> GetAllTeamsAsync()
        {
            var teams = new List<MM_TeamDto>();

            const string query = @"
                SELECT Team_ID, Team_Name
                FROM MM_Team_List
                ORDER BY SORTORDER";

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    teams.Add(new MM_TeamDto
                    {
                        Team_ID = reader.GetDecimal("Team_ID"),
                        Team_Name = reader.GetString("Team_Name")
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return teams;
        }

        public async Task<MM_EmployeeDetailDto?> GetEmployeeDetailsAsync(decimal employeeId, string empno)
        {
            MM_EmployeeDetailDto? employee = null;

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // First query: Get employee basic data
                string employeeQuery;
                if (!string.IsNullOrEmpty(empno) && empno.Length > 2)
                {
                    employeeQuery = @"
                SELECT 
                    e.Employee_ID,
                    e.Employee_Name,
                    e.Employee_No,
                    ISNULL(e.Email_Address, '') as Email_Address,
                    e.Designation_ID,
                    ISNULL(d.Designation_Name, '') as Designation_Name,
                    e.Reporting_Manager_ID,
                    e.Audit_Type_Id,
                    e.Plant_ID,
                    p.Plant_Code
                FROM MM_Employee e
                LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                LEFT JOIN MM_Plant p ON e.Plant_ID = p.Plant_ID
                WHERE e.Employee_No = @Employee_No AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";
                }
                else
                {
                    employeeQuery = @"
                SELECT 
                    e.Employee_ID,
                    e.Employee_Name,
                    e.Employee_No,
                    ISNULL(e.Email_Address, '') as Email_Address,
                    e.Designation_ID,
                    ISNULL(d.Designation_Name, '') as Designation_Name,
                    e.Reporting_Manager_ID,
                    e.Audit_Type_Id,
                    e.Plant_ID,
                     p.Plant_Code
                FROM MM_Employee e
                LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                LEFT JOIN MM_Plant p ON e.Plant_ID = p.Plant_ID
                WHERE e.Employee_ID = @Employee_ID AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";
                }

                using (var command = new SqlCommand(employeeQuery, connection))
                {
                    if (!string.IsNullOrEmpty(empno) && empno.Length > 2)
                    {
                        command.Parameters.Add(new SqlParameter("@Employee_No", empno));
                    }
                    else
                    {
                        command.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                    }
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        employee = new MM_EmployeeDetailDto
                        {
                            Employee_ID = reader.GetDecimal("Employee_ID"),
                            Employee_Name = reader.GetString("Employee_Name"),
                            Employee_No = reader.GetString("Employee_No"),
                            Email_Address = reader.GetString("Email_Address"),
                            Designation_ID = reader.GetDecimal("Designation_ID"),
                            Designation_Name = reader.GetString("Designation_Name"),
                            Reporting_Manager_ID = reader.IsDBNull("Reporting_Manager_ID") ? null : reader.GetDecimal("Reporting_Manager_ID"),
                            Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                            Plant_ID = reader.GetDecimal("Plant_ID"),
                            Plant_Code = reader.IsDBNull("Plant_Code") ? string.Empty : reader.GetString("Plant_Code"),
                            Shop_ID = new List<int>(),
                            Model_ID = new List<int>(),
                            Hostname = ""
                        };
                    }
                }

                // Teams the employee belongs to (drives the team switcher in the UI header).
                if (employee != null)
                {
                    const string teamsQuery = @"
                        SELECT t.Team_ID, t.Team_Name
                        FROM MM_Employee_Team et
                        INNER JOIN MM_Team_List t ON t.Team_ID = et.Team_ID
                        WHERE et.Employee_ID = @Employee_ID
                        ORDER BY t.SORTORDER";

                    using var teamsCommand = new SqlCommand(teamsQuery, connection);
                    teamsCommand.Parameters.Add(new SqlParameter("@Employee_ID", employee.Employee_ID));
                    using var teamsReader = await teamsCommand.ExecuteReaderAsync();

                    while (await teamsReader.ReadAsync())
                    {
                        employee.Teams.Add(new MM_TeamDto
                        {
                            Team_ID = teamsReader.GetDecimal("Team_ID"),
                            Team_Name = teamsReader.GetString("Team_Name")
                        });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return employee;
        }

        public async Task<List<object>> GetUserAuthenticationAsync(string employeeNo, decimal plantId, decimal auditTypeId)
        {
            var result = new List<object>();

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                const string query = @"
                    SELECT DISTINCT
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No
                    FROM MM_Employee e
                    WHERE e.Employee_No = @EmployeeNo";

                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@EmployeeNo", employeeNo));
                command.Parameters.Add(new SqlParameter("@PlantId", plantId));
                command.Parameters.Add(new SqlParameter("@AuditTypeId", auditTypeId));

                using var reader = await command.ExecuteReaderAsync();
                var tempResults = new List<dynamic>();

                while (await reader.ReadAsync())
                {
                    var item = new
                    {
                       
                        Employee_ID = reader.GetDecimal("Employee_ID"),
                        Employee_Name = reader.GetString("Employee_Name"),
                        Employee_No = reader.GetString("Employee_No")
                    };
                    tempResults.Add(item);
                }

                
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        public async Task<bool> CreateUser(MM_EmployeeCreateUserDto[] emplist)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    const string query = @"INSERT INTO MM_Employee
                        (Employee_Name, Employee_No, Email_Address, Designation_ID, Reporting_Manager_ID, Team_Lead_ID,
                         Audit_Type_Id, Plant_ID, Plant_Code, Inserted_Host, Inserted_User_ID, Inserted_Date)
                        OUTPUT INSERTED.Employee_ID
                        VALUES
                        (@Employee_Name, @Employee_No, @Email_Address, @Designation_ID, @Reporting_Manager_ID, @Team_Lead_ID,
                         @Audit_Type_Id, @Plant_ID, @Plant_Code, @Inserted_Host, @Inserted_User_ID, @Inserted_Date)";

                    using var command = new SqlCommand(query, connection, transaction);

                    // Add parameters
                    command.Parameters.Add(new SqlParameter("@Employee_Name", emplist[0].Employee_Name));
                    command.Parameters.Add(new SqlParameter("@Employee_No", emplist[0].Employee_No));
                    command.Parameters.Add(new SqlParameter("@Email_Address", emplist[0].Email_Address));
                    command.Parameters.Add(new SqlParameter("@Designation_ID", emplist[0].Designation_ID));
                    command.Parameters.Add(new SqlParameter("@Reporting_Manager_ID", emplist[0].Reporting_Manager_ID));
                    var teamLeadId = emplist[0].Team_Lead_ID;
                    command.Parameters.Add(new SqlParameter("@Team_Lead_ID",
                        teamLeadId.HasValue && teamLeadId.Value > 0 ? (object)teamLeadId.Value : DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Audit_Type_Id", emplist[0].Audit_Type_Id));
                    command.Parameters.Add(new SqlParameter("@Plant_ID", emplist[0].Plant_ID));
                    command.Parameters.Add(new SqlParameter("@Plant_Code", emplist[0].Plant_Code));
                    command.Parameters.Add(new SqlParameter("@Inserted_Host", emplist[0].Inserted_Host));
                    command.Parameters.Add(new SqlParameter("@Inserted_User_ID", emplist[0].Inserted_User_ID));
                    command.Parameters.Add(new SqlParameter("@Inserted_Date", DateTime.Now));

                    // Execute query
                    var empid = 0;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            empid = (int)reader.GetDecimal("Employee_ID");
                        }
                    } // Reader is properly closed here

                    // Add employee shop and model
                    const string shopmodelquery = @"INSERT INTO MM_User_Shop_Model 
                        (Employee_ID, Shop_ID, Model_ID, Plant_ID, Plant_Code,
                         Inserted_Host, Inserted_User_ID, Inserted_Date)
                        VALUES 
                        (@Employee_ID, @Shop_ID, @Model_ID, @Plant_ID, 
                        @Plant_Code, @Inserted_Host, @Inserted_User_ID, @Inserted_Date)";

                    for (int i = 0; i < emplist.Length; i++)
                    {
                        // Skip shop/model rows when no shop is provided (e.g. simple
                        // create-user form that doesn't capture shop/model).
                        if (emplist[i].Shop_ID <= 0)
                        {
                            continue;
                        }

                        using var commandShopModel = new SqlCommand(shopmodelquery, connection, transaction);
                        commandShopModel.Parameters.Add(new SqlParameter("@Employee_ID", empid));
                        commandShopModel.Parameters.Add(new SqlParameter("@Shop_ID", emplist[i].Shop_ID));
                        commandShopModel.Parameters.Add(new SqlParameter("@Model_ID", emplist[i].Model_ID));
                        commandShopModel.Parameters.Add(new SqlParameter("@Plant_ID", emplist[i].Plant_ID));
                        commandShopModel.Parameters.Add(new SqlParameter("@Plant_Code", emplist[i].Plant_Code));
                        commandShopModel.Parameters.Add(new SqlParameter("@Inserted_Host", emplist[i].Inserted_Host));
                        commandShopModel.Parameters.Add(new SqlParameter("@Inserted_User_ID", emplist[i].Inserted_User_ID));
                        commandShopModel.Parameters.Add(new SqlParameter("@Inserted_Date", DateTime.Now));

                        await commandShopModel.ExecuteNonQueryAsync();
                    }

                    await SaveEmployeeTeamsAsync(connection, transaction, empid,
                        emplist[0].Team_IDs, emplist[0].Inserted_Host, emplist[0].Inserted_User_ID);

                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Replaces the employee's team memberships with the supplied list.
        private static async Task SaveEmployeeTeamsAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            decimal employeeId,
            List<decimal> teamIds,
            string host,
            decimal userId)
        {
            const string deleteQuery = @"DELETE FROM MM_Employee_Team WHERE Employee_ID = @Employee_ID";
            using (var deleteCommand = new SqlCommand(deleteQuery, connection, transaction))
            {
                deleteCommand.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                await deleteCommand.ExecuteNonQueryAsync();
            }

            const string insertQuery = @"INSERT INTO MM_Employee_Team
                (Employee_ID, Team_ID, Inserted_Host, Inserted_User_ID, Inserted_Date)
                VALUES (@Employee_ID, @Team_ID, @Inserted_Host, @Inserted_User_ID, GETDATE())";

            foreach (var teamId in teamIds.Distinct().Where(id => id > 0))
            {
                using var insertCommand = new SqlCommand(insertQuery, connection, transaction);
                insertCommand.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                insertCommand.Parameters.Add(new SqlParameter("@Team_ID", teamId));
                insertCommand.Parameters.Add(new SqlParameter("@Inserted_Host", host));
                insertCommand.Parameters.Add(new SqlParameter("@Inserted_User_ID", userId));
                await insertCommand.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> UpdateUser(decimal employeeId, MM_EmployeeUpdateUserDto[] empupdatelist)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Update employee main record (includes Team_Lead_ID now).
                    const string updateEmployeeQuery = @"UPDATE MM_Employee
                        SET Employee_Name = @Employee_Name,
                            Employee_No = @Employee_No,
                            Email_Address = @Email_Address,
                            Designation_ID = @Designation_ID,
                            Reporting_Manager_ID = @Reporting_Manager_ID,
                            Team_Lead_ID = @Team_Lead_ID,
                            Audit_Type_Id = @Audit_Type_Id,
                            Plant_ID = @Plant_ID,
                            Plant_Code = @Plant_Code,
                            Updated_Host = @Updated_Host,
                            Updated_User_ID = @Updated_User_ID,
                            Updated_Date = @Updated_Date,
                            Is_Edited = 1
                        WHERE Employee_ID = @Employee_ID";

                    using var updateCommand = new SqlCommand(updateEmployeeQuery, connection, transaction);
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_Name", empupdatelist[0].Employee_Name));
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_No", empupdatelist[0].Employee_No));
                    updateCommand.Parameters.Add(new SqlParameter("@Email_Address", empupdatelist[0].Email_Address));
                    updateCommand.Parameters.Add(new SqlParameter("@Designation_ID", empupdatelist[0].Designation_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Reporting_Manager_ID", empupdatelist[0].Reporting_Manager_ID ?? (object)DBNull.Value));
                    var teamLeadId = empupdatelist[0].Team_Lead_ID;
                    updateCommand.Parameters.Add(new SqlParameter("@Team_Lead_ID",
                        teamLeadId.HasValue && teamLeadId.Value > 0 ? (object)teamLeadId.Value : DBNull.Value));
                    updateCommand.Parameters.Add(new SqlParameter("@Audit_Type_Id", empupdatelist[0].Audit_Type_Id));
                    updateCommand.Parameters.Add(new SqlParameter("@Plant_ID", empupdatelist[0].Plant_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Plant_Code", empupdatelist[0].Plant_Code));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_Host", empupdatelist[0].Updated_Host));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_User_ID", empupdatelist[0].Updated_User_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_Date", DateTime.Now));

                    await updateCommand.ExecuteNonQueryAsync();

                    // Only touch shop/model rows when the caller actually supplied them.
                    // Otherwise (e.g. the simple user-management form) leave them alone so
                    // we don't accidentally wipe existing shop/model assignments.
                    var hasShopRows = empupdatelist.Any(row => row.Shop_ID > 0);
                    if (hasShopRows)
                    {
                        const string deleteShopModelQuery = @"DELETE FROM MM_User_Shop_Model WHERE Employee_ID = @Employee_ID";
                        using var deleteCommand = new SqlCommand(deleteShopModelQuery, connection, transaction);
                        deleteCommand.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                        await deleteCommand.ExecuteNonQueryAsync();

                        const string insertShopModelQuery = @"INSERT INTO MM_User_Shop_Model
                            (Employee_ID, Shop_ID, Model_ID, Plant_ID, Plant_Code,
                             Inserted_Host, Inserted_User_ID, Inserted_Date)
                            VALUES
                            (@Employee_ID, @Shop_ID, @Model_ID, @Plant_ID,
                            @Plant_Code, @Inserted_Host, @Inserted_User_ID, @Inserted_Date)";

                        for (int i = 0; i < empupdatelist.Length; i++)
                        {
                            if (empupdatelist[i].Shop_ID <= 0)
                            {
                                continue;
                            }

                            using var insertCommand = new SqlCommand(insertShopModelQuery, connection, transaction);
                            insertCommand.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                            insertCommand.Parameters.Add(new SqlParameter("@Shop_ID", empupdatelist[i].Shop_ID));
                            insertCommand.Parameters.Add(new SqlParameter("@Model_ID", empupdatelist[i].Model_ID));
                            insertCommand.Parameters.Add(new SqlParameter("@Plant_ID", empupdatelist[i].Plant_ID));
                            insertCommand.Parameters.Add(new SqlParameter("@Plant_Code", empupdatelist[i].Plant_Code));
                            insertCommand.Parameters.Add(new SqlParameter("@Inserted_Host", empupdatelist[i].Updated_Host));
                            insertCommand.Parameters.Add(new SqlParameter("@Inserted_User_ID", empupdatelist[i].Updated_User_ID));
                            insertCommand.Parameters.Add(new SqlParameter("@Inserted_Date", DateTime.Now));

                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await SaveEmployeeTeamsAsync(connection, transaction, employeeId,
                        empupdatelist[0].Team_IDs, empupdatelist[0].Updated_Host, empupdatelist[0].Updated_User_ID);

                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> CheckEmployeeExistsByNoAsync(string employeeNo)
        {
            try
            {
                const string query = @"
                    SELECT COUNT(1) 
                    FROM MM_Employee 
                    WHERE Employee_No = @Employee_No 
                    AND (Is_Deleted IS NULL OR Is_Deleted = 0)";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Employee_No", employeeNo));

                var result = await command.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;
                return count > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(decimal employeeId)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // First, delete from MM_User_Shop_Model table
                    const string deleteShopModelQuery = @"DELETE FROM MM_User_Shop_Model WHERE Employee_ID = @Employee_ID";
                    using var shopModelCommand = new SqlCommand(deleteShopModelQuery, connection, transaction);
                    shopModelCommand.Parameters.AddWithValue("@Employee_ID", employeeId);
                    await shopModelCommand.ExecuteNonQueryAsync();

                    // Delete team memberships (FK to MM_Employee)
                    const string deleteTeamsQuery = @"DELETE FROM MM_Employee_Team WHERE Employee_ID = @Employee_ID";
                    using var teamsCommand = new SqlCommand(deleteTeamsQuery, connection, transaction);
                    teamsCommand.Parameters.AddWithValue("@Employee_ID", employeeId);
                    await teamsCommand.ExecuteNonQueryAsync();

                    // Then, delete from MM_Employee table
                    const string deleteEmployeeQuery = @"DELETE FROM MM_Employee WHERE Employee_ID = @Employee_ID";
                    using var employeeCommand = new SqlCommand(deleteEmployeeQuery, connection, transaction);
                    employeeCommand.Parameters.AddWithValue("@Employee_ID", employeeId);

                    var rowsAffected = await employeeCommand.ExecuteNonQueryAsync();

                    // Commit the transaction
                    transaction.Commit();

                    return rowsAffected > 0;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<MM_PlantDetailDto?> GetPlantDetailsAsync(decimal plantId)
        {
            MM_PlantDetailDto? plant = null;

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                const string plantQuery = @"
            SELECT Plant_ID, Plant_Name, Plant_Code
            FROM MM_Plant
            WHERE Plant_ID = @Plant_ID AND (Is_Deleted IS NULL OR Is_Deleted = 0)";

                using var command = new SqlCommand(plantQuery, connection);
                command.Parameters.Add(new SqlParameter("@Plant_ID", plantId));

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    plant = new MM_PlantDetailDto
                    {
                        Plant_ID = reader.GetDecimal(reader.GetOrdinal("Plant_ID")),
                        //Plant_Name = reader.GetString(reader.GetOrdinal("Plant_Name")),
                        Plant_Code = reader.GetString(reader.GetOrdinal("Plant_Code"))
                    };
                }
            }
            catch (Exception)
            {
                throw;
            }

            return plant;
        }

        public async Task<List<MM_DesignationListDto>> GetAllDesignationsAsync()
        {
            var designations = new List<MM_DesignationListDto>();

            const string query = @"
                SELECT Designation_ID, Designation_Name
                FROM MM_Designation
                WHERE (Is_Deleted IS NULL OR Is_Deleted = 0)
                ORDER BY Designation_ID";

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    designations.Add(new MM_DesignationListDto
                    {
                        Designation_ID = reader.GetDecimal("Designation_ID"),
                        Designation_Name = reader.GetString("Designation_Name")
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return designations;
        }

        public async Task<List<MM_EmployeeListDto>> GetEmployeesByModeAsync(decimal employeeId, string? mode, decimal? teamId = null)
        {
            var employees = new List<MM_EmployeeListDto>();

            var normalizedMode = (mode ?? "developer").Trim().ToLowerInvariant();
            var teamFilter = teamId ?? 0; // 0 = all teams the manager belongs to

            string query;
            if (normalizedMode == "manager")
            {
                // Team-based visibility: any manager who is a member of a team sees every
                // member of that team (not just their reporting hierarchy). @Team_ID = 0
                // means all of the manager's teams; a specific @Team_ID narrows to that
                // team (only if the manager is a member of it). Self is always included.
                query = @"
                    SELECT
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No,
                        ISNULL(e.Email_Address, '') AS Email_Address,
                        e.Designation_ID,
                        ISNULL(d.Designation_Name, '') AS Designation_Name,
                        e.Reporting_Manager_ID,
                        ISNULL(rm.Employee_Name, '') AS Reporting_Manager_Name,
                        e.Team_Lead_ID,
                        ISNULL(tl.Employee_Name, '') AS Team_Lead_Name,
                        e.Audit_Type_Id,
                        e.Plant_ID
                    FROM MM_Employee e
                    LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                    LEFT JOIN MM_Employee rm ON e.Reporting_Manager_ID = rm.Employee_ID
                    LEFT JOIN MM_Employee tl ON e.Team_Lead_ID = tl.Employee_ID
                    WHERE (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
                      AND (
                        e.Employee_ID = @Employee_ID
                        OR e.Employee_ID IN (
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
                    ORDER BY e.Employee_Name";
            }
            else if (normalizedMode == "lead")
            {
                // Self + employees whose Team_Lead_ID is the logged-in lead.
                query = @"
                    SELECT
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No,
                        ISNULL(e.Email_Address, '') AS Email_Address,
                        e.Designation_ID,
                        ISNULL(d.Designation_Name, '') AS Designation_Name,
                        e.Reporting_Manager_ID,
                        ISNULL(rm.Employee_Name, '') AS Reporting_Manager_Name,
                        e.Team_Lead_ID,
                        ISNULL(tl.Employee_Name, '') AS Team_Lead_Name,
                        e.Audit_Type_Id,
                        e.Plant_ID
                    FROM MM_Employee e
                    LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                    LEFT JOIN MM_Employee rm ON e.Reporting_Manager_ID = rm.Employee_ID
                    LEFT JOIN MM_Employee tl ON e.Team_Lead_ID = tl.Employee_ID
                    WHERE (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
                      AND (e.Employee_ID = @Employee_ID OR e.Team_Lead_ID = @Employee_ID)
                    ORDER BY e.Employee_Name";
            }
            else
            {
                // Developer / self only.
                query = @"
                    SELECT
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No,
                        ISNULL(e.Email_Address, '') AS Email_Address,
                        e.Designation_ID,
                        ISNULL(d.Designation_Name, '') AS Designation_Name,
                        e.Reporting_Manager_ID,
                        ISNULL(rm.Employee_Name, '') AS Reporting_Manager_Name,
                        e.Team_Lead_ID,
                        ISNULL(tl.Employee_Name, '') AS Team_Lead_Name,
                        e.Audit_Type_Id,
                        e.Plant_ID
                    FROM MM_Employee e
                    LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                    LEFT JOIN MM_Employee rm ON e.Reporting_Manager_ID = rm.Employee_ID
                    LEFT JOIN MM_Employee tl ON e.Team_Lead_ID = tl.Employee_ID
                    WHERE e.Employee_ID = @Employee_ID
                      AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";
            }

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                if (normalizedMode == "manager")
                {
                    command.Parameters.Add(new SqlParameter("@Team_ID", teamFilter));
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    employees.Add(new MM_EmployeeListDto
                    {
                        Employee_ID = reader.GetDecimal("Employee_ID"),
                        Employee_Name = reader.GetString("Employee_Name"),
                        Employee_No = reader.GetString("Employee_No"),
                        Email_Address = reader.GetString("Email_Address"),
                        Designation_ID = reader.GetDecimal("Designation_ID"),
                        Designation_Name = reader.GetString("Designation_Name"),
                        Reporting_Manager_ID = reader.IsDBNull("Reporting_Manager_ID") ? null : reader.GetDecimal("Reporting_Manager_ID"),
                        Reporting_Manager_Name = reader.GetString("Reporting_Manager_Name"),
                        Team_Lead_ID = reader.IsDBNull("Team_Lead_ID") ? null : reader.GetDecimal("Team_Lead_ID"),
                        Team_Lead_Name = reader.GetString("Team_Lead_Name"),
                        Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                        Plant_ID = reader.GetDecimal("Plant_ID")
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return employees;
        }

        public async Task<MM_EmployeeDetailDto?> GetEmployeeListAsync(decimal employeeId)
        {
            MM_EmployeeDetailDto? employee = null;

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();


                var employeeQuery = @"
                SELECT 
                    e.Employee_ID,
                    e.Employee_Name,
                    e.Employee_No,
                    ISNULL(e.Email_Address, '') as Email_Address,
                    e.Designation_ID,
                    ISNULL(d.Designation_Name, '') as Designation_Name,
                    e.Reporting_Manager_ID,
                    e.Audit_Type_Id,
                    e.Plant_ID,
                    p.Plant_Code
                FROM MM_Employee e
                LEFT JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                LEFT JOIN MM_Plant p ON e.Plant_ID = p.Plant_ID
                WHERE e.Employee_No = @Employee_No AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";


                using (var command = new SqlCommand(employeeQuery, connection))
                {

                    command.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));

                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        employee = new MM_EmployeeDetailDto
                        {
                            Employee_ID = reader.GetDecimal("Employee_ID"),
                            Employee_Name = reader.GetString("Employee_Name"),
                            Employee_No = reader.GetString("Employee_No"),
                            Email_Address = reader.GetString("Email_Address"),
                            Designation_ID = reader.GetDecimal("Designation_ID"),
                            Designation_Name = reader.GetString("Designation_Name"),
                            Reporting_Manager_ID = reader.IsDBNull("Reporting_Manager_ID") ? null : reader.GetDecimal("Reporting_Manager_ID"),
                            Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                            Plant_ID = reader.GetDecimal("Plant_ID"),
                            Plant_Code = reader.IsDBNull("Plant_Code") ? string.Empty : reader.GetString("Plant_Code"),
                            Shop_ID = new List<int>(),
                            Model_ID = new List<int>(),
                            Hostname = ""
                        };
                    }
                }

                // If employee found, get shop and model data
                if (employee != null)
                {
                    const string shopModelQuery = @"
                        SELECT Shop_ID, Model_ID
                        FROM MM_User_Shop_Model
                        WHERE Employee_ID = @Employee_ID
                        ORDER BY Shop_ID, Model_ID";

                    using (var shopModelCommand = new SqlCommand(shopModelQuery, connection))
                    {
                        shopModelCommand.Parameters.Add(new SqlParameter("@Employee_ID", employee.Employee_ID));
                        using var shopModelReader = await shopModelCommand.ExecuteReaderAsync();

                        while (await shopModelReader.ReadAsync())
                        {
                            var shopId = Convert.ToInt32(shopModelReader.GetDecimal("Shop_ID"));
                            var modelId = Convert.ToInt32(shopModelReader.GetDecimal("Model_ID"));

                            employee.Shop_ID.Add(shopId);
                            employee.Model_ID.Add(modelId);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return employee;
        }

    }
}
