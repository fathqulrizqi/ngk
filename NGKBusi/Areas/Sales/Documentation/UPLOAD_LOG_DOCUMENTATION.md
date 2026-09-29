# Dokumentasi Sistem Logging Upload Excel - Sales D365 Form

## Ringkasan Fitur

Sistem logging telah ditambahkan untuk melacak semua upload file Excel pada fungsi-fungsi berikut:
- **OrderLines** (Upload Sales Order Lines)
- **SalesBI** (Upload Sales BI)
- **Sales** (Upload Sales Data)

Setiap upload tercatat dalam database dengan informasi lengkap:
- Nama file yang diupload
- Tanggal dan waktu upload (dengan timestamp lengkap)
- User/NIK yang melakukan upload
- Jumlah baris data yang berhasil diimpor
- Status upload (Success/Failed)
- Pesan error (jika ada)

---

## Database Schema

### Tabel: Sales_D365ExcelUploadLog

Tabel ini menyimpan semua history upload dengan struktur sebagai berikut:

```sql
CREATE TABLE Sales_D365ExcelUploadLog (
    ID INT PRIMARY KEY IDENTITY(1,1),
    UploadType NVARCHAR(50) NOT NULL,        -- Tipe upload: OrderLines, SalesBI, Sales, dll
    FileName NVARCHAR(255),                   -- Nama file yang diupload
    RowCount INT,                             -- Jumlah baris yang diimpor
    UploadDateTime DATETIME NOT NULL,         -- Tanggal dan waktu upload
    UploadedBy NVARCHAR(50),                  -- User ID/NIK yang upload
    Status NVARCHAR(50),                      -- Success, Failed, Partial
    ErrorMessage NVARCHAR(MAX)                -- Pesan error jika ada
);
```

---

## Model Class

### Sales_D365ExcelUploadLog

Lokasi file: `NGKBusi\Areas\Sales\Models\D365ExcelForm.cs`

```csharp
public class Sales_D365ExcelUploadLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    /// <summary>
    /// Tipe upload (OrderLines, SalesBI, Sales, SalesPackingQuantity, dll)
    /// </summary>
    public string UploadType { get; set; }

    /// <summary>
    /// Nama file yang diupload
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Jumlah baris data yang berhasil diimpor
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Tanggal dan waktu upload
    /// </summary>
    public DateTime UploadDateTime { get; set; }

    /// <summary>
    /// User ID yang melakukan upload (NIK)
    /// </summary>
    public string UploadedBy { get; set; }

    /// <summary>
    /// Status upload (Success, Failed, Partial)
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Pesan error jika ada (optional)
    /// </summary>
    public string ErrorMessage { get; set; }
}
```

---

## Methods/Functions

### 1. SaveUploadLog (Private Helper)

**Lokasi:** `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`

**Deskripsi:** Method private untuk menyimpan log upload ke database.

**Signature:**
```csharp
private void SaveUploadLog(
    string uploadType, 
    string fileName, 
    int rowCount, 
    string status, 
    string errorMessage = null
)
```

**Parameter:**
- `uploadType` (string): Tipe upload (misal: "OrderLines", "SalesBI", "Sales")
- `fileName` (string): Nama file Excel yang diupload
- `rowCount` (int): Jumlah baris yang berhasil diimpor
- `status` (string): Status upload ("Success" atau "Failed")
- `errorMessage` (string, optional): Pesan error jika upload gagal

**Contoh Penggunaan:**
```csharp
// Ketika upload berhasil
SaveUploadLog("OrderLines", "20240115123456.xlsx", 150, "Success");

// Ketika upload gagal
SaveUploadLog("SalesBI", "20240115123456.xlsx", 0, "Failed", "File is empty or not uploaded");
```

**Catatan:** Method ini dilindungi dengan try-catch untuk memastikan bahwa error pada logging tidak mengganggu proses upload utama.

---

### 2. GetUploadHistory (Public API)

**Lokasi:** `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`

**Deskripsi:** Mengembalikan history/log upload dengan format JSON, diurutkan dari yang terbaru.

**HTTP Method:** POST

**URL:** `/Sales/D365ExcelForm/GetUploadHistory`

