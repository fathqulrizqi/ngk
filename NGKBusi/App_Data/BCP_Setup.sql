-- ═══════════════════════════════════════════════════════════
-- BCP (Business Continuity Plan) Database Setup
-- NGK Portal — HC Area
-- Run this script on SQL Server
-- ═══════════════════════════════════════════════════════════

-- 1. HC_BCP_Organization
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Organization' AND xtype='U')
CREATE TABLE HC_BCP_Organization (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    NIK             VARCHAR(20) NOT NULL,
    Name            NVARCHAR(100),
    BCP_Role        VARCHAR(20) NOT NULL,
    DepartmentName  NVARCHAR(100),
    ParentID        INT NULL,
    IsActive        BIT DEFAULT 1,
    Created_At      DATETIME DEFAULT GETDATE(),
    Created_By      VARCHAR(20)
);
GO

-- 2. HC_BCP_Incident
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Incident' AND xtype='U')
CREATE TABLE HC_BCP_Incident (
    ID                  INT IDENTITY(1,1) PRIMARY KEY,
    IncidentNo          VARCHAR(20) NOT NULL,
    Title               NVARCHAR(200) NOT NULL,
    Description         NVARCHAR(MAX),
    Location            NVARCHAR(200),
    IncidentDate        DATETIME,
    Category            VARCHAR(50),
    Severity            VARCHAR(20),
    Status              INT DEFAULT 1,
    Created_By          VARCHAR(20),
    Created_At          DATETIME DEFAULT GETDATE(),
    ForwardedToPresdir  BIT DEFAULT 0,
    Activated_By        VARCHAR(20) NULL,
    Activated_At        DATETIME NULL,
    Activation_Decision VARCHAR(20) NULL,
    Resolved_At         DATETIME NULL,
    Resolved_Note       NVARCHAR(MAX) NULL
);
GO

-- 3. HC_BCP_Incident_Photo
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Incident_Photo' AND xtype='U')
CREATE TABLE HC_BCP_Incident_Photo (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    IncidentID      INT NOT NULL,
    FileName        NVARCHAR(255),
    FilePath        NVARCHAR(500),
    Uploaded_At     DATETIME DEFAULT GETDATE(),
    Uploaded_By     VARCHAR(20)
);
GO

-- 4. HC_BCP_Assessment
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Assessment' AND xtype='U')
CREATE TABLE HC_BCP_Assessment (
    ID                  INT IDENTITY(1,1) PRIMARY KEY,
    IncidentID          INT NOT NULL,
    AssessorNIK         VARCHAR(20) NOT NULL,
    AssessorName        NVARCHAR(100),
    DepartmentName      NVARCHAR(100),
    IsImpacted          BIT NULL,
    ImpactDescription   NVARCHAR(MAX),
    TotalScore          DECIMAL(5,2) NULL,
    Status              INT DEFAULT 0,
    Created_At          DATETIME DEFAULT GETDATE(),
    Submitted_At        DATETIME NULL
);
GO

-- 5. HC_BCP_Assessment_Detail
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Assessment_Detail' AND xtype='U')
CREATE TABLE HC_BCP_Assessment_Detail (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    AssessmentID    INT NOT NULL,
    CriteriaID      INT NOT NULL,
    Score           INT NULL,
    Notes           NVARCHAR(500)
);
GO

-- 6. HC_BCP_Assessment_Criteria
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Assessment_Criteria' AND xtype='U')
CREATE TABLE HC_BCP_Assessment_Criteria (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(500),
    Weight          DECIMAL(5,2),
    SortOrder       INT,
    IsActive        BIT DEFAULT 1
);
GO

-- 7. HC_BCP_Review
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Review' AND xtype='U')
CREATE TABLE HC_BCP_Review (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    IncidentID      INT NOT NULL,
    ReviewerNIK     VARCHAR(20) NOT NULL,
    ReviewerRole    VARCHAR(20),
    ReviewNote      NVARCHAR(MAX),
    Decision        VARCHAR(30),
    Created_At      DATETIME DEFAULT GETDATE()
);
GO

