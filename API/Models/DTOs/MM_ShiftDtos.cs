namespace Taskmanagement_API.Models.DTOs
{
    public class MM_ShiftDto
    {
        public decimal Shift_ID { get; set; }
        public string Shift_Name { get; set; } = String.Empty;
        public string Shift_Code { get; set; } = String.Empty;
        public string Start_Time { get; set; } = String.Empty;
        public string End_Time { get; set; } = String.Empty;
        public bool Is_General { get; set; }
        public decimal SORTORDER { get; set; }
    }

    // One employee's weekly assignment on the board.
    public class MM_ShiftRosterDto
    {
        public decimal Employee_ID { get; set; }
        public string Employee_Name { get; set; } = String.Empty;
        public string Employee_No { get; set; } = String.Empty;
        public decimal Designation_ID { get; set; }
        public string Designation_Name { get; set; } = String.Empty;
        public decimal Shift_ID { get; set; } = 1; // defaults to General
        public string Shift_Name { get; set; } = String.Empty;
        // How many days in the selected week differ from the weekly shift.
        public int Override_Count { get; set; }
    }

    public class MM_ShiftWeeklyAssignmentDto
    {
        public decimal Employee_ID { get; set; }
        public decimal Team_ID { get; set; }
        public string Week_Start_Date { get; set; } = String.Empty; // yyyy-MM-dd (Monday)
        public decimal Shift_ID { get; set; }
        public string Host { get; set; } = String.Empty;
        public decimal User_ID { get; set; }
    }

    // Effective shift for a single day within the week (weekly value or override).
    public class MM_ShiftDayDto
    {
        public string Shift_Date { get; set; } = String.Empty; // yyyy-MM-dd
        public string Day_Label { get; set; } = String.Empty;  // Mon, Tue, ...
        public decimal Weekly_Shift_ID { get; set; }
        public decimal Effective_Shift_ID { get; set; }
        public bool Is_Override { get; set; }
    }

    public class MM_ShiftDayOverrideDto
    {
        public decimal Employee_ID { get; set; }
        public decimal Team_ID { get; set; }
        public string Shift_Date { get; set; } = String.Empty; // yyyy-MM-dd
        public decimal Shift_ID { get; set; }
        public string Host { get; set; } = String.Empty;
        public decimal User_ID { get; set; }
    }
}
