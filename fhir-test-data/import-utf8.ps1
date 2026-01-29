# PowerShell Script to Import FHIR Test Data with UTF-8 Encoding
# 用於將測試資料匯入 FHIR Server 的腳本（修正 UTF-8 編碼問題）

param(
    [Parameter(Mandatory = $false)]
    [string]$FhirServerUrl = "https://thas.mohw.gov.tw/v/r4/fhir"
)

# Force UTF-8 encoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FHIR Test Data Import (UTF-8 Fixed)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "FHIR Server URL: $FhirServerUrl" -ForegroundColor Yellow
Write-Host ""

# Get script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Define files to import (order matters: Organization -> Patient -> others)
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
$totalResources = 0

foreach ($file in $dataFiles) {
    $filePath = Join-Path $scriptPath $file

    if (Test-Path $filePath) {
        Write-Host "Importing: $file" -ForegroundColor Cyan

        try {
            # Read JSON file with UTF-8 encoding
            $jsonContent = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)

            # Convert to UTF-8 byte array for proper encoding in HTTP request
            $utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes($jsonContent)

            # Parse JSON to check type
            $jsonObj = $jsonContent | ConvertFrom-Json

            if ($jsonObj.resourceType -eq "Bundle" -and $jsonObj.type -eq "transaction") {
                # POST Transaction Bundle to root
                $uri = $FhirServerUrl

                $response = Invoke-WebRequest -Uri $uri -Method Post `
                    -ContentType "application/fhir+json; charset=utf-8" `
                    -Body $utf8Bytes `
                    -UseBasicParsing

                $responseObj = $response.Content | ConvertFrom-Json

                if ($responseObj.entry) {
                    $resourceCount = $responseObj.entry.Count
                    Write-Host "  OK: $file ($resourceCount resources)" -ForegroundColor Green
                    $totalResources += $resourceCount
                } else {
                    Write-Host "  OK: $file" -ForegroundColor Green
                }
            }
            else {
                # Single resource - use PUT if has ID
                if ($jsonObj.id) {
                    $uri = "$FhirServerUrl/$($jsonObj.resourceType)/$($jsonObj.id)"
                    $response = Invoke-WebRequest -Uri $uri -Method Put `
                        -ContentType "application/fhir+json; charset=utf-8" `
                        -Body $utf8Bytes `
                        -UseBasicParsing
                }
                else {
                    $uri = "$FhirServerUrl/$($jsonObj.resourceType)"
                    $response = Invoke-WebRequest -Uri $uri -Method Post `
                        -ContentType "application/fhir+json; charset=utf-8" `
                        -Body $utf8Bytes `
                        -UseBasicParsing
                }
                Write-Host "  OK: $file" -ForegroundColor Green
                $totalResources++
            }

            $successCount++
        }
        catch {
            Write-Host "  FAILED: $file" -ForegroundColor Red
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
            $failCount++
        }

        Write-Host ""
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Import Complete!" -ForegroundColor Cyan
Write-Host "  Files: $successCount success, $failCount failed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
Write-Host "  Total Resources: $totalResources" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
