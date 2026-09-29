-- ═══════════════════════════════════════════════════════════
-- BCP Database Upgrade: Department Mapping
-- Run this script on SQL Server (NGKBusi_Dev)
-- ═══════════════════════════════════════════════════════════

-- 1. Create HC_BCP_Department Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Department' AND xtype='U')
BEGIN
    CREATE TABLE HC_BCP_Department (
        ID              INT IDENTITY(1,1) PRIMARY KEY,
        DeptName        NVARCHAR(100) NOT NULL,
        Code            VARCHAR(20) NULL,
        IsActive        BIT DEFAULT 1,
        Created_At      DATETIME DEFAULT GETDATE()
    );
END
GO

-- 2. Populate BCP Departments / Sections (18 Sections from the layout)
IF NOT EXISTS (SELECT * FROM HC_BCP_Department)
BEGIN
    INSERT INTO HC_BCP_Department (DeptName, Code, IsActive) VALUES
    ('EOM/OES SALES', 'EOM/OES', 1),
    ('IAM SALES', 'IAM', 1),
    ('IC', 'IC', 1),
    ('PROCUREMENT', 'PROC', 1),
    ('LOGISTIC', 'LOG', 1),
    ('PP', 'PP', 1),
    ('MSI', 'MSI', 1),
    ('SP', 'SP', 1),
    ('PC', 'PC', 1),
    ('PE', 'PE', 1),
    ('MAINT', 'MAINT', 1),
    ('QC', 'QC', 1),
    ('QA', 'QA', 1),
    ('HSE', 'HSE', 1),
    ('IT', 'IT', 1),
    ('FA', 'FA', 1),
    ('GA', 'GA', 1),
    ('HR', 'HR', 1);
END
GO

-- 3. Add DepartmentID to HC_BCP_Area
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Area') AND name = 'DepartmentID')
BEGIN
    ALTER TABLE HC_BCP_Area ADD DepartmentID INT NULL;
    
    -- Add Foreign Key
    ALTER TABLE HC_BCP_Area ADD CONSTRAINT FK_HC_BCP_Area_Department FOREIGN KEY (DepartmentID) REFERENCES HC_BCP_Department(ID);
END
GO

-- 4. Map Existing Areas to Departments
-- Existing Area IDs:
-- 1: Pruduction Line 1
-- 2: Production Line 2
-- 3: IT & Infrastructure
-- 4: Supply Chain
-- 5: Human Capital
-- 6: Finance
-- 7: Quality Control
-- 8: Engineering

UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'PE') WHERE ID = 1; -- PE
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'PC') WHERE ID = 2; -- PC
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'IT') WHERE ID = 3; -- IT
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'LOG') WHERE ID = 4; -- LOGISTIC
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'HR') WHERE ID = 5; -- HR
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'FA') WHERE ID = 6; -- FA
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'QC') WHERE ID = 7; -- QC
UPDATE HC_BCP_Area SET DepartmentID = (SELECT ID FROM HC_BCP_Department WHERE Code = 'MAINT') WHERE ID = 8; -- MAINT
GO

PRINT 'BCP Department mapping database upgrade completed successfully.';
GO
