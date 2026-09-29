// ============================================================================
// Upload Log Integration Examples - JavaScript Snippets
// ============================================================================
// Location: Add these snippets to your View/JavaScript files
// ============================================================================

// =========================
// 1. BASIC INITIALIZATION
// =========================

function initializeUploadLog() {
    // Load statistics on page load
    loadUploadStatistics();
    
    // Load history on page load
    loadUploadHistory();
    
    // Auto-refresh every 5 minutes
    setInterval(function() {
        loadUploadStatistics();
        loadUploadHistory();
    }, 5 * 60 * 1000);
}

// Call on document ready
$(document).ready(function() {
    initializeUploadLog();
});

// =========================
// 2. LOAD STATISTICS
// =========================

function loadUploadStatistics() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadStatistics", "D365ExcelForm", new { area = "Sales" })',
        dataType: 'json',
        success: function(response) {
            // Update dashboard cards
            $('#statTodayTotal').text(response.TodayTotal);
            $('#statTodaySuccess').text(response.TodaySuccess);
            $('#statTodayFailed').text(response.TodayFailed);
            $('#statTotalRows').text(response.TotalRowsTodaySuccess);
            $('#statThisMonth').text(response.ThisMonthTotal);
            
            console.log('Statistics loaded successfully');
        },
        error: function(xhr, status, error) {
            console.log('Error loading statistics:', error);
        }
    });
}

// =========================
// 3. LOAD UPLOAD HISTORY
// =========================

function loadUploadHistory(filterType = '', filterStatus = '') {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
        dataType: 'json',
        success: function(response) {
            if (response.status === 1) {
                displayUploadHistory(response.data, filterType, filterStatus);
                console.log('Upload history loaded:', response.data.length + ' records');
            } else {
                console.log('Error loading history:', response.msg);
            }
        },
        error: function(xhr, status, error) {
            console.log('Error fetching history:', error);
        }
    });
}

// =========================
// 4. DISPLAY HISTORY IN TABLE
// =========================

function displayUploadHistory(data, filterType = '', filterStatus = '') {
    // Filter data based on criteria
    var filteredData = data;
    
    if (filterType) {
        filteredData = filteredData.filter(x => x.UploadType === filterType);
    }
    
    if (filterStatus) {
        filteredData = filteredData.filter(x => x.Status === filterStatus);
    }
    
    // Build table rows
    var tbody = $('#uploadLogTableBody');
    tbody.empty();
    
    if (filteredData.length === 0) {
        tbody.html('<tr><td colspan="8" class="text-center text-muted">No upload logs found</td></tr>');
        return;
    }
    
    filteredData.forEach(function(log) {
        // Create status badge
        var statusBadge = '';
        if (log.Status === 'Success') {
            statusBadge = '<span class="badge badge-success">? Success</span>';
        } else if (log.Status === 'Failed') {
            statusBadge = '<span class="badge badge-danger">? Failed</span>';
        } else {
            statusBadge = '<span class="badge badge-secondary">' + log.Status + '</span>';
        }
        
        // Create error message cell
        var errorCell = log.ErrorMessage && log.ErrorMessage !== '-' 
            ? '<small class="text-danger d-block">' + truncateText(log.ErrorMessage, 50) + '</small>'
            : '<span class="text-muted">-</span>';
        
        // Build row
        var row = '<tr>' +
            '<td>' + log.No + '</td>' +
            '<td><strong class="text-primary">' + log.UploadType + '</strong></td>' +
            '<td><code class="bg-light">' + log.FileName + '</code></td>' +
            '<td class="text-right"><strong>' + log.RowCount.toLocaleString() + '</strong></td>' +
            '<td>' + log.UploadDateTime + '</td>' +
            '<td>' + log.UploadedBy + '</td>' +
            '<td>' + statusBadge + '</td>' +
            '<td>' + errorCell + '</td>' +
            '</tr>';
        
        tbody.append(row);
    });
}

// =========================
// 5. FILTER FUNCTIONS
// =========================

function filterByUploadType(type) {
    var status = $('#filterStatus').val();
    loadUploadHistory(type, status);
}

function filterByStatus(status) {
    var type = $('#filterType').val();
    loadUploadHistory(type, status);
}

function clearFilters() {
    $('#filterType').val('');
    $('#filterStatus').val('');
    loadUploadHistory();
}

// =========================
// 6. HELPER FUNCTIONS
// =========================

function truncateText(text, length) {
    if (text.length > length) {
        return text.substring(0, length) + '...';
    }
    return text;
}

function formatDate(dateString) {
    var options = { year: 'numeric', month: '2-digit', day: '2-digit', 
                    hour: '2-digit', minute: '2-digit', second: '2-digit' };
    return new Date(dateString).toLocaleDateString('id-ID', options);
}

