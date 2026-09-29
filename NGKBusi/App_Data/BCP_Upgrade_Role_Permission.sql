-- ═══════════════════════════════════════════════════════════
-- BCP Database Migration: Rename and Upgrade BCP Permissions Table
-- Run this script on SQL Server (NGKBusi_Dev)
-- ═══════════════════════════════════════════════════════════

-- 1. Rename Table if the old one exists
IF EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Review_Permission' AND xtype='U')
BEGIN
    EXEC sp_rename 'HC_BCP_Review_Permission', 'HC_BCP_Role_permission';
    PRINT 'Table HC_BCP_Review_Permission renamed to HC_BCP_Role_permission successfully.';
END
GO

-- 2. Rename Column: IsAllowed -> IsAllowedReview
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Role_permission') AND name = 'IsAllowed')
BEGIN
    EXEC sp_rename 'HC_BCP_Role_permission.IsAllowed', 'IsAllowedReview', 'COLUMN';
    PRINT 'Column IsAllowed renamed to IsAllowedReview successfully.';
END
GO

-- 3. Rename Column: IsAllowedSubmit -> IsAllowedSubmitReview (if exists) or Add it
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Role_permission') AND name = 'IsAllowedSubmit')
BEGIN
    EXEC sp_rename 'HC_BCP_Role_permission.IsAllowedSubmit', 'IsAllowedSubmitReview', 'COLUMN';
    PRINT 'Column IsAllowedSubmit renamed to IsAllowedSubmitReview successfully.';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Role_permission') AND name = 'IsAllowedSubmitReview')
BEGIN
    ALTER TABLE HC_BCP_Role_permission ADD IsAllowedSubmitReview BIT NOT NULL DEFAULT 1;
    PRINT 'Column IsAllowedSubmitReview added successfully.';
END
GO

-- 4. Rename Column: IsCanActivate -> IsAllowedActivate (if exists) or Add it
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Role_permission') AND name = 'IsCanActivate')
BEGIN
    EXEC sp_rename 'HC_BCP_Role_permission.IsCanActivate', 'IsAllowedActivate', 'COLUMN';
    PRINT 'Column IsCanActivate renamed to IsAllowedActivate successfully.';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Role_permission') AND name = 'IsAllowedActivate')
BEGIN
    ALTER TABLE HC_BCP_Role_permission ADD IsAllowedActivate BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsAllowedActivate added successfully.';
END
GO

-- 5. Add Column: IsAllowedManageBCP (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('HC_BCP_Role_permission') AND name = 'IsAllowedManageBCP')
BEGIN
    ALTER TABLE HC_BCP_Role_permission ADD IsAllowedManageBCP BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsAllowedManageBCP added successfully.';
END
GO

-- Ensure default constraint values are populated correctly
-- Default for SrManager, DGM, DGMDeputy, Presdir should have Review and Submit review allowed.
-- Presdir should have Activate allowed as well.
UPDATE HC_BCP_Role_permission SET IsAllowedSubmitReview = 1 WHERE IsAllowedSubmitReview IS NULL;
UPDATE HC_BCP_Role_permission SET IsAllowedActivate = 1 WHERE RoleName = 'Presdir';
GO
