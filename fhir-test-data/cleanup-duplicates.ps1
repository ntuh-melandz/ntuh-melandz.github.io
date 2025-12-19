$fhirUrl = 'https://thas.mohw.gov.tw/v/r4/fhir'
$patientIds = @('patient-001', 'patient-002', 'patient-003', 'patient-004', 'patient-005')
$totalDeleted = 0

foreach ($patientId in $patientIds) {
    Write-Host 'Processing patient:' $patientId -ForegroundColor Cyan

    $url = "$fhirUrl/Observation?patient=$patientId&_count=500"
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -Headers @{ 'Accept' = 'application/fhir+json' }
    } catch {
        Write-Host '  Error fetching:' $_.Exception.Message -ForegroundColor Red
        continue
    }

    if (-not $response.entry) {
        Write-Host '  No observations found' -ForegroundColor Gray
        continue
    }

    Write-Host '  Found' $response.entry.Count 'observations' -ForegroundColor Yellow

    $grouped = @{}

    foreach ($entry in $response.entry) {
        $obs = $entry.resource
        $code = if ($obs.code.coding) { $obs.code.coding[0].display } else { 'Unknown' }
        $dt = $obs.effectiveDateTime
        $val = if ($obs.valueQuantity) { "$($obs.valueQuantity.value)$($obs.valueQuantity.unit)" } else { '' }
        if ($obs.component) {
            foreach ($c in $obs.component) {
                if ($c.valueQuantity) { $val += "$($c.valueQuantity.value)$($c.valueQuantity.unit)_" }
            }
        }
        $key = "$code|$dt|$val"
        if (-not $grouped.ContainsKey($key)) { $grouped[$key] = @() }
        $grouped[$key] += $obs.id
    }

    foreach ($key in $grouped.Keys) {
        $ids = $grouped[$key]
        if ($ids.Count -gt 1) {
            Write-Host '  Duplicate found:' $key.Split('|')[0] '(' $ids.Count 'items)' -ForegroundColor Yellow
            Write-Host '    Keeping ID:' $ids[0] -ForegroundColor Green
            for ($i = 1; $i -lt $ids.Count; $i++) {
                $delUrl = "$fhirUrl/Observation/$($ids[$i])"
                try {
                    Invoke-RestMethod -Uri $delUrl -Method Delete -Headers @{ 'Accept' = 'application/fhir+json' }
                    Write-Host '    Deleted ID:' $ids[$i] -ForegroundColor Red
                    $totalDeleted++
                } catch {
                    Write-Host '    Failed to delete:' $ids[$i] '-' $_.Exception.Message -ForegroundColor Magenta
                }
            }
        }
    }
}

Write-Host ''
Write-Host 'Done! Deleted' $totalDeleted 'duplicate observations.' -ForegroundColor Cyan
