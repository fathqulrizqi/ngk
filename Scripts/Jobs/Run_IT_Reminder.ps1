# ==============================================================================
# Script Name: Run_IT_Reminder.ps1
# Description: Automated Job Scheduler script to trigger IT & Legal Permit Reminders
# Target: GET http://localhost/NGKBusi/IT/Reminder/GetReminderTask
# Usage: Execute via Windows Task Scheduler or cron every hour.
# ==============================================================================

$logFile = "C:\inetpub\wwwroot\ngk-portal\ngk-portal\App_Data\Logs\IT_Reminder_Job.log"
$logDir = [System.IO.Path]::GetDirectoryName($logFile)
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$url = "http://localhost/NGKBusi/IT/Reminder/GetReminderTask"

Add-Content -Path $logFile -Value "[$timestamp] Starting IT & Legal Permit Reminder Job..."

try {
    $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 60
    $resultJson = $response | ConvertTo-Json -Compress
    Add-Content -Path $logFile -Value "[$timestamp] IT Reminder Job completed successfully. Response: $resultJson"
    Write-Host "IT Reminder Job finished successfully: $resultJson"
} catch {
    $err = $_.Exception.Message
    Add-Content -Path $logFile -Value "[$timestamp] [ERROR] IT Reminder Job failed: $err"
    Write-Error "IT Reminder Job failed: $err"
}
