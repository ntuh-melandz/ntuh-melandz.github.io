$uri = "https://thas.mohw.gov.tw/v/r4/sim/WzIsIiIsIiIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMSwiIl0/fhir/AuditEvent?_count=20&_sort=-date"

Write-Host "Fetching AuditEvents from FHIR Server..."
Write-Host "URL: $uri"
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers @{
        "Accept" = "application/fhir+json"
    }
    
    $data = $response.Content | ConvertFrom-Json
    
    Write-Host "Total AuditEvents: $($data.total)"
    Write-Host "====================================="
    Write-Host ""
    
    if ($data.entry) {
        foreach ($entry in $data.entry) {
            $event = $entry.resource
            $recorded = $event.recorded
            $action = $event.action
            $agentName = $event.agent[0].name
            $patientRef = $event.entity[0].what.reference
            $patientName = $event.entity[0].name
            
            Write-Host "時間: $recorded"
            Write-Host "動作: $action (Read)"
            Write-Host "來源: $agentName"
            Write-Host "病患: $patientRef ($patientName)"
            Write-Host "-------------------------------------"
        }
    }
    else {
        Write-Host "No AuditEvents found."
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)"
}
