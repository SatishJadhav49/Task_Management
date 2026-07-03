-- Team support: employee <-> team membership.
-- MM_Team_List (Team_ID, Team_Name, SORTORDER) already exists with teams added manually.
-- A developer/lead belongs to one team, a manager can belong to multiple teams,
-- so membership lives in a separate mapping table.
USE [PQ_Task_Management]
GO

IF OBJECT_ID('dbo.MM_Employee_Team', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_Employee_Team (
        Employee_Team_ID  numeric(18, 0) IDENTITY(1,1) NOT NULL,
        -- Types must exactly match the referenced PK columns for the FKs:
        -- MM_Employee.Employee_ID is decimal(18,0), MM_Team_List.Team_ID is numeric(18,0).
        Employee_ID       decimal(18, 0) NOT NULL,
        Team_ID           numeric(18, 0) NOT NULL,
        Inserted_Host     nvarchar(100)  NULL,
        Inserted_User_ID  numeric(18, 0) NULL,
        Inserted_Date     datetime       NULL,
        CONSTRAINT PK_MM_Employee_Team PRIMARY KEY CLUSTERED (Employee_Team_ID),
        CONSTRAINT UQ_MM_Employee_Team UNIQUE (Employee_ID, Team_ID),
        CONSTRAINT FK_MM_Employee_Team_Employee FOREIGN KEY (Employee_ID) REFERENCES dbo.MM_Employee (Employee_ID),
        CONSTRAINT FK_MM_Employee_Team_Team FOREIGN KEY (Team_ID) REFERENCES dbo.MM_Team_List (Team_ID)
    );
END
GO

-- Seed: put every active employee that has no team yet into team 1 (PQ) so the
-- app keeps showing data. Adjust real memberships from the User Management page.
INSERT INTO dbo.MM_Employee_Team (Employee_ID, Team_ID, Inserted_Host, Inserted_User_ID, Inserted_Date)
SELECT e.Employee_ID, 1, 'migration', NULL, GETDATE()
FROM dbo.MM_Employee e
WHERE (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
  AND NOT EXISTS (SELECT 1 FROM dbo.MM_Employee_Team et WHERE et.Employee_ID = e.Employee_ID)
GO
