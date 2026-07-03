export class Activity_Type_List
{
    Activity_Type_ID: number = 0;
    Activity_Name: string = "";
}

export class MM_Activity_Details
{
    Activity_ID: number = 0;
    Activity_Type_ID: number = 0;
    Employee_ID: number = 0;
    Description: string = "";
    Activity_Date: string = "";
    Add_In_Mail:boolean = false;
    Inserted_Host: string = "";
    Inserted_User_ID: number = 0;
    Plant_Code: string = "";
    Updated_Host: string = "";
    Updated_User_ID: number = 0;
}

export class Activity_Details_List
{
    Activity_ID: number = 0;
    Activity_Type_ID: number = 0
    Activity_Name: string = "";
    Employee_ID: number = 0
    Description: string = "";
    Activity_Date: string = ""
    Add_In_Mail:boolean = false;
}

export class MM_Daily_Status
{
    Status_ID: number = 0;
    Employee_ID: number = 0;
    Date: string = "";
    In_Time: string = "";
    Out_Time: string = "";
    Status: string = "";
    Remark: string = "";
    Is_Working: boolean = true;
    Inserted_Host: string = "";
    Inserted_User_ID: number = 0;
    Updated_Host: string = "";
    Updated_User_ID: number = 0;
    Plant_Code: string = "";
}