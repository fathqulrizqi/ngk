# Upload Excel Log Implementation Guide

## ?? Overview

Sistem logging telah ditambahkan untuk melacak semua upload file Excel pada 3 fungsi utama:
- **OrderLines** - Upload Sales Order Lines
- **SalesBI** - Upload Sales BI Data  
- **Sales** - Upload Sales Data

Setiap upload akan dicatat ke database dengan informasi:
? Nama file
? Jumlah baris (row count)
? Tanggal & waktu upload
? User yang upload (NIK)
? Status (Success/Failed)
? Error message (jika ada)

---

## ?? Quick Start Implementation

### Step 1: Apply Database Migration

Jalankan SQL script untuk membuat table:

```sql
-- File: NGKBusi\Database\Migrations\001_CreateSalesD365ExcelUploadLogTable.sql
-- Copy dan jalankan di SQL Server Management Studio
```

**Script mencakup:**
- Create table `Sales_D365ExcelUploadLog`
- Create indexes untuk performa
- Create views untuk reporting
- Create stored procedures

### Step 2: Verify Model & DbContext

Pastikan model sudah ditambahkan:

```csharp
// File: NGKBusi\Areas\Sales\Models\D365ExcelForm.cs
// Check: Class Sales_D365ExcelUploadLog ada
// Check: DbSet<Sales_D365ExcelUploadLog> ada di D365ExcelFormConnection
```

### Step 3: Verify Controller Changes

Pastikan helper method dan logging calls ada:

```csharp
// File: NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs
// Check: SaveUploadLog() method ada
// Check: GetUploadHistory() method ada
// Check: GetUploadStatistics() method ada
// Check: SaveUploadLog() call ada di OrderLines(), SalesBI(), Sales()
```

### Step 4: Test Upload

1. Navigate ke upload page
2. Upload file Excel
3. Check database untuk log entry:
   ```sql
   SELECT * FROM Sales_D365ExcelUploadLog 
   ORDER BY UploadDateTime DESC;
   ```

---

## ?? Available Methods

### Public Methods (For UI Integration)

#### GetUploadHistory()
```csharp
POST /Sales/D365ExcelForm/GetUploadHistory
Response: JSON array dengan log entries (max 100)
```

**Usage:**
```javascript
$.ajax({
    type: 'POST',
    url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
    success: function(response) {
        console.log(response.data); // Array of upload logs
    }
});
```

#### GetUploadStatistics()
```csharp
POST /Sales/D365ExcelForm/GetUploadStatistics
Response: JSON dengan statistik harian dan bulanan
```

**Usage:**
```javascript
$.ajax({
    type: 'POST',
    url: '@Url.Action("GetUploadStatistics", "D365ExcelForm", new { area = "Sales" })',
    success: function(response) {
        console.log('Today:', response.TodayTotal);
        console.log('Success:', response.TodaySuccess);
    }
});
```

---

## ?? Implementation Examples

### Example 1: Add Upload History Table to Existing View

```html
<!-- Add to NGKBusi\Areas\Sales\Views\D365ExcelForm\Index.cshtml -->

<div class="card mt-4">
    <div class="card-header bg-info">
        <h5>Upload History</h5>
    </div>
    <div class="card-body">
        <table id="uploadLogTable" class="table table-sm table-hover">
            <thead>
                <tr>
                    <th>Type</th>
                    <th>File</th>
                    <th>Rows</th>
                    <th>Date</th>
                    <th>User</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody></tbody>
        </table>
    </div>
</div>

<script>
$(document).ready(function() {
    loadUploadLog();
});

function loadUploadLog() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
        success: function(resp) {
            var tbody = $('#uploadLogTable tbody');
            tbody.empty();
            $.each(resp.data, function(i, log) {
                var badge = log.Status === 'Success' 
                    ? '<span class="badge badge-success">Success</span>'
                    : '<span class="badge badge-danger">Failed</span>';
                tbody.append('<tr><td>' + log.UploadType + '</td><td>' + log.FileName + 
                    '</td><td>' + log.RowCount + '</td><td>' + log.UploadDateTime + 
                    '</td><td>' + log.UploadedBy + '</td><td>' + badge + '</td></tr>');
            });
        }
    });
}
</script>
```

