using Taskmanagement_API.Models;
using Taskmanagement_API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Taskmanagement_API.Data
{
    public class MM_ActivityDetailsService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_ActivityDetailsService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<MM_Activity_Type_MasterListDto>> GetActivityTypesAsync()
        {
            var activityTypes = new List<MM_Activity_Type_MasterListDto>();

            try
            {
                const string query = @"
                    select * from MM_Activity_Type_Master
                    ORDER BY SORTORDER";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    activityTypes.Add(new MM_Activity_Type_MasterListDto
                    {
                        Activity_Type_ID = reader.GetDecimal("Activity_Type_ID"),
                        Activity_Name = reader.GetString("Activity_Name"),
                        SORTORDER = reader.GetDecimal("SORTORDER")
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return activityTypes;
        }

        public async Task<MM_Activity_DetailsCreateDto> CreateActivityDetailAsync(MM_Activity_DetailsCreateDto activityDetailDto)
        {
            try
            {
                const string query = @"
                    INSERT INTO MM_Activity_Details (Activity_Type_ID, Description, Employee_ID, Activity_Date, Add_In_Mail, Inserted_Host, Plant_Code, Inserted_User_ID, Inserted_Date)
                    VALUES (@Activity_Type_ID, @Description, @Employee_ID, @Activity_Date, @Add_In_Mail, @Inserted_Host, @Plant_Code, @Inserted_User_ID,getdate());
                    SELECT SCOPE_IDENTITY();";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Activity_Type_ID", activityDetailDto.Activity_Type_ID);
                command.Parameters.AddWithValue("@Description", activityDetailDto.Description);
                command.Parameters.AddWithValue("@Employee_ID", activityDetailDto.Employee_ID);
                command.Parameters.AddWithValue("@Activity_Date", activityDetailDto.Activity_Date);
                command.Parameters.AddWithValue("@Add_In_Mail", (object?)activityDetailDto.Add_In_Mail ?? DBNull.Value);
                command.Parameters.AddWithValue("@Inserted_Host", (object?)activityDetailDto.Inserted_Host ?? DBNull.Value);
                command.Parameters.AddWithValue("@Plant_Code", (object?)activityDetailDto.Plant_Code ?? DBNull.Value);
                command.Parameters.AddWithValue("@Inserted_User_ID", (object?)activityDetailDto.Inserted_User_ID ?? DBNull.Value);

                var newId = await command.ExecuteScalarAsync();
                if (newId != null)
                {
                    activityDetailDto.Activity_Type_ID = Convert.ToDecimal(newId);
                    return activityDetailDto;
                }
                else
                {
                    throw new Exception("Failed to retrieve the new ID after inserting the activity detail.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<MM_Activity_DetailsListDto>> GetActivitiesByDateAsync(string filterFrom,string filterTo, decimal employeeId)
        {
            var activities = new List<MM_Activity_DetailsListDto>();

            try
            {
                const string query = @"
                    SELECT 
details.Activity_Date,
details.Activity_ID,
details.Description,
details.Add_In_Mail,
activity.Activity_Name,
activity.Activity_Type_ID,
ds.Date as Status_Date,
ds.In_Time,
ds.Out_Time
FROM MM_Activity_Details details
inner join MM_Activity_Type_Master activity on activity.Activity_Type_ID = details.Activity_Type_ID
left join MM_Daily_Status ds on details.Activity_Date = ds.Date and ds.Employee_ID = details.Employee_ID
WHERE (CAST(details.Activity_Date AS DATE) BETWEEN @FilterFrom AND @FilterTo) AND details.Employee_ID = @Employee_ID 
order by details.Activity_Date";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@FilterFrom", filterFrom);
                command.Parameters.AddWithValue("@FilterTo", filterTo);
                command.Parameters.AddWithValue("@Employee_ID", employeeId);

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    activities.Add(new MM_Activity_DetailsListDto
                    {
                        Activity_ID = reader.GetDecimal("Activity_ID"),
                        Activity_Type_ID = reader.GetDecimal("Activity_Type_ID"),
                        Description = reader.GetString("Description"),
                        Activity_Name = reader.GetString("Activity_Name"),
                        Activity_Date = reader.GetDateTime("Activity_Date").ToString("yyyy-MM-dd"),
                        Add_In_Mail = reader.IsDBNull(reader.GetOrdinal("Add_In_Mail")) ? (bool?)null : reader.GetBoolean("Add_In_Mail"),
                        Status_Date = reader.IsDBNull(reader.GetOrdinal("Status_Date")) ? (string?)null : reader.GetDateTime("Status_Date").ToString("yyyy-MM-dd"),
                        In_Time = reader.IsDBNull(reader.GetOrdinal("In_Time")) ? (string?)null : reader.GetDateTime("In_Time").ToString(),
                        Out_Time = reader.IsDBNull(reader.GetOrdinal("Out_Time")) ? (string?)null : reader.GetDateTime("Out_Time").ToString(),
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return activities;
        }

        public async Task<bool> DeleteActivityDetailAsync(decimal activityId)
        {
            try
            {
                const string query = "DELETE FROM MM_Activity_Details WHERE Activity_ID = @Activity_ID";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Activity_ID", activityId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        

        public async Task<MM_Activity_DetailsUpdateDto> UpdateActivityDetailAsync(MM_Activity_DetailsUpdateDto activityDetailDto)
        {
            try
            {
                const string query = @"
                    UPDATE MM_Activity_Details
                    SET Activity_Type_ID = @Activity_Type_ID,
                        Description = @Description,
                        Employee_ID = @Employee_ID,
                        Activity_Date = @Activity_Date,
                        Add_In_Mail = @Add_In_Mail,
                        Updated_Host = @Updated_Host,
                        Plant_Code = @Plant_Code,
                        Updated_User_ID = @Updated_User_ID,
                        Updated_Date = getdate()
                    WHERE Activity_ID = @Activity_ID";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Activity_Type_ID", activityDetailDto.Activity_Type_ID);
                command.Parameters.AddWithValue("@Description", activityDetailDto.Description);
                command.Parameters.AddWithValue("@Employee_ID", activityDetailDto.Employee_ID);
                command.Parameters.AddWithValue("@Activity_Date", activityDetailDto.Activity_Date);
                command.Parameters.AddWithValue("@Add_In_Mail", (object?)activityDetailDto.Add_In_Mail ?? DBNull.Value);
                command.Parameters.AddWithValue("@Updated_Host", (object?)activityDetailDto.Updated_Host ?? DBNull.Value);
                command.Parameters.AddWithValue("@Plant_Code", (object?)activityDetailDto.Plant_Code ?? DBNull.Value);
                command.Parameters.AddWithValue("@Updated_User_ID", (object?)activityDetailDto.Updated_User_ID ?? DBNull.Value);
                command.Parameters.AddWithValue("@Activity_ID", activityDetailDto.Activity_ID);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return activityDetailDto;
                }
                else
                {
                    throw new Exception("No records were updated. Please check the provided Activity ID.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}