using Taskmanagement_API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Taskmanagement_API.Data
{
    public class MM_DeploymentDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_DeploymentDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Raise a new request. The approver is resolved from the requester's
        // reporting manager at creation time so the routing is fixed for this request.
        public async Task<decimal> CreateRequestAsync(MM_Deployment_CreateDto dto)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Resolve the requester's reporting manager.
            object approverManager = DBNull.Value;
            const string managerQuery = @"
                SELECT Reporting_Manager_ID
                FROM MM_Employee
                WHERE Employee_ID = @Employee_ID
                  AND (Is_Deleted IS NULL OR Is_Deleted = 0)";
            using (var managerCommand = new SqlCommand(managerQuery, connection))
            {
                managerCommand.Parameters.AddWithValue("@Employee_ID", dto.Requested_By);
                var result = await managerCommand.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    approverManager = result;
                }
            }

            const string insertQuery = @"
                INSERT INTO MM_Deployment_Request
                    (Feature_Module, Changes_Description, Risk_Challenge, Change_Type,
                     Requested_By, Approver_Manager_ID, Status, Plant_Code,
                     Inserted_Host, Inserted_User_ID, Inserted_Date)
                VALUES
                    (@Feature_Module, @Changes_Description, @Risk_Challenge, @Change_Type,
                     @Requested_By, @Approver_Manager_ID, 'Pending', @Plant_Code,
                     @Inserted_Host, @Inserted_User_ID, GETDATE());
                SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Feature_Module", dto.Feature_Module);
            command.Parameters.AddWithValue("@Changes_Description", dto.Changes_Description);
            command.Parameters.AddWithValue("@Risk_Challenge",
                string.IsNullOrWhiteSpace(dto.Risk_Challenge) ? DBNull.Value : dto.Risk_Challenge.Trim());
            command.Parameters.AddWithValue("@Change_Type", dto.Change_Type);
            command.Parameters.AddWithValue("@Requested_By", dto.Requested_By);
            command.Parameters.AddWithValue("@Approver_Manager_ID", approverManager);
            command.Parameters.AddWithValue("@Plant_Code", (object?)dto.Plant_Code ?? DBNull.Value);
            command.Parameters.AddWithValue("@Inserted_Host", (object?)dto.Inserted_Host ?? DBNull.Value);
            command.Parameters.AddWithValue("@Inserted_User_ID", dto.Inserted_User_ID);

            var newId = await command.ExecuteScalarAsync();
            return newId != null ? Convert.ToDecimal(newId) : 0m;
        }

        // Visibility:
        //   developer -> own requests
        //   lead      -> own + requests raised by their team (Team_Lead_ID = self)
        //   manager   -> requests routed to them for approval (Approver_Manager_ID = self)
        public async Task<List<MM_Deployment_ListDto>> GetRequestsByEmployeeAsync(decimal employeeId, string? mode)
        {
            var requests = new List<MM_Deployment_ListDto>();
            var normalizedMode = (mode ?? "developer").Trim().ToLowerInvariant();

            string whereClause = normalizedMode switch
            {
                "manager" => "r.Approver_Manager_ID = @Employee_ID",
                "lead" => "(r.Requested_By = @Employee_ID OR req.Team_Lead_ID = @Employee_ID)",
                _ => "r.Requested_By = @Employee_ID",
            };

            string query = $@"
                SELECT
                    r.Request_ID,
                    r.Feature_Module,
                    r.Changes_Description,
                    r.Risk_Challenge,
                    r.Change_Type,
                    r.Status,
                    r.Manager_Remark,
                    r.Requested_By,
                    ISNULL(req.Employee_Name, '') AS Requested_By_Name,
                    ISNULL(d.Designation_Name, '') AS Requested_By_Designation,
                    r.Approver_Manager_ID,
                    ISNULL(mgr.Employee_Name, '') AS Approver_Manager_Name,
                    r.Approved_By,
                    ISNULL(app.Employee_Name, '') AS Approved_By_Name,
                    r.Approved_Date,
                    r.Inserted_Date
                FROM MM_Deployment_Request r
                INNER JOIN MM_Employee req ON req.Employee_ID = r.Requested_By
                LEFT JOIN MM_Designation d ON d.Designation_ID = req.Designation_ID
                LEFT JOIN MM_Employee mgr ON mgr.Employee_ID = r.Approver_Manager_ID
                LEFT JOIN MM_Employee app ON app.Employee_ID = r.Approved_By
                WHERE {whereClause}
                ORDER BY r.Inserted_Date DESC";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Employee_ID", employeeId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                requests.Add(new MM_Deployment_ListDto
                {
                    Request_ID = reader.GetDecimal("Request_ID"),
                    Feature_Module = reader.GetString("Feature_Module"),
                    Changes_Description = reader.GetString("Changes_Description"),
                    Risk_Challenge = reader.IsDBNull("Risk_Challenge") ? null : reader.GetString("Risk_Challenge"),
                    Change_Type = reader.GetString("Change_Type"),
                    Status = reader.GetString("Status"),
                    Manager_Remark = reader.IsDBNull("Manager_Remark") ? null : reader.GetString("Manager_Remark"),
                    Requested_By = reader.GetDecimal("Requested_By"),
                    Requested_By_Name = reader.GetString("Requested_By_Name"),
                    Requested_By_Designation = reader.GetString("Requested_By_Designation"),
                    Approver_Manager_ID = reader.IsDBNull("Approver_Manager_ID") ? null : reader.GetDecimal("Approver_Manager_ID"),
                    Approver_Manager_Name = reader.GetString("Approver_Manager_Name"),
                    Approved_By = reader.IsDBNull("Approved_By") ? null : reader.GetDecimal("Approved_By"),
                    Approved_By_Name = reader.GetString("Approved_By_Name"),
                    Approved_Date = reader.IsDBNull("Approved_Date")
                        ? null
                        : reader.GetDateTime("Approved_Date").ToString("yyyy-MM-ddTHH:mm:ss"),
                    Requested_Date = reader.GetDateTime("Inserted_Date").ToString("yyyy-MM-ddTHH:mm:ss"),
                });
            }

            return requests;
        }

        // Manager approves or rejects. Only a manager (Designation_ID = 1) may decide,
        // and only while the request is still Pending.
        public async Task<bool> UpdateDecisionAsync(decimal requestId, MM_Deployment_DecisionDto dto)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

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
                throw new UnauthorizedAccessException("Only managers can approve or reject deployment requests.");
            }

            var status = dto.Status?.Trim();
            if (!string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Status must be Approved or Rejected.");
            }

            const string updateQuery = @"
                UPDATE MM_Deployment_Request
                SET Status          = @Status,
                    Manager_Remark  = @Manager_Remark,
                    Approved_By     = @Approved_By,
                    Approved_Date   = GETDATE(),
                    Updated_Host    = @Updated_Host,
                    Updated_User_ID = @Updated_User_ID,
                    Updated_Date    = GETDATE()
                WHERE Request_ID = @Request_ID
                  AND Status = 'Pending'";

            using var command = new SqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@Manager_Remark",
                string.IsNullOrWhiteSpace(dto.Manager_Remark) ? DBNull.Value : dto.Manager_Remark.Trim());
            command.Parameters.AddWithValue("@Approved_By", dto.Updated_User_ID);
            command.Parameters.AddWithValue("@Updated_Host", (object?)dto.Updated_Host ?? DBNull.Value);
            command.Parameters.AddWithValue("@Updated_User_ID", dto.Updated_User_ID);
            command.Parameters.AddWithValue("@Request_ID", requestId);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // Requester may withdraw their own request while it is still Pending.
        public async Task<bool> DeleteRequestAsync(decimal requestId, decimal employeeId)
        {
            const string query = @"
                DELETE FROM MM_Deployment_Request
                WHERE Request_ID = @Request_ID
                  AND Requested_By = @Employee_ID
                  AND Status = 'Pending'";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Request_ID", requestId);
            command.Parameters.AddWithValue("@Employee_ID", employeeId);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
