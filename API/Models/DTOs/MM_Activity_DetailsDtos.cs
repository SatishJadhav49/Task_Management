namespace Taskmanagement_API.Models.DTOs
{

    public class MM_Activity_DetailsCreateDto
    {
        public decimal Activity_Type_ID { get; set; }
        public string Description { get; set; } = String.Empty;
        public decimal Employee_ID { get; set; }
        public DateTime Activity_Date { get; set; }
        public bool? Add_In_Mail { get; set; }
        public string? Inserted_Host { get; set; }
        public string? Plant_Code { get; set; }
        public decimal? Inserted_User_ID { get; set; }

    }

    public class MM_Activity_DetailsListDto
    {
        public decimal Activity_ID { get; set; }
        public decimal Activity_Type_ID { get; set; }
        public string Description { get; set; } = String.Empty;
        public string Activity_Name { get; set; } = String.Empty;
        public string Activity_Date { get; set; } = String.Empty;
        public bool? Add_In_Mail { get; set; }
        public string? Status_Date { get; set; } = String.Empty;
        public string? In_Time { get; set; } = String.Empty;
        public string? Out_Time { get; set; } = String.Empty;

    }

    public class MM_Activity_DetailsUpdateDto
    {

        public decimal Activity_ID { get; set; }
        public decimal Activity_Type_ID { get; set; }
        public string Description { get; set; } = String.Empty;
        public decimal Employee_ID { get; set; }
        public DateTime Activity_Date { get; set; }
        public bool? Add_In_Mail { get; set; }
        public string? Updated_Host { get; set; }
        public string? Plant_Code { get; set; }
        public decimal? Updated_User_ID { get; set; }
    }
}