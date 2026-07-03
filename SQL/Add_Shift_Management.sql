-- Shift Management: weekly shift scheduling per team, with mid-week day overrides.
-- Column types must match referenced PKs exactly:
--   MM_Employee.Employee_ID = decimal(18,0), MM_Team_List.Team_ID = numeric(18,0).
USE [PQ_Task_Management]
GO

/* ---------------------------------------------------------------------------
   MM_Shift_Master — the fixed set of shifts. "General Shift" is the default /
   unassigned pool employees start in; 1st/2nd/3rd are the working shifts.
--------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.MM_Shift_Master', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_Shift_Master (
        Shift_ID    numeric(18, 0) IDENTITY(1,1) NOT NULL,
        Shift_Name  nvarchar(50)  NOT NULL,
        Shift_Code  nvarchar(20)  NULL,
        Start_Time  nvarchar(10)  NULL,
        End_Time    nvarchar(10)  NULL,
        Is_General  bit           NOT NULL CONSTRAINT DF_MM_Shift_Master_IsGeneral DEFAULT (0),
        SORTORDER   numeric(18, 0) NOT NULL CONSTRAINT DF_MM_Shift_Master_Sort DEFAULT (0),
        CONSTRAINT PK_MM_Shift_Master PRIMARY KEY CLUSTERED (Shift_ID)
    );

    SET IDENTITY_INSERT dbo.MM_Shift_Master ON;
    INSERT INTO dbo.MM_Shift_Master (Shift_ID, Shift_Name, Shift_Code, Start_Time, End_Time, Is_General, SORTORDER)
    VALUES
        (1, 'General Shift', 'GEN', '09:00', '17:30', 1, 1),
        (2, '1st Shift',     'S1',  '06:00', '14:30', 0, 2),
        (3, '2nd Shift',     'S2',  '14:30', '23:00', 0, 3),
        (4, '3rd Shift',     'S3',  '23:00', '06:00', 0, 4);
    SET IDENTITY_INSERT dbo.MM_Shift_Master OFF;
END
GO

/* ---------------------------------------------------------------------------
   MM_Shift_Schedule — one row per employee per week (Monday start). This is
   what the weekly drag-drop board reads/writes. Absence of a row = General.
--------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.MM_Shift_Schedule', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_Shift_Schedule (
        Schedule_ID       numeric(18, 0) IDENTITY(1,1) NOT NULL,
        Employee_ID       decimal(18, 0) NOT NULL,
        Team_ID           numeric(18, 0) NOT NULL,
        Week_Start_Date   date           NOT NULL,   -- always the Monday of the week
        Shift_ID          numeric(18, 0) NOT NULL,
        Inserted_Host     nvarchar(100)  NULL,
        Inserted_User_ID  numeric(18, 0) NULL,
        Inserted_Date     datetime       NULL,
        Updated_Host      nvarchar(100)  NULL,
        Updated_User_ID   numeric(18, 0) NULL,
        Updated_Date      datetime       NULL,
        CONSTRAINT PK_MM_Shift_Schedule PRIMARY KEY CLUSTERED (Schedule_ID),
        CONSTRAINT UQ_MM_Shift_Schedule UNIQUE (Employee_ID, Week_Start_Date),
        CONSTRAINT FK_MM_Shift_Schedule_Employee FOREIGN KEY (Employee_ID) REFERENCES dbo.MM_Employee (Employee_ID),
        CONSTRAINT FK_MM_Shift_Schedule_Team FOREIGN KEY (Team_ID) REFERENCES dbo.MM_Team_List (Team_ID),
        CONSTRAINT FK_MM_Shift_Schedule_Shift FOREIGN KEY (Shift_ID) REFERENCES dbo.MM_Shift_Master (Shift_ID)
    );
END
GO

/* ---------------------------------------------------------------------------
   MM_Shift_Day_Override — mid-week per-day changes. A row here overrides the
   weekly assignment for that single date only. No row = use the weekly shift.
--------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.MM_Shift_Day_Override', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_Shift_Day_Override (
        Override_ID       numeric(18, 0) IDENTITY(1,1) NOT NULL,
        Employee_ID       decimal(18, 0) NOT NULL,
        Team_ID           numeric(18, 0) NOT NULL,
        Shift_Date        date           NOT NULL,
        Shift_ID          numeric(18, 0) NOT NULL,
        Inserted_Host     nvarchar(100)  NULL,
        Inserted_User_ID  numeric(18, 0) NULL,
        Inserted_Date     datetime       NULL,
        Updated_Host      nvarchar(100)  NULL,
        Updated_User_ID   numeric(18, 0) NULL,
        Updated_Date      datetime       NULL,
        CONSTRAINT PK_MM_Shift_Day_Override PRIMARY KEY CLUSTERED (Override_ID),
        CONSTRAINT UQ_MM_Shift_Day_Override UNIQUE (Employee_ID, Shift_Date),
        CONSTRAINT FK_MM_Shift_Day_Override_Employee FOREIGN KEY (Employee_ID) REFERENCES dbo.MM_Employee (Employee_ID),
        CONSTRAINT FK_MM_Shift_Day_Override_Team FOREIGN KEY (Team_ID) REFERENCES dbo.MM_Team_List (Team_ID),
        CONSTRAINT FK_MM_Shift_Day_Override_Shift FOREIGN KEY (Shift_ID) REFERENCES dbo.MM_Shift_Master (Shift_ID)
    );
END
GO