**Response Format:**
```json
{
    "status": 1,
    "data": [
        {
            "No": 1,
            "UploadType": "Sales",
            "FileName": "20240115123456.xlsx",
            "RowCount": 150,
            "UploadDateTime": "15/01/2024 12:34:56",
            "UploadedBy": "E001234",
            "Status": "Success",
            "ErrorMessage": "-"
        },
        {
            "No": 2,
            "UploadType": "OrderLines",
            "FileName": "20240115120000.xlsx",
            "RowCount": 75,
            "UploadDateTime": "15/01/2024 12:00:00",
            "UploadedBy": "E001235",
            "Status": "Success",
            "ErrorMessage": "-"
        }
    ],
    "total": 2
}
```

**Contoh AJAX Call:**
```javascript
$.ajax({
    type: 'POST',
    url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
    dataType: 'json',
    success: function(response) {
        if (response.status === 1) {
            console.log("Upload logs:", response.data);
            // Tampilkan ke table atau list
        }
    }
});
```

---

### 3. GetUploadStatistics (Public API)

**Lokasi:** `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`

**Deskripsi:** Mengembalikan statistik upload untuk hari ini dan bulan ini.

**HTTP Method:** POST

**URL:** `/Sales/D365ExcelForm/GetUploadStatistics`

**Response Format:**
```json
{
    "TodayTotal": 5,
    "ThisMonthTotal": 120,
    "TodaySuccess": 4,
    "TodayFailed": 1,
    "TotalRowsTodaySuccess": 450,
    "UploadTypeStats": [
        {
            "UploadType": "Sales",
            "Count": 45,
            "TotalRows": 3500
        },
        {
            "UploadType": "OrderLines",
            "Count": 35,
            "TotalRows": 2100
        },
        {
            "UploadType": "SalesBI",
            "Count": 40,
            "TotalRows": 4200
        }
    ]
}
```

**Contoh AJAX Call:**
```javascript
$.ajax({
    type: 'POST',
    url: '@Url.Action("GetUploadStatistics", "D365ExcelForm", new { area = "Sales" })',
    dataType: 'json',
    success: function(response) {
        console.log("Today uploads:", response.TodayTotal);
        console.log("Success today:", response.TodaySuccess);
        console.log("Total rows imported today:", response.TotalRowsTodaySuccess);
        
        // Tampilkan statistik di dashboard
    }
});
```

---

## Modified Functions

### OrderLines Function
- **Lokasi:** `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`
- **Perubahan:** Menambahkan `SaveUploadLog()` call untuk mencatat setiap upload
- **Kondisi Logging:**
  - ? **Success:** Ketika data berhasil diimpor ke database (count > 0)
  - ? **Failed:** Ketika tidak ada data atau file kosong

### SalesBI Function
- **Lokasi:** `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`
- **Perubahan:** Menambahkan `SaveUploadLog()` call untuk mencatat setiap upload
- **Kondisi Logging:** Sama seperti OrderLines

### Sales Function
- **Lokasi:** `NGKBusi\Areas\Sales\Controllers\D365ExcelFormController.cs`
- **Perubahan:** Menambahkan `SaveUploadLog()` call untuk mencatat setiap upload
- **Kondisi Logging:** Sama seperti OrderLines

---

## Entity Framework Configuration

Pastikan DbSet telah ditambahkan di `D365ExcelFormConnection`:

```csharp
public class D365ExcelFormConnection : DbContext
{
    public DbSet<Sales_D365ImporForm_OrderLines> Sales_D365ImporForm_OrderLines { get; set; }
    public DbSet<Sales_D365ImporForm_SalesBI> Sales_D365ImporForm_SalesBI { get; set; }
    public DbSet<Sales_D365ImporForm_Sales> Sales_D365ImporForm_Sales { get; set; }
    public DbSet<Sales_D365ImporForm_SalesPackingQuantity> Sales_D365ImporForm_SalesPackingQuantity { get; set; }
    public DbSet<V_Sales_Back_Order> V_Sales_Back_Order { get; set; }
    public DbSet<SCM_D365_Pickinglist> SCM_D365_Pickinglist { get; set; }
    public DbSet<Sales_D365ExcelUploadLog> Sales_D365ExcelUploadLog { get; set; }  // ? Ditambahkan
    
    public D365ExcelFormConnection()
    {
        this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["AxConnection"].ConnectionString;
    }
}
```

---

## Migration (Database Update)

### Opsi 1: Manual SQL (Recommended untuk production)

Jalankan script SQL berikut di database:

