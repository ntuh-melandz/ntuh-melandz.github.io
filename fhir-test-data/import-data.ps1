# PowerShell Script to Import FHIR Test Data
# 用於將測試資料匯入 FHIR Server 的腳本

param(
    [Parameter(Mandatory = $false)]
    [string]$FhirServerUrl = "https://thas.mohw.gov.tw/v/r4/fhir",

    [Parameter(Mandatory = $false)]
    [string]$AccessToken = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FHIR 測試資料匯入工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "FHIR Server URL: $FhirServerUrl" -ForegroundColor Yellow
Write-Host ""

# 設定 Headers
$headers = @{
    "Content-Type" = "application/fhir+json"
}

if ($AccessToken -ne "") {
    $headers["Authorization"] = "Bearer $AccessToken"
    Write-Host "使用 Access Token 進行驗證" -ForegroundColor Green
}

# 取得腳本所在目錄
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# 定義要匯入的檔案列表 (注意順序：機構 -> 病患 -> 媒體/觀測/報告)
$dataFiles = @(
    "organization-ntuh.json",
    "patients.json",
    "vital-signs.json",
    "fundus-media.json",
    "fundus-diagnostic-reports.json",
    "observations.json",
    "conditions.json",
    "medication-requests.json"
)

$successCount = 0
$failCount = 0

foreach ($file in $dataFiles) {
    $filePath = Join-Path $scriptPath $file

    if (Test-Path $filePath) {
        Write-Host "正在匯入: $file" -ForegroundColor Cyan

        try {
            # 讀取 JSON 檔案
            $jsonContent = Get-Content $filePath -Raw -Encoding UTF8

            # 發送 POST 請求到 FHIR Server (加上 -AllowInsecureRedirect 解決轉址問題)
            $response = Invoke-RestMethod -Uri $FhirServerUrl -Method Post -Headers $headers -Body $jsonContent -AllowInsecureRedirect

            Write-Host "  ✓ $file 匯入成功" -ForegroundColor Green
            $successCount++

            # 顯示匯入的資源數量
            if ($response.entry) {
                Write-Host "    匯入了 $($response.entry.Count) 個資源" -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "  ✗ $file 匯入失敗" -ForegroundColor Red
            Write-Host "    錯誤訊息: $($_.Exception.Message)" -ForegroundColor Red
            
            # 嘗試讀取伺服器回應詳細錯誤
            if ($_.Exception.Response) {
                try {
                    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                    $errorResponse = $reader.ReadToEnd()
                    Write-Host "    Server Response: $errorResponse" -ForegroundColor Yellow
                }
                catch {}
            }
            
            $failCount++
        }

        Write-Host ""
    }
    else {
        # 只顯示黃色警告，不計入嚴重失敗（某些檔案可能不存在是正常的）
        # Write-Host "  ⚠ 找不到檔案: $file (跳過)" -ForegroundColor DarkGray
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "匯入完成!" -ForegroundColor Cyan
Write-Host "  成功: $successCount 個檔案" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "  失敗: $failCount 個檔案" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan
