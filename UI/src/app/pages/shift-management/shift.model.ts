export interface Shift {
  Shift_ID: number;
  Shift_Name: string;
  Shift_Code: string;
  Start_Time: string;
  End_Time: string;
  Is_General: boolean;
  SORTORDER: number;
}

export interface ShiftRosterMember {
  Employee_ID: number;
  Employee_Name: string;
  Employee_No: string;
  Designation_ID: number;
  Designation_Name: string;
  Shift_ID: number;
  Shift_Name: string;
  Override_Count: number;
}

export interface ShiftDay {
  Shift_Date: string;
  Day_Label: string;
  Weekly_Shift_ID: number;
  Effective_Shift_ID: number;
  Is_Override: boolean;
}
