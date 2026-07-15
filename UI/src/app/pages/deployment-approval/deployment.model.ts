export interface Deployment_Request {
  Request_ID: number;
  Feature_Module: string;
  Changes_Description: string;
  Risk_Challenge: string | null;
  Change_Type: string;
  Status: string;
  Manager_Remark: string | null;
  Requested_By: number;
  Requested_By_Name: string;
  Requested_By_Designation: string;
  Approver_Manager_ID: number | null;
  Approver_Manager_Name: string;
  Approved_By: number | null;
  Approved_By_Name: string;
  Approved_Date: string | null;
  Requested_Date: string;
}