### Example 2: Dashboard Statistics

```html
<div class="row">
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h6>Today's Uploads</h6>
                <h2 id="todayTotal">0</h2>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h6>Success</h6>
                <h2 id="successCount">0</h2>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h6>Failed</h6>
                <h2 id="failedCount">0</h2>
            </div>
        </div>
    </div>
</div>

<script>
$(document).ready(function() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadStatistics", "D365ExcelForm", new { area = "Sales" })',
        success: function(stats) {
            $('#todayTotal').text(stats.TodayTotal);
            $('#successCount').text(stats.TodaySuccess);
            $('#failedCount').text(stats.TodayFailed);
        }
    });
});
</script>
```

---

## ?? Files Created/Modified

### New Files
- ? `NGKBusi\Database\Migrations\001_CreateSalesD365ExcelUploadLogTable.sql`
- ? `NGKBusi\Areas\Sales\Views\D365ExcelForm\UploadLog.cshtml`
- ? `NGKBusi\Areas\Sales\Documentation\UPLOAD_LOG_DOCUMENTATION.md`

### Modified Files
- ? `NGKBusi\Areas\Sales\Models\D365ExcelForm.cs`
  - Added: `Sales_D365ExcelUploadLog` model
  - Updated: `D365ExcelFormConnection` DbSet
  
- ? `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`
  - Added: `SaveUploadLog()` helper method
  - Added: `GetUploadHistory()` public method
  - Added: `GetUploadStatistics()` public method
  - Updated: `OrderLines()` function
  - Updated: `SalesBI()` function
  - Updated: `Sales()` function

---

## ?? Testing Checklist

- [ ] Database table created successfully
- [ ] Upload file Excel (OrderLines, SalesBI, atau Sales)
- [ ] Log entry appears in database
- [ ] GetUploadHistory() returns data
- [ ] GetUploadStatistics() returns correct counts
- [ ] Status shows "Success" or "Failed" correctly
- [ ] Row count matches actual imported rows
- [ ] User (NIK) is correctly recorded
- [ ] Timestamp is accurate

---

## ?? SQL Queries for Analysis

### View all logs
```sql
SELECT * FROM Sales_D365ExcelUploadLog 
ORDER BY UploadDateTime DESC;
```

### Statistics by user
```sql
SELECT UploadedBy, COUNT(*) as Count, SUM(RowCount) as TotalRows
FROM Sales_D365ExcelUploadLog
GROUP BY UploadedBy
ORDER BY Count DESC;
```

### Statistics by type
```sql
SELECT UploadType, COUNT(*) as Count, SUM(RowCount) as TotalRows
FROM Sales_D365ExcelUploadLog
GROUP BY UploadType;
```

### Failed uploads
```sql
SELECT * FROM Sales_D365ExcelUploadLog
WHERE Status = 'Failed'
ORDER BY UploadDateTime DESC;
```

---

## ? Troubleshooting

### Issue: Logs not being saved
**Solution:** 
- Check database connection
- Verify DbSet added to context
- Check user permissions on table

### Issue: User ID showing as NULL
**Solution:**
- Ensure User.Identity is properly initialized
- Check User is authenticated before upload

### Issue: Old logs accumulating too much space
**Solution:** Create a cleanup job
```csharp
// Delete logs older than 1 year
var oldLogs = dbsi.Sales_D365ExcelUploadLog
    .Where(x => x.UploadDateTime < DateTime.Now.AddYears(-1))
    .ToList();
dbsi.Sales_D365ExcelUploadLog.RemoveRange(oldLogs);
dbsi.SaveChanges();
```

---

## ?? Security Notes

- Logs contain NIK/User ID for accountability
- ErrorMessage logged for debugging
- Consider masking sensitive data in error messages
- Archive old logs for compliance/audit

---

## ?? Support

For issues or questions:
1. Check database logs: `SELECT * FROM Sales_D365ExcelUploadLog`
2. Check application logs
3. Review documentation in `UPLOAD_LOG_DOCUMENTATION.md`

---

## ?? Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 2024 | Initial implementation |

---

**Status:** ? Ready for Production

Last Updated: January 2024
