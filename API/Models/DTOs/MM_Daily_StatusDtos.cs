namespace Taskmanagement_API.Models.DTOs
{
    public class MM_Daily_StatusDto
    {
        public decimal Status_ID { get; set; }
        public decimal Employee_ID { get; set; }
        public string Date { get; set; } = String.Empty;
        public string In_Time { get; set; }  = String.Empty;
        public string Out_Time { get; set; }  = String.Empty;
        public string Status { get; set; }  = String.Empty;
        public string Remark { get; set; }  = String.Empty;
        public bool Is_Working { get; set; }
        public string Inserted_Host { get; set; }  = String.Empty;
        public decimal Inserted_User_ID { get; set; }
        public string Updated_Host { get; set; }  = String.Empty;
        public decimal Updated_User_ID { get; set; }
        public string Plant_Code { get; set; } = String.Empty;
    }
}