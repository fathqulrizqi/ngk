# File: test_smtp.ps1
# Cara jalankan: Buka PowerShell, lalu ketik .\test_smtp.ps1

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls11 -bor [System.Net.SecurityProtocolType]::Tls

# Ignore SSL Certificate warning jika self-signed / hostname beda (opsional untuk testing)
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$smtpServer = "mail.ngkbusi.com"
$smtpPort = 587
$senderEmail = "ngkportal-notification@ngkbusi.com"
$senderPassword = "100%NGKbusi!"
$recipientEmail = "ikhsan.sholihin@niterragroup.com" # Ganti dengan email penerima untuk testing

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Testing Kirim Email via SMTP: $smtpServer : $smtpPort" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

try {
    $smtp = New-Object System.Net.Mail.SmtpClient($smtpServer, $smtpPort)
    $smtp.EnableSsl = $true
    $smtp.UseDefaultCredentials = $false
    $smtp.Credentials = New-Object System.Net.NetworkCredential($senderEmail, $senderPassword)
    $smtp.DeliveryMethod = [System.Net.Mail.SmtpDeliveryMethod]::Network
    $smtp.Timeout = 60000

    $mail = New-Object System.Net.Mail.MailMessage
    $mail.From = New-Object System.Net.Mail.MailAddress($senderEmail, "Test SMTP Portal")
    $mail.To.Add($recipientEmail)
    $mail.Subject = "Test Email SMTP dari Server - " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $mail.Body = "Ini adalah email uji coba untuk memverifikasi koneksi SMTP."

    Write-Host "Mengirim email ke $recipientEmail ..." -ForegroundColor Yellow
    $smtp.Send($mail)
    Write-Host "[BERHASIL] Email berhasil terkirim!" -ForegroundColor Green
}
catch {
    Write-Host "[GAGAL] Error saat kirim email:" -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor DarkRed
}
