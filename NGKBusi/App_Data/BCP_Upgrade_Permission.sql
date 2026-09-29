-- ═══════════════════════════════════════════════════════════
-- BCP Database Upgrade: Review Permission Setup
-- Run this script on SQL Server (NGKBusi_Dev)
-- ═══════════════════════════════════════════════════════════

-- 1. Create HC_BCP_Review_Permission Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='HC_BCP_Review_Permission' AND xtype='U')
BEGIN
    CREATE TABLE HC_BCP_Review_Permission (
        ID              INT IDENTITY(1,1) PRIMARY KEY,
        RoleName        VARCHAR(50) NULL,    -- Role permitted (e.g. 'SrManager', 'DGM', 'Presdir')
        UserNIK         VARCHAR(20) NULL,    -- Specific NIK permitted (optional)
        IsAllowed       BIT DEFAULT 1,
        Created_At      DATETIME DEFAULT GETDATE()
    );
    
    PRINT 'Table HC_BCP_Review_Permission created successfully.';
END
GO

-- 2. Populate Default Permitted BCP Roles
-- Default permitted roles: SrManager, DGM, DGMDeputy, Presdir
IF NOT EXISTS (SELECT * FROM HC_BCP_Review_Permission)
BEGIN
    INSERT INTO HC_BCP_Review_Permission (RoleName, IsAllowed) VALUES
    ('SrManager', 1),
    ('DGM', 1),
    ('DGMDeputy', 1),
    ('Presdir', 1);
    
    PRINT 'Default review permissions seeded successfully.';
END
GO
