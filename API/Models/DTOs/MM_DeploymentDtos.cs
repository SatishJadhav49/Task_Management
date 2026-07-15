namespace Taskmanagement_API.Models.DTOs
{
    public class MM_Deployment_CreateDto
    {
        public string Feature_Module { get; set; } = String.Empty;
        public string Changes_Description { get; set; } = String.Empty;
        public string? Risk_Challenge { get; set; }
        public string Change_Type { get; set; } = String.Empty; // Minor / Major / Critical
        public decimal Requested_By { get; set; }
        public string? Plant_Code { get; set; }
        public string? Inserted_Host { get; set; }
        public decimal Inserted_User_ID { get; set; }
    }

    // Manager approve/reject.
    public class MM_Deployment_DecisionDto
    {
        public string Status { get; set; } = String.Empty;    // Approved / Rejected
        public string? Manager_Remark { get; set; }
        public decimal Updated_User_ID { get; set; }
        public string? Updated_Host { get; set; }
    }

    public class MM_Deployment_ListDto
    {
        public decimal Request_ID { get; set; }
        public string Feature_Module { get; set; } = String.Empty;
        public string Changes_Description { get; set; } = String.Empty;
        public string? Risk_Challenge { get; set; }
        public string Change_Type { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string? Manager_Remark { get; set; }

        public decimal Requested_By { get; set; }
        public string Requested_By_Name { get; set; } = String.Empty;
        public string Requested_By_Designation { get; set; } = String.Empty;

        public decimal? Approver_Manager_ID { get; set; }
        public string Approver_Manager_Name { get; set; } = String.Empty;

        public decimal? Approved_By { get; set; }
        public string Approved_By_Name { get; set; } = String.Empty;
        public string? Approved_Date { get; set; }

        public string Requested_Date { get; set; } = String.Empty;
    }
}
