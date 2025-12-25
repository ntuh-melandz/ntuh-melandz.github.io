using SmartOnFhirApp.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartOnFhirApp.Services;

public class FhirClientService
{
    private readonly HttpClient _httpClient;
    private readonly SmartAuthService _authService;

    public FhirClientService(HttpClient httpClient, SmartAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    private async Task<HttpClient> GetAuthenticatedHttpClientAsync()
    {
        var token = await _authService.GetStoredAccessTokenAsync();
        
        // 在 WASM 中，我們直接修改注入的 HttpClient 標頭（或建立新請求）
        // 注意：在 WASM 中 HttpClient 通常是單例/Scoped，
        // 這裡為了安全，每次請求都重新設定標頭
        _httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrEmpty(token) 
            ? new AuthenticationHeaderValue("Bearer", token) 
            : null;

        if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/fhir+json"))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
        }

        // 移除可能導致 CORS 預檢失敗的自訂標頭
        // 某些 FHIR Server 對於未預期的標頭會回傳 401 或 CORS 錯誤
        if (_httpClient.DefaultRequestHeaders.Contains("X-Requested-With"))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-Requested-With");
        }

        return _httpClient;
    }

    public async Task<Patient?> GetPatientAsync(string fhirBaseUrl, string patientId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Patient/{patientId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var patient = await response.Content.ReadFromJsonAsync<Patient>();
                return patient;
            }
            else
            {
                Console.WriteLine($"Failed to get patient: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting patient: {ex.Message}");
        }

        return null;
    }

    public async Task<(List<Patient>? Patients, int Total, string? RequestUrl)> GetPatientsAsync(string fhirBaseUrl, int count = 20, string? name = null, int offset = 0, string? organizationId = null)
    {
        string? debugUrl = null;
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var patients = new List<Patient>();
            int totalCount = 0;

            // 1. Search by Name with pagination
            var nameUrl = $"{fhirBaseUrl}/Patient?_count={count}&_include=Patient:organization&_total=accurate";
            if (offset > 0)
            {
                nameUrl += $"&_getpagesoffset={offset}";
            }
            if (!string.IsNullOrEmpty(organizationId))
            {
                nameUrl += $"&organization={organizationId}";
            }
            if (!string.IsNullOrEmpty(name))
            {
                nameUrl += $"&name={Uri.EscapeDataString(name)}";
            }
            
            debugUrl = nameUrl; // Capture for debugging
            var nameTask = client.GetAsync(nameUrl);
            Task<HttpResponseMessage>? idTask = null;

            // 2. Search by ID
            if (!string.IsNullOrEmpty(name) && !name.Contains(" "))
            {
                var idUrl = $"{fhirBaseUrl}/Patient?_id={Uri.EscapeDataString(name)}&_include=Patient:organization";
                idTask = client.GetAsync(idUrl);
            }

            await Task.WhenAll(new List<Task> { nameTask, idTask ?? Task.CompletedTask });
            
            var organizations = new Dictionary<string, string>();

            async Task<int> ProcessBundle(HttpResponseMessage response)
            {
                int bundleTotal = 0;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    
                    // 取得 total 欄位
                    if (doc.RootElement.TryGetProperty("total", out var totalElement))
                    {
                        bundleTotal = totalElement.GetInt32();
                    }
                    
                    if (doc.RootElement.TryGetProperty("entry", out var entryArray))
                    {
                        foreach (var entry in entryArray.EnumerateArray())
                        {
                            if (entry.TryGetProperty("resource", out var resource))
                            {
                                if (resource.TryGetProperty("resourceType", out var type) && type.GetString() == "Patient")
                                {
                                    var patient = JsonSerializer.Deserialize<Patient>(resource.GetRawText());
                                    if (patient != null && !patients.Any(p => p.Id == patient.Id))
                                    {
                                        patients.Add(patient);
                                    }
                                }
                                else if (resource.TryGetProperty("resourceType", out var orgType) && orgType.GetString() == "Organization")
                                {
                                    var org = JsonSerializer.Deserialize<Organization>(resource.GetRawText());
                                    if (org != null && !string.IsNullOrEmpty(org.Id) && !string.IsNullOrEmpty(org.Name))
                                    {
                                        organizations[org.Id] = org.Name;
                                    }
                                }
                            }
                        }
                    }
                }
                return bundleTotal;
            }

            // Process Name Results
            totalCount = await ProcessBundle(nameTask.Result);
            
            // Process ID Results
            if (idTask != null)
            {
                await ProcessBundle(idTask.Result);
            }

            // Map Organization Names to Patients
            foreach (var patient in patients)
            {
                if (patient.ManagingOrganization != null && !string.IsNullOrEmpty(patient.ManagingOrganization.ReferenceValue))
                {
                    var orgId = patient.ManagingOrganization.ReferenceValue.Split('/').Last();
                    if (organizations.TryGetValue(orgId, out var orgName))
                    {
                        patient.ManagingOrganization.Display = orgName;
                    }
                }
            }
            
            
            return (patients, totalCount, debugUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting patients: {ex.Message}");
        }

        return (null, 0, debugUrl);
    }

    public async Task<List<Observation>?> GetObservationsAsync(string fhirBaseUrl, string patientId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Observation?patient={patientId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var bundle = await response.Content.ReadFromJsonAsync<Bundle<Observation>>();
                return bundle?.Entry?.Select(e => e.Resource).Where(r => r != null).ToList()!;
            }
            else
            {
                Console.WriteLine($"Failed to get observations: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting observations: {ex.Message}");
        }

        return null;
    }

    public async Task<List<Condition>?> GetConditionsAsync(string fhirBaseUrl, string patientId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Condition?patient={patientId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var bundle = await response.Content.ReadFromJsonAsync<Bundle<Condition>>();
                return bundle?.Entry?.Select(e => e.Resource).Where(r => r != null).ToList()!;
            }
            else
            {
                Console.WriteLine($"Failed to get conditions: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting conditions: {ex.Message}");
        }

        return null;
    }

    public async Task<List<MedicationRequest>?> GetMedicationRequestsAsync(string fhirBaseUrl, string patientId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/MedicationRequest?patient={patientId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var bundle = await response.Content.ReadFromJsonAsync<Bundle<MedicationRequest>>();
                return bundle?.Entry?.Select(e => e.Resource).Where(r => r != null).ToList()!;
            }
            else
            {
                Console.WriteLine($"Failed to get medication requests: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting medication requests: {ex.Message}");
        }

        return null;
    }

    public async Task<Organization?> GetOrganizationAsync(string fhirBaseUrl, string organizationId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Organization/{organizationId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var organization = await response.Content.ReadFromJsonAsync<Organization>();
                return organization;
            }
            else
            {
                Console.WriteLine($"Failed to get organization: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting organization: {ex.Message}");
        }

        return null;
    }

    public string GetDisplayName(HumanName? name)
    {
        if (name == null) return "Unknown";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(name.Family))
        {
            parts.Add(name.Family);
        }

        if (name.Given != null && name.Given.Any())
        {
            parts.AddRange(name.Given);
        }

        return parts.Any() ? string.Join(" ", parts) : "Unknown";
    }

    public string GetCodeDisplay(CodeableConcept? concept)
    {
        if (concept == null) return "Unknown";

        if (!string.IsNullOrEmpty(concept.Text))
        {
            return concept.Text;
        }

        if (concept.Coding != null && concept.Coding.Any())
        {
            var firstCoding = concept.Coding.First();
            return firstCoding.Display ?? firstCoding.Code ?? "Unknown";
        }

        return "Unknown";
    }

    public string GetDosageDisplay(MedicationRequest med)
    {
        if (med.DosageInstruction == null || !med.DosageInstruction.Any())
            return "-";

        var dosage = med.DosageInstruction.First();
        var parts = new List<string>();

        // 1. 優先使用 text
        if (!string.IsNullOrEmpty(dosage.Text))
        {
            return dosage.Text;
        }

        // 2. 嘗試從 doseAndRate 取得劑量
        if (dosage.DoseAndRate?.Any() == true)
        {
            var doseRate = dosage.DoseAndRate.First();
            if (doseRate.DoseQuantity != null)
            {
                parts.Add($"{doseRate.DoseQuantity.Value} {doseRate.DoseQuantity.Unit}");
            }
            else if (doseRate.DoseRange != null)
            {
                var low = doseRate.DoseRange.Low;
                var high = doseRate.DoseRange.High;
                if (low != null && high != null)
                {
                    parts.Add($"{low.Value}-{high.Value} {high.Unit}");
                }
            }
        }

        // 3. 嘗試從 timing 取得頻率
        if (dosage.Timing?.Repeat != null)
        {
            var repeat = dosage.Timing.Repeat;
            if (repeat.Frequency.HasValue && repeat.Period.HasValue)
            {
                var periodUnit = repeat.PeriodUnit switch
                {
                    "d" => "天",
                    "h" => "小時",
                    "wk" => "週",
                    "mo" => "月",
                    _ => repeat.PeriodUnit
                };
                parts.Add($"每{repeat.Period}{periodUnit} {repeat.Frequency}次");
            }
        }
        else if (dosage.Timing?.Code != null)
        {
            parts.Add(GetCodeDisplay(dosage.Timing.Code));
        }

        // 4. 給藥途徑
        if (dosage.Route != null)
        {
            parts.Add(GetCodeDisplay(dosage.Route));
        }

        return parts.Any() ? string.Join(", ", parts) : "-";
    }
    public async Task<Organization?> CreateOrganizationAsync(string fhirBaseUrl, Organization organization)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Organization";

            var response = await client.PostAsJsonAsync(url, organization);

            if (response.IsSuccessStatusCode)
            {
                var createdOrg = await response.Content.ReadFromJsonAsync<Organization>();
                return createdOrg;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to create organization: {response.StatusCode} {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating organization: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdatePatientOrganizationAsync(string fhirBaseUrl, string patientId, string organizationId, string organizationName)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            
            // 1. Get current patient
            var patient = await GetPatientAsync(fhirBaseUrl, patientId);
            if (patient == null) return false;

            // 2. Update managingOrganization
            patient.ManagingOrganization = new Reference
            {
                ReferenceValue = $"Organization/{organizationId}",
                Display = organizationName
            };

            // 3. PUT updated patient
            var url = $"{fhirBaseUrl}/Patient/{patientId}";
            var response = await client.PutAsJsonAsync(url, patient);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to update patient: {response.StatusCode} {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating patient: {ex.Message}");
            return false;
        }
    }
    public async Task<List<Organization>?> GetOrganizationsAsync(string fhirBaseUrl)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            // 增加數量並按更新時間倒序排列，確保剛新增的醫院會出現在清單中
            var url = $"{fhirBaseUrl}/Organization?_count=200&_sort=-_lastUpdated";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var bundle = await response.Content.ReadFromJsonAsync<Bundle<Organization>>();
                return bundle?.Entry?.Select(e => e.Resource).Where(r => r != null).ToList()!;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting organizations: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> ClearPatientOrganizationAsync(string fhirBaseUrl, string patientId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            
            var patient = await GetPatientAsync(fhirBaseUrl, patientId);
            if (patient == null) return false;

            patient.ManagingOrganization = null;

            var url = $"{fhirBaseUrl}/Patient/{patientId}";
            var response = await client.PutAsJsonAsync(url, patient);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing patient organization: {ex.Message}");
            return false;
        }
    }

    // Fetch DiagnosticReports for a patient (fundus exams)
    public async Task<List<DiagnosticReport>?> GetDiagnosticReportsAsync(string fhirBaseUrl, string patientId, string? code = null)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/DiagnosticReport?subject=Patient/{patientId}";
            
            // Filter by code if specified (e.g., 92134-4 for Fundus photography)
            if (!string.IsNullOrEmpty(code))
            {
                url += $"&code={code}";
            }

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var reports = new List<DiagnosticReport>();
                if (doc.RootElement.TryGetProperty("entry", out var entryArray))
                {
                    foreach (var entry in entryArray.EnumerateArray())
                    {
                        if (entry.TryGetProperty("resource", out var resource))
                        {
                            var report = JsonSerializer.Deserialize<DiagnosticReport>(resource.GetRawText());
                            if (report != null)
                            {
                                reports.Add(report);
                            }
                        }
                    }
                }
                return reports;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting diagnostic reports: {ex.Message}");
        }

        return null;
    }

    // Fetch a single Media resource by ID
    public async Task<Media?> GetMediaAsync(string fhirBaseUrl, string mediaId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Media/{mediaId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Media>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting media: {ex.Message}");
        }

        return null;
    }

    // Fetch all Media for a patient
    public async Task<List<Media>?> GetPatientMediaAsync(string fhirBaseUrl, string patientId)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/Media?subject=Patient/{patientId}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var mediaList = new List<Media>();
                if (doc.RootElement.TryGetProperty("entry", out var entryArray))
                {
                    foreach (var entry in entryArray.EnumerateArray())
                    {
                        if (entry.TryGetProperty("resource", out var resource))
                        {
                            var media = JsonSerializer.Deserialize<Media>(resource.GetRawText());
                            if (media != null)
                            {
                                mediaList.Add(media);
                            }
                        }
                    }
                }
                return mediaList;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting patient media: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Posts an AuditEvent to the FHIR server to log patient access.
    /// Returns (success, statusCode, errorMessage)
    /// </summary>
    public async Task<(bool Success, int StatusCode, string? Error)> PostAuditEventAsync(
        string fhirBaseUrl, 
        string patientId, 
        string? patientName = null,
        string appName = "SmartOnFhirApp",
        string? sourceIp = null)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/AuditEvent";

            var auditEvent = new AuditEvent
            {
                Recorded = DateTime.UtcNow.ToString("o"),
                Agent = new List<AuditEventAgent>
                {
                    new AuditEventAgent
                    {
                        Name = appName,
                        Requestor = true,
                        Who = string.IsNullOrEmpty(sourceIp) ? null : new Reference 
                        { 
                            Display = sourceIp 
                        }
                    }
                },
                Source = new AuditEventSource
                {
                    Site = fhirBaseUrl,
                    Observer = new Reference { Display = appName }
                },
                Entity = new List<AuditEventEntity>
                {
                    new AuditEventEntity
                    {
                        What = new Reference 
                        { 
                            ReferenceValue = $"Patient/{patientId}",
                            Display = patientName
                        },
                        Type = new Coding 
                        { 
                            System = "http://terminology.hl7.org/CodeSystem/audit-entity-type",
                            Code = "1", 
                            Display = "Person" 
                        },
                        Name = patientName ?? patientId
                    }
                }
            };

            Console.WriteLine($"[AuditEvent] Posting to {url}...");
            var response = await client.PostAsJsonAsync(url, auditEvent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AuditEvent] ✅ Success! Status: {(int)response.StatusCode}");
                return (true, (int)response.StatusCode, null);
            }
            else
            {
                Console.WriteLine($"[AuditEvent] ❌ Failed! Status: {(int)response.StatusCode}, Body: {responseBody}");
                return (false, (int)response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditEvent] ❌ Exception: {ex.Message}");
            return (false, 0, ex.Message);
        }
    }

    /// <summary>
    /// Fetches AuditEvent records from the FHIR server.
    /// </summary>
    public async Task<List<AuditEvent>?> GetAuditEventsAsync(string fhirBaseUrl, int count = 20)
    {
        try
        {
            var client = await GetAuthenticatedHttpClientAsync();
            var url = $"{fhirBaseUrl}/AuditEvent?_count={count}&_sort=-date";

            Console.WriteLine($"[AuditEvent] Fetching from {url}...");
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);

                var events = new List<AuditEvent>();
                if (doc.RootElement.TryGetProperty("entry", out var entryArray))
                {
                    foreach (var entry in entryArray.EnumerateArray())
                    {
                        if (entry.TryGetProperty("resource", out var resource))
                        {
                            var evt = System.Text.Json.JsonSerializer.Deserialize<AuditEvent>(resource.GetRawText());
                            if (evt != null)
                            {
                                events.Add(evt);
                            }
                        }
                    }
                }
                Console.WriteLine($"[AuditEvent] Found {events.Count} records.");
                return events;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AuditEvent] Failed to fetch: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditEvent] Fetch error: {ex.Message}");
        }
        return null;
    }
}