```sql
CREATE TABLE Sales_D365ExcelUploadLog (
    ID INT PRIMARY KEY IDENTITY(1,1),
    UploadType NVARCHAR(50) NOT NULL,
    FileName NVARCHAR(255),
    RowCount INT NOT NULL DEFAULT 0,
    UploadDateTime DATETIME NOT NULL,
    UploadedBy NVARCHAR(50),
    Status NVARCHAR(50),
    ErrorMessage NVARCHAR(MAX),
    CONSTRAINT PK_Sales_D365ExcelUploadLog PRIMARY KEY (ID)
);

-- Create index untuk performa query
CREATE INDEX IX_UploadDateTime ON Sales_D365ExcelUploadLog(UploadDateTime DESC);
CREATE INDEX IX_UploadType ON Sales_D365ExcelUploadLog(UploadType);
CREATE INDEX IX_UploadedBy ON Sales_D365ExcelUploadLog(UploadedBy);
```

### Opsi 2: Entity Framework Code First Migration

Jika menggunakan Code First Migrations:

```powershell
# Di Package Manager Console
Enable-Migrations
Add-Migration AddSalesD365ExcelUploadLog
Update-Database
```

---

## Usage Examples

### Contoh 1: Menampilkan Upload Log di View

**View File:** `NGKBusi\Areas\Sales\Views\D365ExcelForm\Index.cshtml`

```html
<!-- Tambahkan section untuk upload log -->
<div class="row">
    <div class="col-12">
        <div class="card mt-4">
            <div class="card-header bg-info">
                <div>
                    <a class="collapsed d-block" data-toggle="collapse" href="#collapse-upload-log" 
                       aria-expanded="true" aria-controls="collapse-upload-log">
                        Upload History
                        <i class="fa fa-chevron-down pull-right" style="float:right"></i>
                    </a>
                </div>
            </div>
            <div id="collapse-upload-log" class="collapse" aria-labelledby="collapse-upload-log">
                <div class="card-body">
                    <table id="uploadLogTable" class="table table-sm table-hover table-striped">
                        <thead>
                            <tr>
                                <th>No</th>
                                <th>Upload Type</th>
                                <th>File Name</th>
                                <th>Row Count</th>
                                <th>Upload Date Time</th>
                                <th>Uploaded By</th>
                                <th>Status</th>
                                <th>Error Message</th>
                            </tr>
                        </thead>
                        <tbody id="uploadLogBody">
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
$(document).ready(function() {
    // Load upload history saat page load
    loadUploadHistory();
    
    // Refresh setiap 5 menit
    setInterval(loadUploadHistory, 5 * 60 * 1000);
});

function loadUploadHistory() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
        dataType: 'json',
        success: function(response) {
            if (response.status === 1) {
                var tbody = $('#uploadLogBody');
                tbody.empty();
                
                $.each(response.data, function(index, log) {
                    var statusBadge = log.Status === 'Success' 
                        ? '<span class="badge badge-success">Success</span>'
                        : '<span class="badge badge-danger">Failed</span>';
                    
                    tbody.append('<tr>' +
                        '<td>' + log.No + '</td>' +
                        '<td>' + log.UploadType + '</td>' +
                        '<td>' + log.FileName + '</td>' +
                        '<td>' + log.RowCount + '</td>' +
                        '<td>' + log.UploadDateTime + '</td>' +
                        '<td>' + log.UploadedBy + '</td>' +
                        '<td>' + statusBadge + '</td>' +
                        '<td>' + log.ErrorMessage + '</td>' +
                        '</tr>');
                });
            }
        }
    });
}
</script>
```

### Contoh 2: Dashboard Statistics

```html
<div class="row">
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Today Uploads</h5>
                <p id="todayTotal" class="text-primary font-weight-bold" style="font-size: 1.5em;">0</p>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Success Today</h5>
                <p id="todaySuccess" class="text-success font-weight-bold" style="font-size: 1.5em;">0</p>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Failed Today</h5>
                <p id="todayFailed" class="text-danger font-weight-bold" style="font-size: 1.5em;">0</p>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Total Rows (Success)</h5>
                <p id="totalRows" class="text-info font-weight-bold" style="font-size: 1.5em;">0</p>
            </div>
        </div>
    </div>
</div>

<script>
$(document).ready(function() {
    loadStatistics();
});

function loadStatistics() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadStatistics", "D365ExcelForm", new { area = "Sales" })',
        dataType: 'json',
        success: function(response) {
            $('#todayTotal').text(response.TodayTotal);
            $('#todaySuccess').text(response.TodaySuccess);
            $('#todayFailed').text(response.TodayFailed);
            $('#totalRows').text(response.TotalRowsTodaySuccess.toLocaleString());
        }
    });
}
</script>
```

