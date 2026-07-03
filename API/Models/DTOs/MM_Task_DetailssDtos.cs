namespace Taskmanagement_API.Models.DTOs
{
    public class MM_Task_DetailsCreateDto
    {
        public decimal Task_ID { get; set; }
        public decimal Employee_ID { get; set; }
        public string Due_Date { get; set; } = String.Empty;
        public string Task_Description { get; set; } = String.Empty;
        public string Priority { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string Inserted_Host { get; set; } = String.Empty;
        public decimal Inserted_User_ID { get; set; }
        public string Plant_Code { get; set; } = String.Empty;
    }

    public class MM_Task_DetailsListDto
    {
        public decimal Task_ID { get; set; }
        public decimal Employee_ID { get; set; }
        public string Due_Date { get; set; } = String.Empty;
        public string Task_Description { get; set; } = String.Empty;
        public string Priority { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string Responsibility { get; set; } = String.Empty;
        public string? Manager_Remark { get; set; }
        public decimal? Remark_Updated_By { get; set; }
        public string Remark_Updated_By_Name { get; set; } = String.Empty;
        public string? Remark_Updated_Date { get; set; }
    }

    public class MM_Task_RemarkUpdateDto
    {
        public string Manager_Remark { get; set; } = String.Empty;
        public decimal Updated_User_ID { get; set; }
        public string Updated_Host { get; set; } = String.Empty;
    }

    public class MM_Task_DetailsUpdateDto
    {
        public decimal Task_ID { get; set; }
        public decimal Employee_ID { get; set; }
        public string Due_Date { get; set; } = String.Empty;
        public string Task_Description { get; set; } = String.Empty;
        public string Priority { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string Updated_Host { get; set; } = String.Empty;
        public decimal Updated_User_ID { get; set; }
    }
}