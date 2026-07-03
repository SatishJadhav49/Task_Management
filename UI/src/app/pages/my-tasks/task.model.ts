export class MM_Task_Details {
  Task_ID: number = 0;
  Employee_ID: number = 0;
  Due_Date: string = '';
  Task_Description: string = '';
  Priority: string = '';
  Status: string = '';
  Inserted_Host: string = '';
  Inserted_User_ID: number = 0;
  Plant_Code: string = '';
  Updated_Host: string = '';
  Updated_User_ID: number = 0;
}

export class Task_Details_List {
  Task_ID: number = 0;
  Employee_ID: number = 0;
  Due_Date: string = '';
  Task_Description: string = '';
  Priority: string = '';
  Status: string = '';
  Responsibility: string = '';
  Manager_Remark: string | null = null;
  Remark_Updated_By: number | null = null;
  Remark_Updated_By_Name: string = '';
  Remark_Updated_Date: string | null = null;
}

export interface Task_Remark_Update {
  Manager_Remark: string;
  Updated_User_ID: number;
  Updated_Host: string;
}