-- 8. HC_BCP_Notification_Log
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Notification_Log' AND xtype='U')
CREATE TABLE HC_BCP_Notification_Log (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    IncidentID      INT NOT NULL,
    RecipientNIK    VARCHAR(20),
    Channel         VARCHAR(20),
    Message         NVARCHAR(MAX),
    IsSent          BIT DEFAULT 0,
    SentAt          DATETIME NULL,
    ErrorMessage    NVARCHAR(500) NULL
);
GO

-- 9. HC_BCP_Activation_Log
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Activation_Log' AND xtype='U')
CREATE TABLE HC_BCP_Activation_Log (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    IncidentID      INT NOT NULL,
    Action          VARCHAR(20),
    ActionBy        VARCHAR(20),
    ActionAt        DATETIME DEFAULT GETDATE(),
    Note            NVARCHAR(MAX)
);
GO

-- ═══════════════════════════════════════════════════════════
-- DEFAULT ASSESSMENT CRITERIA
-- ═══════════════════════════════════════════════════════════
INSERT INTO HC_BCP_Assessment_Criteria (Name, Description, Weight, SortOrder, IsActive) VALUES
('Operational / Production Impact', 'Impact on production lines and daily operations', 25.00, 1, 1),
('Financial Impact', 'Estimated financial loss or additional cost incurred', 20.00, 2, 1),
('Supply Chain Impact', 'Impact on suppliers, raw materials, and logistics', 15.00, 3, 1),
('Human Resources / Safety Impact', 'Impact on employee safety and workforce availability', 20.00, 4, 1),
('Reputation / Customer Impact', 'Impact on brand reputation and customer relations', 10.00, 5, 1),
('Estimated Recovery Time', 'How long to fully recover from this incident', 10.00, 6, 1);
GO

-- ═══════════════════════════════════════════════════════════
-- DUMMY BCP ORGANIZATION DATA
-- Replace NIK values with actual employee NIKs
-- ═══════════════════════════════════════════════════════════
INSERT INTO HC_BCP_Organization (NIK, Name, BCP_Role, DepartmentName, ParentID, IsActive, Created_At, Created_By) VALUES
('PRESDIR01', 'President Director', 'Presdir', 'BOD', NULL, 1, GETDATE(), 'SYSTEM'),
('DGM001', 'Deputy General Manager', 'DGM', 'Management', NULL, 1, GETDATE(), 'SYSTEM'),
('DGMDEP01', 'DGM Deputy Officer', 'DGMDeputy', 'Management', NULL, 1, GETDATE(), 'SYSTEM'),
('SRM001', 'Sr. Manager Production', 'SrManager', 'Production', 2, 1, GETDATE(), 'SYSTEM'),
('SRM002', 'Sr. Manager SCM', 'SrManager', 'SCM', 2, 1, GETDATE(), 'SYSTEM'),
('SRM003', 'Sr. Manager HC & GA', 'SrManager', 'HC', 2, 1, GETDATE(), 'SYSTEM'),
('MGR001', 'Manager Production 1', 'Manager', 'Production', 4, 1, GETDATE(), 'SYSTEM'),
('MGR002', 'Manager Production 2', 'Manager', 'Production', 4, 1, GETDATE(), 'SYSTEM'),
('MGR003', 'Manager IT', 'Manager', 'IT', 5, 1, GETDATE(), 'SYSTEM'),
('MGR004', 'Manager Purchasing', 'Manager', 'Purchasing', 5, 1, GETDATE(), 'SYSTEM'),
('MGR005', 'Manager HC', 'Manager', 'HC', 6, 1, GETDATE(), 'SYSTEM'),
('MGR006', 'Manager HSE', 'Manager', 'HSE', 6, 1, GETDATE(), 'SYSTEM');
GO

PRINT 'BCP Database setup completed successfully.';
GO
