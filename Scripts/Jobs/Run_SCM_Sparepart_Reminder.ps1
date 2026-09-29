# ==============================================================================
# Script Name: Run_SCM_Sparepart_Reminder.ps1
# Description: Automated Job Scheduler script to trigger SCM Sparepart PO Reminders
# Target: GET http://localhost/NGKBusi/SCM/SparepartPORequest/SendReminders
# Usage: Execute via Windows Task Scheduler or cron every hour or daily at specified hours.
# ==============================================================================

$logFile = "C:\inetpub\wwwroot\ngk-portal\ngk-portal\App_Data\Logs\SCM_Reminder_Job.log"
$logDir = [System.IO.Path]::GetDirectoryName($logFile)
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$url = "http://localhost/NGKBusi/SCM/SparepartPORequest/SendReminders"

Add-Content -Path $logFile -Value "[$timestamp] Starting SCM Sparepart PO Reminder Job..."

try {
    $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 60
    $resultJson = $response | ConvertTo-Json -Compress
    Add-Content -Path $logFile -Value "[$timestamp] SCM Reminder Job completed successfully. Response: $resultJson"
    Write-Host "Reminder Job finished successfully: $resultJson"
} catch {
    $err = $_.Exception.Message
    Add-Content -Path $logFile -Value "[$timestamp] [ERROR] SCM Reminder Job failed: $err"
    Write-Error "Reminder Job failed: $err"
}
