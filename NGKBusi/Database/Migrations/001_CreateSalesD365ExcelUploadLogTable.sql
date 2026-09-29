-- ============================================================================
-- Sales D365 Excel Upload Log - Database Migration Script
-- ============================================================================
-- Purpose: Create Sales_D365ExcelUploadLog table untuk menyimpan history upload
-- Created: January 2024
-- ============================================================================

-- Check if table exists, if yes drop it (optional - untuk development)
-- IF OBJECT_ID('dbo.Sales_D365ExcelUploadLog', 'U') IS NOT NULL
-- DROP TABLE dbo.Sales_D365ExcelUploadLog;

-- Create main table
CREATE TABLE Sales_D365ExcelUploadLog (
    ID INT PRIMARY KEY IDENTITY(1,1),
    UploadType NVARCHAR(50) NOT NULL,
    FileName NVARCHAR(255),
    RowCount INT NOT NULL DEFAULT 0,
    UploadDateTime DATETIME NOT NULL DEFAULT GETDATE(),
    UploadedBy NVARCHAR(50),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    ErrorMessage NVARCHAR(MAX),
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create indexes untuk performa
CREATE INDEX IX_UploadDateTime ON Sales_D365ExcelUploadLog(UploadDateTime DESC);
CREATE INDEX IX_UploadType ON Sales_D365ExcelUploadLog(UploadType);
CREATE INDEX IX_UploadedBy ON Sales_D365ExcelUploadLog(UploadedBy);
CREATE INDEX IX_Status ON Sales_D365ExcelUploadLog(Status);
CREATE INDEX IX_Combined_Date_Status ON Sales_D365ExcelUploadLog(UploadDateTime DESC, Status);

-- Add constraints
ALTER TABLE Sales_D365ExcelUploadLog
ADD CONSTRAINT CK_Status CHECK (Status IN ('Success', 'Failed', 'Partial', 'Pending'));

ALTER TABLE Sales_D365ExcelUploadLog
ADD CONSTRAINT CK_UploadType CHECK (UploadType IN ('OrderLines', 'SalesBI', 'Sales', 'SalesPackingQuantity', 'Other'));

-- Insert sample data untuk testing (optional)
INSERT INTO Sales_D365ExcelUploadLog (UploadType, FileName, RowCount, UploadDateTime, UploadedBy, Status)
VALUES 
    ('OrderLines', '20240115120000.xlsx', 150, GETDATE(), 'E001234', 'Success'),
    ('SalesBI', '20240115113000.xlsx', 200, GETDATE(), 'E001235', 'Success'),
    ('Sales', '20240115110000.xlsx', 100, DATEADD(HOUR, -1, GETDATE()), 'E001234', 'Success');

-- Verify
SELECT * FROM Sales_D365ExcelUploadLog;
SELECT COUNT(*) as [Total Records] FROM Sales_D365ExcelUploadLog;

-- ============================================================================
-- Optional: Create views for reporting
-- ============================================================================

-- View untuk Daily Statistics
CREATE VIEW vw_Sales_D365ExcelUploadLog_Daily AS
SELECT 
    CAST(UploadDateTime AS DATE) as UploadDate,
    UploadType,
    COUNT(*) as UploadCount,
    SUM(RowCount) as TotalRowsImported,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount
FROM Sales_D365ExcelUploadLog
GROUP BY CAST(UploadDateTime AS DATE), UploadType;

-- View untuk User Statistics
CREATE VIEW vw_Sales_D365ExcelUploadLog_ByUser AS
SELECT 
    UploadedBy,
    COUNT(*) as TotalUploads,
    SUM(RowCount) as TotalRowsImported,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
    MAX(UploadDateTime) as LastUploadDateTime
FROM Sales_D365ExcelUploadLog
WHERE UploadedBy IS NOT NULL
GROUP BY UploadedBy;

-- View untuk Type Statistics
CREATE VIEW vw_Sales_D365ExcelUploadLog_ByType AS
SELECT 
    UploadType,
    COUNT(*) as TotalUploads,
    SUM(RowCount) as TotalRowsImported,
    CAST(AVG(CAST(RowCount AS FLOAT)) AS INT) as AvgRowsPerUpload,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
    CAST(SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as SuccessRate
FROM Sales_D365ExcelUploadLog
GROUP BY UploadType;

-- ============================================================================
-- Optional: Create stored procedures untuk common queries
-- ============================================================================

-- Stored Procedure: Get upload history
CREATE PROCEDURE sp_GetUploadHistory
    @Days INT = 30,
    @TopRecords INT = 100
AS
BEGIN
    SELECT TOP (@TopRecords)
        ID,
        UploadType,
        FileName,
        RowCount,
        UploadDateTime,
        UploadedBy,
        Status,
        ErrorMessage
    FROM Sales_D365ExcelUploadLog
    WHERE UploadDateTime >= DATEADD(DAY, -@Days, GETDATE())
    ORDER BY UploadDateTime DESC;
END;

-- Stored Procedure: Get statistics
CREATE PROCEDURE sp_GetUploadStatistics
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL
AS
BEGIN
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -1, GETDATE());
    IF @ToDate IS NULL SET @ToDate = GETDATE();
    
    SELECT 
        COUNT(*) as TotalUploads,
        SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessUploads,
        SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedUploads,
        SUM(RowCount) as TotalRowsImported,
        AVG(RowCount) as AvgRowsPerUpload,
        MIN(UploadDateTime) as FirstUploadDateTime,
        MAX(UploadDateTime) as LastUploadDateTime
    FROM Sales_D365ExcelUploadLog
    WHERE UploadDateTime BETWEEN @FromDate AND @ToDate;
END;

-- Stored Procedure: Get failed uploads
CREATE PROCEDURE sp_GetFailedUploads
    @Days INT = 30
AS
BEGIN
    SELECT 
        ID,
        UploadType,
        FileName,
        UploadDateTime,
        UploadedBy,
        ErrorMessage
    FROM Sales_D365ExcelUploadLog
    WHERE Status = 'Failed'
        AND UploadDateTime >= DATEADD(DAY, -@Days, GETDATE())
    ORDER BY UploadDateTime DESC;
END;

-- ============================================================================
-- Test Queries
-- ============================================================================

-- Test 1: Lihat semua log
SELECT * FROM Sales_D365ExcelUploadLog ORDER BY UploadDateTime DESC;

-- Test 2: Lihat daily statistics
SELECT * FROM vw_Sales_D365ExcelUploadLog_Daily ORDER BY UploadDate DESC;

-- Test 3: Lihat user statistics
SELECT * FROM vw_Sales_D365ExcelUploadLog_ByUser ORDER BY TotalUploads DESC;

-- Test 4: Lihat type statistics
SELECT * FROM vw_Sales_D365ExcelUploadLog_ByType;

-- Test 5: Execute stored procedure
EXEC sp_GetUploadHistory @Days = 30, @TopRecords = 50;
EXEC sp_GetUploadStatistics;
EXEC sp_GetFailedUploads @Days = 30;

-- ============================================================================
-- Cleanup (Optional - for development)
-- ============================================================================

-- Delete test data
-- DELETE FROM Sales_D365ExcelUploadLog WHERE UploadDateTime < DATEADD(MONTH, -12, GETDATE());

-- ============================================================================
-- END OF SCRIPT
-- ============================================================================
