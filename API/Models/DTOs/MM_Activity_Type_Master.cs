namespace Taskmanagement_API.Models.DTOs
{

    public class MM_Activity_Type_MasterListDto
    {
        public decimal Activity_Type_ID { get; set; }
        public string Activity_Name { get; set; } = String.Empty;
        public decimal SORTORDER { get; set; }
    }
}