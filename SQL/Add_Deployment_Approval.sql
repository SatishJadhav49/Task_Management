-- Deployment Approval: developers / team leads raise a deployment request that
-- routes to their reporting manager for approve / reject (with remark).
-- Employee_ID columns are decimal(18,0) to match MM_Employee.Employee_ID.
USE [PQ_Task_Management]
GO

IF OBJECT_ID('dbo.MM_Deployment_Request', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_Deployment_Request (
        Request_ID           numeric(18, 0) IDENTITY(1,1) NOT NULL,
        Feature_Module       nvarchar(200)  NOT NULL,
        Changes_Description  nvarchar(max)  NOT NULL,
        Risk_Challenge       nvarchar(max)  NULL,            -- optional
        Change_Type          nvarchar(20)   NOT NULL,        -- Minor / Major / Critical
        Requested_By         decimal(18, 0) NOT NULL,        -- Employee who raised it
        Approver_Manager_ID  decimal(18, 0) NULL,            -- requester's reporting manager
        Status               nvarchar(20)   NOT NULL
            CONSTRAINT DF_MM_Deployment_Request_Status DEFAULT ('Pending'), -- Pending / Approved / Rejected
        Manager_Remark       nvarchar(max)  NULL,
        Approved_By          decimal(18, 0) NULL,
        Approved_Date        datetime       NULL,
        Plant_Code           nvarchar(15)   NULL,
        Inserted_Host        nvarchar(100)  NULL,
        Inserted_User_ID     decimal(18, 0) NULL,
        Inserted_Date        datetime       NULL,
        Updated_Host         nvarchar(100)  NULL,
        Updated_User_ID      decimal(18, 0) NULL,
        Updated_Date         datetime       NULL,
        CONSTRAINT PK_MM_Deployment_Request PRIMARY KEY CLUSTERED (Request_ID),
        CONSTRAINT FK_MM_Deployment_Request_Requester FOREIGN KEY (Requested_By) REFERENCES dbo.MM_Employee (Employee_ID),
        CONSTRAINT FK_MM_Deployment_Request_Manager FOREIGN KEY (Approver_Manager_ID) REFERENCES dbo.MM_Employee (Employee_ID),
        CONSTRAINT FK_MM_Deployment_Request_Approver FOREIGN KEY (Approved_By) REFERENCES dbo.MM_Employee (Employee_ID)
    );
END
GO