function formatNumber(number) {
    return number.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// =========================
// 7. CHART INITIALIZATION
// =========================

var uploadTypeChart = null;
var successRateChart = null;

function initializeCharts(stats) {
    if (stats.UploadTypeStats && stats.UploadTypeStats.length > 0) {
        createUploadTypeChart(stats.UploadTypeStats);
        createSuccessRateChart(stats.UploadTypeStats);
    }
}

function createUploadTypeChart(data) {
    var ctx = document.getElementById('uploadTypeChart');
    
    if (!ctx) return; // Chart element not found
    
    if (uploadTypeChart) {
        uploadTypeChart.destroy();
    }
    
    uploadTypeChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map(x => x.UploadType),
            datasets: [
                {
                    label: 'Upload Count',
                    data: data.map(x => x.Count),
                    backgroundColor: 'rgba(54, 162, 235, 0.8)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                },
                {
                    label: 'Total Rows',
                    data: data.map(x => x.TotalRows),
                    backgroundColor: 'rgba(75, 192, 75, 0.8)',
                    borderColor: 'rgba(75, 192, 75, 1)',
                    borderWidth: 1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
}

function createSuccessRateChart(data) {
    var ctx = document.getElementById('successRateChart');
    
    if (!ctx) return; // Chart element not found
    
    if (successRateChart) {
        successRateChart.destroy();
    }
    
    successRateChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.map(x => x.UploadType),
            datasets: [{
                data: data.map(x => x.Count),
                backgroundColor: [
                    'rgba(255, 99, 132, 0.8)',
                    'rgba(54, 162, 235, 0.8)',
                    'rgba(255, 206, 86, 0.8)',
                    'rgba(75, 192, 75, 0.8)',
                    'rgba(153, 102, 255, 0.8)'
                ],
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 75, 1)',
                    'rgba(153, 102, 255, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });
}

// =========================
// 8. EXPORT TO EXCEL
// =========================

function exportUploadHistoryToExcel() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
        success: function(response) {
            if (response.status === 1) {
                downloadExcel(response.data);
            }
        }
    });
}

function downloadExcel(data) {
    var csv = 'No,Upload Type,File Name,Row Count,Upload DateTime,Uploaded By,Status,Error Message\n';
    
    data.forEach(function(log) {
        csv += [
            log.No,
            log.UploadType,
            '"' + log.FileName + '"',
            log.RowCount,
            log.UploadDateTime,
            log.UploadedBy,
            log.Status,
            '"' + (log.ErrorMessage || '') + '"'
        ].join(',') + '\n';
    });
    
    var blob = new Blob([csv], { type: 'text/csv' });
    var url = window.URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = 'upload-history-' + new Date().toISOString().slice(0, 10) + '.csv';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
}

// =========================
// 9. REAL-TIME NOTIFICATIONS
// =========================

function checkNewUploads() {
    $.ajax({
        type: 'POST',
        url: '@Url.Action("GetUploadHistory", "D365ExcelForm", new { area = "Sales" })',
        success: function(response) {
            if (response.data.length > 0) {
                var lastLog = response.data[0];
                
                // Check if upload is recent (within last minute)
                var uploadTime = new Date(lastLog.UploadDateTime);
                var now = new Date();
                var diff = (now - uploadTime) / 1000; // Convert to seconds
                
                if (diff < 60) { // Less than 1 minute old
                    showNotification(lastLog);
                }
            }
        }
    });
}

function showNotification(log) {
    var message = log.UploadType + ': ' + log.FileName + ' - ' + log.Status;
    var alertClass = log.Status === 'Success' ? 'alert-success' : 'alert-danger';
    
    var alertHtml = '<div class="alert ' + alertClass + ' alert-dismissible fade show" role="alert">' +
        '<strong>' + log.Status + '!</strong> ' + message +
        '<button type="button" class="close" data-dismiss="alert"><span>&times;</span></button>' +
        '</div>';
    
    $('#notificationContainer').prepend(alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow', function() {
            $(this).remove();
        });
    }, 5000);
}

// =========================
// 10. COMPLETE SETUP EXAMPLE
// =========================

/*
// In your View (Index.cshtml), add:

<!-- Notification Container -->
<div id="notificationContainer"></div>

<!-- Statistics Cards -->
<div class="row mb-4">
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5>Today's Uploads</h5>
                <h3 id="statTodayTotal">0</h3>
            </div>
        </div>
    </div>
    <!-- More cards... -->
</div>

<!-- Filters -->
<div class="form-row mb-3">
    <div class="col-md-4">
        <select id="filterType" class="form-control" onchange="filterByUploadType(this.value)">
            <option value="">All Types</option>
            <option value="OrderLines">Order Lines</option>
            <option value="SalesBI">Sales BI</option>
            <option value="Sales">Sales</option>
        </select>
    </div>
    <div class="col-md-4">
        <select id="filterStatus" class="form-control" onchange="filterByStatus(this.value)">
            <option value="">All Status</option>
            <option value="Success">Success</option>
            <option value="Failed">Failed</option>
        </select>
    </div>
    <div class="col-md-4">
        <button class="btn btn-primary" onclick="clearFilters()">Clear Filters</button>
        <button class="btn btn-success" onclick="exportUploadHistoryToExcel()">Export</button>
    </div>
</div>

<!-- History Table -->
<table class="table table-sm">
    <thead>
        <tr>
            <th>No</th>
            <th>Type</th>
            <th>File</th>
            <th>Rows</th>
            <th>Date</th>
            <th>User</th>
            <th>Status</th>
            <th>Error</th>
        </tr>
    </thead>
    <tbody id="uploadLogTableBody">
    </tbody>
</table>

<!-- Scripts -->
<script src="https://cdn.jsdelivr.net/npm/chart.js@3"></script>
<script src="~/Scripts/upload-log.js"></script>
*/
