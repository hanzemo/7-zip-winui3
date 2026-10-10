# Windows PowerShell 里跑
# 生成自签证书 + 导出 pfx + base64

$cert = New-SelfSignedCertificate `
  -Type Custom `
  -Subject "CN=hanzemo" `
  -KeyUsage DigitalSignature `
  -FriendlyName "SevenZipWinUI" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -KeyExportPolicy Exportable `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

Write-Host "SUBJECT: $($cert.Subject)"
Write-Host "THUMBPRINT: $($cert.Thumbprint)"

$pwd = ConvertTo-SecureString -String "SevenZip123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "$env:USERPROFILE\Desktop\SevenZip.pfx" -Password $pwd
Export-Certificate -Cert $cert -FilePath "$env:USERPROFILE\Desktop\SevenZip.cer"

$bytes = [IO.File]::ReadAllBytes("$env:USERPROFILE\Desktop\SevenZip.pfx")
$b64 = [Convert]::ToBase64String($bytes)
$b64 | Out-File "$env:USERPROFILE\Desktop\SevenZip.pfx.b64" -Encoding ASCII

Write-Host ""
Write-Host "===== 填到 GitHub Secrets ====="
Write-Host "BASE64_ENCODED_PFX = 打开 $env:USERPROFILE\Desktop\SevenZip.pfx.b64 复制全部内容"
Write-Host "PFX_PASSWORD = SevenZip123!"
Write-Host ""
Write-Host "证书文件：$env:USERPROFILE\Desktop\SevenZip.cer（发给用户装信任）"