---

## Query Database untuk Analysis

### Lihat semua upload logs
```sql
SELECT * FROM Sales_D365ExcelUploadLog 
ORDER BY UploadDateTime DESC;
```

### Lihat upload berdasarkan user
```sql
SELECT UploadedBy, COUNT(*) as UploadCount, SUM(RowCount) as TotalRows
FROM Sales_D365ExcelUploadLog
GROUP BY UploadedBy
ORDER BY UploadCount DESC;
```

### Lihat upload berdasarkan tipe
```sql
SELECT UploadType, COUNT(*) as UploadCount, SUM(RowCount) as TotalRows
FROM Sales_D365ExcelUploadLog
WHERE UploadDateTime >= DATEADD(MONTH, -1, GETDATE())
GROUP BY UploadType;
```

### Lihat upload dengan error
```sql
SELECT * FROM Sales_D365ExcelUploadLog
WHERE Status = 'Failed'
ORDER BY UploadDateTime DESC;
```

### Statistik harian
```sql
SELECT CAST(UploadDateTime as DATE) as UploadDate, 
       COUNT(*) as TotalUploads,
       SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
       SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
       SUM(RowCount) as TotalRowsImported
FROM Sales_D365ExcelUploadLog
GROUP BY CAST(UploadDateTime as DATE)
ORDER BY UploadDate DESC;
```

---

## Testing

### Test Case 1: Successful Upload
1. Upload file Excel dengan data valid (misal 50 baris)
2. Verifikasi:
   - Upload berhasil dan data tersimpan
   - Log tersimpan di `Sales_D365ExcelUploadLog`
   - `Status` = "Success"
   - `RowCount` = 50
   - `ErrorMessage` = NULL

### Test Case 2: Empty File
1. Upload file Excel kosong
2. Verifikasi:
   - Upload gagal
   - Log tersimpan dengan `Status` = "Failed"
   - `ErrorMessage` contains "No data found to import"

### Test Case 3: Invalid File
1. Upload file yang bukan Excel atau corrupt
2. Verifikasi:
   - Terjadi exception, tapi log tetap tersimpan
   - `Status` = "Failed"
   - `ErrorMessage` contains error description

---

## Troubleshooting

### Log tidak tersimpan
- Periksa koneksi database
- Pastikan `D365ExcelFormConnection` sudah di-initialize dengan baik
- Check user memiliki permission INSERT pada table

### Error "The entity type is not part of the model"
- Pastikan `DbSet<Sales_D365ExcelUploadLog>` sudah ditambahkan ke `D365ExcelFormConnection`
- Rebuild solution

### Foreign Key Error
- Tabel `Sales_D365ExcelUploadLog` tidak memiliki FK, jadi seharusnya tidak ada error
- Jika ada error, clear DbContext dan rebuild

---

## Best Practices

1. **Jangan hardcode upload type** - Gunakan constant atau enum:
   ```csharp
   public enum UploadTypes
   {
        OrderLines,
        SalesBI,
        Sales,
        SalesPackingQuantity
   }
   ```

2. **Implementasikan automatic log cleanup** - Hapus log lama:
   ```csharp
   // Hapus log lebih dari 1 tahun
   var oldLogs = dbsi.Sales_D365ExcelUploadLog
       .Where(x => x.UploadDateTime < DateTime.Now.AddYears(-1))
       .ToList();
   dbsi.Sales_D365ExcelUploadLog.RemoveRange(oldLogs);
   dbsi.SaveChanges();
   ```

3. **Monitor failed uploads** - Buat alert untuk failed uploads
4. **Archive logs** - Backup logs lama untuk audit trail
5. **Use proper timezone** - Pertimbangkan timezone untuk `UploadDateTime`

---

## Support & Maintenance

Jika ada pertanyaan atau issue:
1. Check database untuk log entries
2. Verify koneksi database
3. Check DbContext initialization
4. Review error messages di log

---

**Last Updated:** January 2024
**Version:** 1.0
