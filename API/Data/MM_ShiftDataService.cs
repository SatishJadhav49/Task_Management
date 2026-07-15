using Taskmanagement_API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace Taskmanagement_API.Data
{
    public class MM_ShiftDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_ShiftDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const decimal GeneralShiftId = 1m;

        private static DateTime ParseDate(string value) =>
            DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Monday of the week that contains the given date.
        private static DateTime MondayOf(DateTime date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7; // Mon=0 .. Sun=6
            return date.Date.AddDays(-diff);
        }

        public async Task<List<MM_ShiftDto>> GetAllShiftsAsync()
        {
            var shifts = new List<MM_ShiftDto>();

            const string query = @"
                SELECT Shift_ID, Shift_Name, ISNULL(Shift_Code, '') AS Shift_Code,
                       ISNULL(Start_Time, '') AS Start_Time, ISNULL(End_Time, '') AS End_Time,
                       Is_General, SORTORDER
                FROM MM_Shift_Master
                ORDER BY SORTORDER";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                shifts.Add(new MM_ShiftDto
                {
                    Shift_ID = reader.GetDecimal("Shift_ID"),
                    Shift_Name = reader.GetString("Shift_Name"),
                    Shift_Code = reader.GetString("Shift_Code"),
                    Start_Time = reader.GetString("Start_Time"),
                    End_Time = reader.GetString("End_Time"),
                    Is_General = reader.GetBoolean("Is_General"),
                    SORTORDER = reader.GetDecimal("SORTORDER")
                });
            }

            return shifts;
        }

        // Team roster (leads + developers) with their weekly shift for the given week
        // and a count of mid-week day overrides.
        public async Task<List<MM_ShiftRosterDto>> GetWeeklyScheduleAsync(decimal teamId, string weekStart)
        {
            var roster = new List<MM_ShiftRosterDto>();
            var monday = MondayOf(ParseDate(weekStart));
            var sunday = monday.AddDays(6);

            const string query = @"
                SELECT
                    e.Employee_ID,
                    e.Employee_Name,
                    e.Employee_No,
                    e.Designation_ID,
                    ISNULL(d.Designation_Name, '') AS Designation_Name,
                    ISNULL(sched.Shift_ID, @GeneralShiftId) AS Shift_ID,
                    ISNULL(sm.Shift_Name, gen.Shift_Name) AS Shift_Name,
                    (
                        SELECT COUNT(1)
                        FROM MM_Shift_Day_Override ov
                        WHERE ov.Employee_ID = e.Employee_ID
                          AND ov.Shift_Date BETWEEN @Monday AND @Sunday
                    ) AS Override_Count
                FROM MM_Employee_Team et
                INNER JOIN MM_Employee e ON e.Employee_ID = et.Employee_ID
                LEFT JOIN MM_Designation d ON d.Designation_ID = e.Designation_ID
                LEFT JOIN MM_Shift_Schedule sched
                    ON sched.Employee_ID = e.Employee_ID AND sched.Week_Start_Date = @Monday
                LEFT JOIN MM_Shift_Master sm ON sm.Shift_ID = sched.Shift_ID
                CROSS JOIN (SELECT Shift_Name FROM MM_Shift_Master WHERE Shift_ID = @GeneralShiftId) gen
                WHERE et.Team_ID = @Team_ID
                  AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
                  AND e.Designation_ID IN (2, 3,4)
                ORDER BY e.Employee_Name";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Team_ID", teamId);
            command.Parameters.AddWithValue("@Monday", monday.Date);
            command.Parameters.AddWithValue("@Sunday", sunday.Date);
            command.Parameters.AddWithValue("@GeneralShiftId", GeneralShiftId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                roster.Add(new MM_ShiftRosterDto
                {
                    Employee_ID = reader.GetDecimal("Employee_ID"),
                    Employee_Name = reader.GetString("Employee_Name"),
                    Employee_No = reader.GetString("Employee_No"),
                    Designation_ID = reader.GetDecimal("Designation_ID"),
                    Designation_Name = reader.GetString("Designation_Name"),
                    Shift_ID = reader.GetDecimal("Shift_ID"),
                    Shift_Name = reader.GetString("Shift_Name"),
                    Override_Count = reader.GetInt32("Override_Count")
                });
            }

            return roster;
        }

        // Upsert a single employee's weekly shift (called on every drag-drop).
        public async Task<bool> SaveWeeklyAssignmentAsync(MM_ShiftWeeklyAssignmentDto dto)
        {
            var monday = MondayOf(ParseDate(dto.Week_Start_Date));

            const string updateQuery = @"
                UPDATE MM_Shift_Schedule
                SET Shift_ID = @Shift_ID,
                    Team_ID = @Team_ID,
                    Updated_Host = @Host,
                    Updated_User_ID = @User_ID,
                    Updated_Date = GETDATE()
                WHERE Employee_ID = @Employee_ID AND Week_Start_Date = @Monday";

            const string insertQuery = @"
                INSERT INTO MM_Shift_Schedule
                    (Employee_ID, Team_ID, Week_Start_Date, Shift_ID, Inserted_Host, Inserted_User_ID, Inserted_Date)
                VALUES
                    (@Employee_ID, @Team_ID, @Monday, @Shift_ID, @Host, @User_ID, GETDATE())";

            using var connection = await _connectionFactory.CreateConnectionAsync();

            using (var updateCommand = new SqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@Shift_ID", dto.Shift_ID);
                updateCommand.Parameters.AddWithValue("@Team_ID", dto.Team_ID);
                updateCommand.Parameters.AddWithValue("@Host", (object?)dto.Host ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@User_ID", dto.User_ID);
                updateCommand.Parameters.AddWithValue("@Employee_ID", dto.Employee_ID);
                updateCommand.Parameters.AddWithValue("@Monday", monday.Date);

                var rows = await updateCommand.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    return true;
                }
            }

            using (var insertCommand = new SqlCommand(insertQuery, connection))
            {
                insertCommand.Parameters.AddWithValue("@Employee_ID", dto.Employee_ID);
                insertCommand.Parameters.AddWithValue("@Team_ID", dto.Team_ID);
                insertCommand.Parameters.AddWithValue("@Monday", monday.Date);
                insertCommand.Parameters.AddWithValue("@Shift_ID", dto.Shift_ID);
                insertCommand.Parameters.AddWithValue("@Host", (object?)dto.Host ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@User_ID", dto.User_ID);
                await insertCommand.ExecuteNonQueryAsync();
            }

            return true;
        }

        // The 7 days of the week with the effective shift per day (override or weekly).
        public async Task<List<MM_ShiftDayDto>> GetDayOverridesAsync(decimal employeeId, string weekStart)
        {
            var monday = MondayOf(ParseDate(weekStart));
            var sunday = monday.AddDays(6);

            decimal weeklyShiftId = GeneralShiftId;
            var overrides = new Dictionary<DateTime, decimal>();

            using var connection = await _connectionFactory.CreateConnectionAsync();

            const string weeklyQuery = @"
                SELECT Shift_ID FROM MM_Shift_Schedule
                WHERE Employee_ID = @Employee_ID AND Week_Start_Date = @Monday";
            using (var weeklyCommand = new SqlCommand(weeklyQuery, connection))
            {
                weeklyCommand.Parameters.AddWithValue("@Employee_ID", employeeId);
                weeklyCommand.Parameters.AddWithValue("@Monday", monday.Date);
                var result = await weeklyCommand.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    weeklyShiftId = Convert.ToDecimal(result);
                }
            }

            const string overrideQuery = @"
                SELECT Shift_Date, Shift_ID FROM MM_Shift_Day_Override
                WHERE Employee_ID = @Employee_ID AND Shift_Date BETWEEN @Monday AND @Sunday";
            using (var overrideCommand = new SqlCommand(overrideQuery, connection))
            {
                overrideCommand.Parameters.AddWithValue("@Employee_ID", employeeId);
                overrideCommand.Parameters.AddWithValue("@Monday", monday.Date);
                overrideCommand.Parameters.AddWithValue("@Sunday", sunday.Date);
                using var reader = await overrideCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    overrides[reader.GetDateTime("Shift_Date").Date] = reader.GetDecimal("Shift_ID");
                }
            }

            var days = new List<MM_ShiftDayDto>();
            for (int i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                var hasOverride = overrides.TryGetValue(date, out var overrideShift);
                days.Add(new MM_ShiftDayDto
                {
                    Shift_Date = date.ToString("yyyy-MM-dd"),
                    Day_Label = date.ToString("ddd", CultureInfo.InvariantCulture),
                    Weekly_Shift_ID = weeklyShiftId,
                    Effective_Shift_ID = hasOverride ? overrideShift : weeklyShiftId,
                    Is_Override = hasOverride
                });
            }

            return days;
        }

        // Upsert (or clear) a single day's override. When the chosen shift equals the
        // week's shift the override row is removed so it no longer counts as a change.
        public async Task<bool> SaveDayOverrideAsync(MM_ShiftDayOverrideDto dto)
        {
            var date = ParseDate(dto.Shift_Date).Date;
            var monday = MondayOf(date);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            decimal weeklyShiftId = GeneralShiftId;
            const string weeklyQuery = @"
                SELECT Shift_ID FROM MM_Shift_Schedule
                WHERE Employee_ID = @Employee_ID AND Week_Start_Date = @Monday";
            using (var weeklyCommand = new SqlCommand(weeklyQuery, connection))
            {
                weeklyCommand.Parameters.AddWithValue("@Employee_ID", dto.Employee_ID);
                weeklyCommand.Parameters.AddWithValue("@Monday", monday.Date);
                var result = await weeklyCommand.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    weeklyShiftId = Convert.ToDecimal(result);
                }
            }

            // Same as the weekly shift -> no override needed, remove any existing one.
            if (dto.Shift_ID == weeklyShiftId)
            {
                const string deleteQuery = @"
                    DELETE FROM MM_Shift_Day_Override
                    WHERE Employee_ID = @Employee_ID AND Shift_Date = @Shift_Date";
                using var deleteCommand = new SqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@Employee_ID", dto.Employee_ID);
                deleteCommand.Parameters.AddWithValue("@Shift_Date", date);
                await deleteCommand.ExecuteNonQueryAsync();
                return true;
            }

            const string updateQuery = @"
                UPDATE MM_Shift_Day_Override
                SET Shift_ID = @Shift_ID,
                    Team_ID = @Team_ID,
                    Updated_Host = @Host,
                    Updated_User_ID = @User_ID,
                    Updated_Date = GETDATE()
                WHERE Employee_ID = @Employee_ID AND Shift_Date = @Shift_Date";

            const string insertQuery = @"
                INSERT INTO MM_Shift_Day_Override
                    (Employee_ID, Team_ID, Shift_Date, Shift_ID, Inserted_Host, Inserted_User_ID, Inserted_Date)
                VALUES
                    (@Employee_ID, @Team_ID, @Shift_Date, @Shift_ID, @Host, @User_ID, GETDATE())";

            using (var updateCommand = new SqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@Shift_ID", dto.Shift_ID);
                updateCommand.Parameters.AddWithValue("@Team_ID", dto.Team_ID);
                updateCommand.Parameters.AddWithValue("@Host", (object?)dto.Host ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@User_ID", dto.User_ID);
                updateCommand.Parameters.AddWithValue("@Employee_ID", dto.Employee_ID);
                updateCommand.Parameters.AddWithValue("@Shift_Date", date);

                var rows = await updateCommand.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    return true;
                }
            }

            using (var insertCommand = new SqlCommand(insertQuery, connection))
            {
                insertCommand.Parameters.AddWithValue("@Employee_ID", dto.Employee_ID);
                insertCommand.Parameters.AddWithValue("@Team_ID", dto.Team_ID);
                insertCommand.Parameters.AddWithValue("@Shift_Date", date);
                insertCommand.Parameters.AddWithValue("@Shift_ID", dto.Shift_ID);
                insertCommand.Parameters.AddWithValue("@Host", (object?)dto.Host ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@User_ID", dto.User_ID);
                await insertCommand.ExecuteNonQueryAsync();
            }

            return true;
        }
    }
}
