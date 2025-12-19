using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using SmartOnFhirApp.Models;

namespace SmartOnFhirApp.Services
{
    public class AiSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiSummaryService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> SummarizePatientDataAsync(Patient patient, List<Condition> conditions, List<MedicationRequest> medications, List<Observation> observations, List<DiagnosticReport>? reports = null)
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");

                var patientName = patient.Name?.FirstOrDefault()?.Text ?? 
                                 $"{patient.Name?.FirstOrDefault()?.Family} {patient.Name?.FirstOrDefault()?.Given?.FirstOrDefault()}".Trim();
                var genderDisplay = patient.Gender == "male" ? "男" : (patient.Gender == "female" ? "女" : "未知");
                var birthDate = patient.BirthDate ?? "未提供";

                // 1. 在 C# 端先整理好生命徵象表格（含日期以便對比時間變化）
                var vitalSignsTable = new StringBuilder();
                vitalSignsTable.AppendLine("| 項目 | 數值 | 檢測日期 |");
                vitalSignsTable.AppendLine("| :--- | :--- | :--- |");
                
                bool hasVitals = false;
                if (observations != null)
                {
                    // 篩選生命徵象並按日期排序（最新在前）
                    var vitalObs = observations
                        .Where(o => {
                            var display = o.Code?.Text ?? o.Code?.Coding?.FirstOrDefault()?.Display ?? "";
                            return display.Contains("Height", StringComparison.OrdinalIgnoreCase) || 
                                   display.Contains("Weight", StringComparison.OrdinalIgnoreCase) || 
                                   display.Contains("BMI", StringComparison.OrdinalIgnoreCase) || 
                                   display.Contains("Blood Pressure", StringComparison.OrdinalIgnoreCase) || 
                                   display.Contains("身高") || display.Contains("體重") ||
                                   display.Contains("Heart rate", StringComparison.OrdinalIgnoreCase) || 
                                   display.Contains("Pulse", StringComparison.OrdinalIgnoreCase) ||
                                   display.Contains("Respiratory rate", StringComparison.OrdinalIgnoreCase) ||
                                   display.Contains("Oxygen saturation", StringComparison.OrdinalIgnoreCase) ||
                                   display.Contains("SpO2", StringComparison.OrdinalIgnoreCase) ||
                                   display.Contains("Temperature", StringComparison.OrdinalIgnoreCase) ||
                                   display.Contains("體溫") || display.Contains("心率") || display.Contains("呼吸") || display.Contains("血氧");
                        })
                        .OrderByDescending(o => o.EffectiveDateTime ?? "0000-00-00")
                        .ToList();

                    foreach (var o in vitalObs)
                    {
                        var display = o.Code?.Text ?? o.Code?.Coding?.FirstOrDefault()?.Display ?? "";
                        var valStr = "";
                        if (o.ValueQuantity != null) 
                        {
                            // 格式化數值為最多2位小數
                            var formattedValue = Math.Round(o.ValueQuantity.Value ?? 0, 2);
                            valStr = $"{formattedValue} {o.ValueQuantity.Unit}";
                        }
                        else if (o.Component?.Any() == true) 
                        {
                            valStr = string.Join(", ", o.Component.Select(c => {
                                var compValue = Math.Round(c.ValueQuantity?.Value ?? 0, 2);
                                return $"{c.Code?.Text ?? c.Code?.Coding?.FirstOrDefault()?.Display}: {compValue} {c.ValueQuantity?.Unit}";
                            }));
                        }
                        
                        // 格式化日期
                        var dateStr = "-";
                        if (!string.IsNullOrEmpty(o.EffectiveDateTime))
                        {
                            if (DateTime.TryParse(o.EffectiveDateTime, out var dt))
                            {
                                dateStr = dt.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                dateStr = o.EffectiveDateTime.Length > 10 ? o.EffectiveDateTime.Substring(0, 10) : o.EffectiveDateTime;
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(valStr))
                        {
                            vitalSignsTable.AppendLine($"| {display} | {valStr} | {dateStr} |");
                            hasVitals = true;
                        }
                    }
                }
                if (!hasVitals) vitalSignsTable.AppendLine("| 暫無記錄 | - | - |");

                // 2. 在 C# 端先整理好診斷表格
                var diagnosisSection = new StringBuilder();
                if (conditions != null && conditions.Any())
                {
                    // 統計相同診斷的次數
                    var diagnosisGroups = conditions
                        .GroupBy(c => c.Code?.Text ?? c.Code?.Coding?.FirstOrDefault()?.Display ?? "未知診斷")
                        .Select(g => new {
                            Name = g.Key,
                            Count = g.Count(),
                            Status = g.First().ClinicalStatus?.Coding?.FirstOrDefault()?.Code ?? "unknown"
                        });

                    foreach (var diag in diagnosisGroups)
                    {
                        var statusDisplay = diag.Status switch {
                            "active" => "進行中",
                            "resolved" => "已痊癒",
                            "inactive" => "非活動",
                            _ => diag.Status
                        };
                        var countStr = diag.Count > 1 ? $" x{diag.Count}" : "";
                        diagnosisSection.AppendLine($"- **{diag.Name}** (狀態: {statusDisplay}){countStr}");
                    }
                }
                else
                {
                    diagnosisSection.AppendLine("- 無診斷記錄");
                }

                // 2.5 整理檢查報告 (眼底鏡等)
                var reportSection = new StringBuilder();
                if (reports != null && reports.Any())
                {
                    foreach(var r in reports)
                    {
                        var reportName = r.Code?.Text ?? r.Code?.Coding?.FirstOrDefault()?.Display ?? "檢查報告";
                        // 優先使用 ConclusionCode 的文字描述，若無則使用 Conclusion
                        var conclusion = r.ConclusionCode?.FirstOrDefault()?.Text ?? r.Conclusion ?? "無結論";
                        var date = !string.IsNullOrEmpty(r.EffectiveDateTime) && DateTime.TryParse(r.EffectiveDateTime, out var dt) 
                            ? dt.ToString("yyyy-MM-dd") : r.EffectiveDateTime;
                        
                        reportSection.AppendLine($"- **{reportName}** ({date}): {conclusion}");
                    }
                }

                // 3. 在 C# 端先整理好用藥表格
                var medicationTable = new StringBuilder();
                medicationTable.AppendLine("| 藥物 | 劑量 | 狀態 |");
                medicationTable.AppendLine("| :--- | :--- | :--- |");
                
                bool hasMeds = false;
                if (medications != null && medications.Any())
                {
                    foreach (var m in medications)
                    {
                        var medName = m.MedicationCodeableConcept?.Text ?? m.MedicationCodeableConcept?.Coding?.FirstOrDefault()?.Display ?? "未知藥物";
                        var dosage = m.DosageInstruction?.FirstOrDefault()?.Text ?? "-";
                        var statusDisplay = m.Status switch {
                            "active" => "使用中",
                            "completed" => "已完成",
                            "stopped" => "已停止",
                            "on-hold" => "暫停",
                            _ => m.Status ?? "-"
                        };
                        medicationTable.AppendLine($"| {medName} | {dosage} | {statusDisplay} |");
                        hasMeds = true;
                    }
                }
                if (!hasMeds) medicationTable.AppendLine("| 暫無記錄 | - | - |");

                var patientContext = BuildContextData(patient, conditions, medications, observations, reports);

                // 4. 嘗試呼叫 AI 服務 (主要 + Fallback)
                var aiSummary = await CallAiWithFallbackAsync(patientContext);

                // 5. 在 C# 端組合完整報告（確保格式完整）
                var finalMarkdown = new StringBuilder();
                finalMarkdown.AppendLine($"## 一、基本資料");
                finalMarkdown.AppendLine($"- **姓名**: {patientName}");
                finalMarkdown.AppendLine($"- **性別**: {genderDisplay}");
                finalMarkdown.AppendLine($"- **出生日期**: {birthDate}");
                finalMarkdown.AppendLine($"- **報告日期**: {today}");
                finalMarkdown.AppendLine();
                finalMarkdown.AppendLine($"## 二、體格檢查與生命徵象");
                finalMarkdown.Append(vitalSignsTable);
                finalMarkdown.AppendLine();
                finalMarkdown.AppendLine($"## 三、診斷與病史");
                finalMarkdown.Append(diagnosisSection);
                if (reportSection.Length > 0)
                {
                    finalMarkdown.AppendLine();
                    finalMarkdown.AppendLine("### 檢查報告");
                    finalMarkdown.Append(reportSection);
                }
                finalMarkdown.AppendLine();
                finalMarkdown.AppendLine($"## 四、用藥記錄");
                finalMarkdown.Append(medicationTable);
                finalMarkdown.AppendLine();
                finalMarkdown.AppendLine($"## 五、臨床分析摘要");
                finalMarkdown.AppendLine(string.IsNullOrEmpty(aiSummary) ? "暫無 AI 分析結果。" : aiSummary);
                finalMarkdown.AppendLine();
                finalMarkdown.AppendLine("> **重要：本摘要內容由LLM自動生成，僅供輔助參考。請務必核對原始病歷資料。**");
                
                return finalMarkdown.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiService] Exception: {ex.Message}");
                return $"生成失敗 (Error): {ex.Message}";
            }
        }

        private async Task<string> CallAiWithFallbackAsync(string patientContext)
        {
            // 1. 嘗試主要 AI 服務 (AiService)
            var primaryEndpoint = _configuration["AiService:Endpoint"];
            var primaryModel = _configuration["AiService:Model"] ?? "Openai-Gpt";
            var primaryKey = _configuration["AiService:Key"];

            if (!string.IsNullOrEmpty(primaryEndpoint))
            {
                // 對於 OpenRouter 或 OpenAI Chat API，Endpoint 通常應該是 /chat/completions
                if (!primaryEndpoint.EndsWith("/chat/completions") && !primaryEndpoint.Contains("generate")) 
                {
                   // 若使用者只填了 Base URL (如 https://openrouter.ai/api/v1)，自動補上
                   primaryEndpoint = primaryEndpoint.TrimEnd('/') + "/chat/completions";
                }

                try
                {
                    Console.WriteLine($"[AiService] 嘗試主要服務: {primaryEndpoint} (Model: {primaryModel})");
                    var result = await CallPrimaryAiAsync(_httpClient, primaryEndpoint, primaryModel, primaryKey ?? "", patientContext);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AiService] 主要服務失敗: {ex.Message}，嘗試切換至 Ollama...");
                }
            }
            else
            {
                Console.WriteLine("[AiService] 未設定主要服務 (AiService:Endpoint)，直接使用 Ollama。");
            }

            // 2. Fallback 到 Ollama (OllamaService)
            var ollamaEndpoint = _configuration["OllamaService:Endpoint"];
            var ollamaModel = _configuration["OllamaService:Model"] ?? "devstral-small-2";

            if (!string.IsNullOrEmpty(ollamaEndpoint))
            {
                 // 確保 Endpoint 包含 /api/generate
                if (!ollamaEndpoint.EndsWith("/api/generate"))
                {
                    ollamaEndpoint = ollamaEndpoint.TrimEnd('/') + "/api/generate";
                }

                try
                {
                    Console.WriteLine($"[AiService] 嘗試 Ollama 服務: {ollamaEndpoint} ({ollamaModel})");
                    return await CallOllamaAsync(_httpClient, ollamaEndpoint, ollamaModel, patientContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AiService] 設定的 Ollama 服務失敗: {ex.Message}，嘗試預設值...");
                }
            }

            // 3. 最後 Fallback 到預設 Localhost Ollama
            var defaultEndpoint = "http://localhost:11434/api/generate";
            var defaultModel = "qwen3:0.6b"; // 預設模型
            
            try 
            {
                Console.WriteLine($"[AiService] 嘗試預設 Ollama: {defaultEndpoint}");
                return await CallOllamaAsync(_httpClient, defaultEndpoint, defaultModel, patientContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiService] 全部服務皆失敗: {ex.Message}");
                return $"⚠️ AI 服務暫時無法使用 (所有備援皆失敗)";
            }
        }

        private async Task<string> CallPrimaryAiAsync(HttpClient httpClient, string endpoint, string model, string apiKey, string patientContext)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("未設定 AI Service Key");
            }

            // 確保請求包含 Authorization Header
            // 注意：httpClient 是單例的，修改 DefaultRequestHeaders 會影響全域。
            // 建議使用 HttpRequestMessage 來發送請求。
            
            var prompt = BuildPrompt(patientContext);

            var payload = new
            {
                model = model,
                messages = new[] { 
                    new { role = "user", content = prompt } 
                },
                max_tokens = 2000,
                temperature = 0.3,
                top_p = 1.0,
                frequency_penalty = 0.3,
                presence_penalty = 0.0
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            // OpenRouter 需要 Referer 和 X-Title
            request.Headers.Add("HTTP-Referer", "https://github.com/ntuh-melandz/ntuh-melandz.github.io");
            request.Headers.Add("X-Title", "SmartOnFhirApp");
            request.Content = JsonContent.Create(payload);

            var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error {response.StatusCode}: {error}");
            }

            var rawResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[AiService] Raw Response: {rawResponse.Substring(0, Math.Min(rawResponse.Length, 200))}..."); // 只印前200字避免Log過長

            try 
            {
                using var doc = JsonDocument.Parse(rawResponse);
                var root = doc.RootElement;
                string? aiSummary = null;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var messageProp) && 
                        messageProp.TryGetProperty("content", out var contentProp))
                    {
                        aiSummary = contentProp.GetString();
                    }
                    else if (firstChoice.TryGetProperty("text", out var textProp))
                    {
                        aiSummary = textProp.GetString();
                    }
                }

                return CleanAiOutput(aiSummary ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiService] JSON Parsing Error: {ex.Message}");
                return "";
            }
        }

        private async Task<string> CallOllamaAsync(HttpClient httpClient, string endpoint, string model, string patientContext)
        {
            var prompt = BuildPrompt(patientContext);

            var payload = new
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    num_predict = 500
                }
            };

            var response = await httpClient.PostAsJsonAsync(endpoint, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ollama Error {response.StatusCode}: {error}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            var aiSummary = jsonResponse?.Response?.Trim() ?? "";
            
            return CleanAiOutput(aiSummary);
        }

        private string CalculateAge(string? birthDateString)
        {
            if (string.IsNullOrEmpty(birthDateString) || !DateTime.TryParse(birthDateString, out var birthDate))
            {
                return "未知";
            }
            
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return $"{age}歲";
        }

        private string BuildPrompt(string patientContext)
        {
            return $"[角色]\n你是專業的醫療分析師，請根據以下病患資料生成一段臨床分析摘要。\n\n" +
                   $"[病患資料]\n{patientContext}\n\n" +
                   $"[要求]\n" +
                   $"1. 僅生成「臨床分析摘要」段落，約 100-300 字。\n" +
                   $"2. 請使用繁體中文撰寫。\n" +
                   $"3. 重點包含：主要健康問題、用藥情況、需要關注的事項。\n" +
                   $"4. 使用專業但易懂的醫療用語。\n" +
                   $"5. 請遵循以下專有名詞與描述風格：\n" +
                   $"   - **描述病患基本資料時，僅使用「年齡」（如：30歲），嚴禁輸出「出生年份」或「出生日期」。**\n" +
                   $"   - **用藥名稱請務必「完全依照資料中的文字」描述，嚴禁自行翻譯、簡化或替換為學名（例如：資料為「脈優錠」，不要寫成「氨氯地平」）。**\n" +
                   $"   - 描述用藥時，請包含劑量與頻率（如：現行藥物為脈優錠 5mg 每日一次）。\n" +
                   $"   - **數據分析必須遵循時序性，優先描述「最新」的檢測結果，並與「過去」的數據進行對比以說明趨勢（例如：血壓已從 145/90 降至 120/80，顯示控制良好）。**\n" +
                   $"   - 若血壓控制尚可，請在摘要中明確指出。\n" +
                   $"   - 檢查報告中的日期為「檢查執行日期」，描述建議轉診或追蹤時，請勿將該日期誤用為「截止日期」或「期限」。\n" +
                   $"   - 檢查報告的結論與建議追蹤時間，請務必依照資料內容描述，不要自行推測。\n" +
                   $"   - **嚴禁輸出任何與「兒童」或「小兒科」相關的字眼（除非資料中明確提及）。**\n" +
                   $"6. **嚴禁輸出任何思考過程、草稿、字數統計或英文說明（如 Proceed.）。**\n" +
                   $"7. **嚴禁在摘要中提及「台灣繁體中文」這幾個字。**\n" +
                   $"8. **直接輸出最終的摘要內容，不要加任何標題、前言、引號或「臨床分析摘要」字樣。**\n\n" +
                   $"[臨床分析摘要]";
                   
        }

        private string CleanAiOutput(string aiSummary)
        {
            if (string.IsNullOrEmpty(aiSummary)) return "";

            // 1. 移除 <think> 標籤 (qwen 模型可能會輸出)
            if (aiSummary.Contains("<think>"))
            {
                var thinkEnd = aiSummary.IndexOf("</think>");
                if (thinkEnd >= 0)
                {
                    aiSummary = aiSummary.Substring(thinkEnd + 8).Trim();
                }
                else
                {
                    // 如果只有開始標籤沒有結束標籤，嘗試移除開始標籤後的所有內容（或直到下一個段落）
                    var thinkStart = aiSummary.IndexOf("<think>");
                    aiSummary = aiSummary.Substring(0, thinkStart).Trim();
                }
            }

            // 2. 處理 AI 輸出中可能包含的「草稿」、「總結如下」等標記
            string[] markers = { "Final answer:", "Final summary:", "臨床分析摘要：", "臨床分析總結：", "摘要如下：", "總結如下：", "Draft:", "Output:", "Proceed.", "Proceed:" };
            foreach (var marker in markers)
            {
                int index = aiSummary.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    aiSummary = aiSummary.Substring(index + marker.Length).Trim();
                }
            }

            // 3. 清理 AI 可能輸出的多餘前言（如：好的、根據資料...）
            string[] prefixes = { "好的", "以下是", "根據", "針對", "這名" };
            foreach (var prefix in prefixes)
            {
                if (aiSummary.StartsWith(prefix))
                {
                    // 尋找第一個句號，如果句號很靠前，通常是廢話前言
                    int firstFullStop = aiSummary.IndexOf("。");
                    if (firstFullStop >= 0 && firstFullStop < 40) 
                    {
                        aiSummary = aiSummary.Substring(firstFullStop + 1).Trim();
                    }
                }
            }

            // 4. 移除 Markdown 標題 (如 ## 臨床分析摘要)
            if (aiSummary.StartsWith("#"))
            {
                var lines = aiSummary.Split('\n');
                aiSummary = string.Join("\n", lines.Where(l => !l.Trim().StartsWith("#"))).Trim();
            }

            // 5. 如果內容中混雜了大量英文（可能是 AI 的思考過程），嘗試只保留中文段落
            if (aiSummary.Any(c => c >= 0x4E00 && c <= 0x9FFF)) // 含有中文
            {
                var paragraphs = aiSummary.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                // 找出最像醫療摘要的段落（長度適中且中文比例高）
                var bestParagraph = paragraphs
                    .OrderByDescending(p => p.Count(c => c >= 0x4E00 && c <= 0x9FFF))
                    .FirstOrDefault();
                
                if (bestParagraph != null && bestParagraph.Length > 20)
                {
                    aiSummary = bestParagraph;
                }
            }

            // 6. 強制將常見的簡體醫療術語轉換為繁體 (作為最後一道防線)
            var s2t = new Dictionary<string, string>
            {
                { "体重", "體重" }, { "分钟", "分鐘" }, { "血压", "血壓" }, { "追踪", "追蹤" },
                { "癫痫", "癲癇" }, { "检查", "檢查" }, { "建议", "建議" }, { "诊断", "診斷" },
                { "药物", "藥物" }, { "治疗", "治療" }, { "医院", "醫院" }, { "医生", "醫生" },
                { "视网膜", "視網膜" }, { "糖尿病", "糖尿病" }, { "控制", "控制" }, { "进展", "進展" },
                { "口服", "口服" }, { "次数", "次數" }, { "项目", "項目" }, { "记录", "記錄" }
            };

            foreach (var item in s2t)
            {
                aiSummary = aiSummary.Replace(item.Key, item.Value);
            }

            // 7. 移除前後可能出現的引號
            aiSummary = aiSummary.Trim().Trim('"').Trim('\'').Trim();

            return aiSummary;
        }

        private string BuildContextData(Patient p, List<Condition>? conds, List<MedicationRequest>? meds, List<Observation>? obs, List<DiagnosticReport>? reports)
        {
            var sb = new StringBuilder();
            var age = CalculateAge(p.BirthDate);
            // 移除出生日期，避免 AI 誤用
            sb.AppendLine($"病患: {p.Name?[0]?.Text ?? (p.Name?[0]?.Family + p.Name?[0]?.Given?.FirstOrDefault())} ({p.Gender}, 年齡: {age})");
            
            // 分離生命徵象與一般檢驗
            var vitalSigns = new List<string>();
            var otherObs = new List<string>();

            if (obs != null)
            {
                // 按日期排序（最新在前），確保 AI 優先看到最新數據
                var sortedObs = obs.OrderByDescending(o => o.EffectiveDateTime ?? "0000-00-00").ToList();

                foreach (var o in sortedObs)
                {
                    var display = o.Code?.Text ?? o.Code?.Coding?.FirstOrDefault()?.Display ?? "";
                    
                    // 清理顯示名稱，移除可能干擾 AI 的技術性文字 (如 LOINC 的 with all children optional)
                    display = display.Replace(" with all children optional", "", StringComparison.OrdinalIgnoreCase).Trim();
                    
                    var valStr = "";
                    
                    if (o.ValueQuantity != null)
                    {
                        valStr = $"{o.ValueQuantity.Value} {o.ValueQuantity.Unit}";
                    }
                    else if (o.Component?.Any() == true)
                    {
                        valStr = string.Join(", ", o.Component.Select(c => $"{c.Code?.Text}: {c.ValueQuantity?.Value} {c.ValueQuantity?.Unit}"));
                    }

                    // 格式化日期
                    var dateStr = "";
                    if (!string.IsNullOrEmpty(o.EffectiveDateTime) && DateTime.TryParse(o.EffectiveDateTime, out var dt))
                    {
                        dateStr = dt.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        dateStr = o.EffectiveDateTime ?? "未知日期";
                    }

                    var line = $"- {display}: {valStr} ({dateStr})";
                    
                    // 簡單判斷是否為生命徵象 (身高, 體重, BMI, 血壓, 心率, 體溫, 血氧等)
                    if (display.Contains("Height", StringComparison.OrdinalIgnoreCase) || 
                        display.Contains("Weight", StringComparison.OrdinalIgnoreCase) || 
                        display.Contains("BMI", StringComparison.OrdinalIgnoreCase) || 
                        display.Contains("Blood Pressure", StringComparison.OrdinalIgnoreCase) || 
                        display.Contains("身高") || display.Contains("體重") ||
                        display.Contains("Heart rate", StringComparison.OrdinalIgnoreCase) || 
                        display.Contains("Pulse", StringComparison.OrdinalIgnoreCase) ||
                        display.Contains("Respiratory rate", StringComparison.OrdinalIgnoreCase) ||
                        display.Contains("Oxygen saturation", StringComparison.OrdinalIgnoreCase) ||
                        display.Contains("SpO2", StringComparison.OrdinalIgnoreCase) ||
                        display.Contains("Temperature", StringComparison.OrdinalIgnoreCase) ||
                        display.Contains("體溫") || display.Contains("心率") || display.Contains("呼吸") || display.Contains("血氧"))
                    {
                        vitalSigns.Add(line);
                    }
                    else
                    {
                        otherObs.Add(line);
                    }
                }
            }

            // 自動計算 BMI
            if (obs != null)
            {
                var heights = obs.Where(o => (o.Code?.Text?.Contains("Body Height", StringComparison.OrdinalIgnoreCase) == true || 
                                             o.Code?.Text?.Contains("Height", StringComparison.OrdinalIgnoreCase) == true ||
                                             o.Code?.Text?.Contains("身高") == true) && 
                                             o.Value is Hl7.Fhir.Model.Quantity)
                                 .Select(o => (Hl7.Fhir.Model.Quantity)o.Value)
                                 .OrderByDescending(q => q.Value) // 取最新的或合理的
                                 .FirstOrDefault();

                var weights = obs.Where(o => (o.Code?.Text?.Contains("Body Weight", StringComparison.OrdinalIgnoreCase) == true || 
                                             o.Code?.Text?.Contains("Weight", StringComparison.OrdinalIgnoreCase) == true ||
                                             o.Code?.Text?.Contains("體重") == true) && 
                                             o.Value is Hl7.Fhir.Model.Quantity)
                                 .Select(o => (Hl7.Fhir.Model.Quantity)o.Value)
                                 .OrderByDescending(q => q.Value)
                                 .FirstOrDefault();

                if (heights?.Value.HasValue == true && weights?.Value.HasValue == true)
                {
                    double h = (double)heights.Value.Value;
                    double w = (double)weights.Value.Value;
                    
                    // 單位轉換
                    if (heights.Unit?.Contains("cm", StringComparison.OrdinalIgnoreCase) == true) h = h / 100.0;
                    if (weights.Unit?.Contains("g", StringComparison.OrdinalIgnoreCase) == true && !weights.Unit.Equals("kg", StringComparison.OrdinalIgnoreCase)) w = w / 1000.0; // 簡單防呆

                    if (h > 0 && w > 0)
                    {
                        double bmi = w / (h * h);
                        string status;
                        if (bmi < 18.5) status = "過輕";
                        else if (bmi < 24) status = "正常";
                        else if (bmi < 27) status = "過重";
                        else status = "輕度肥胖以上";

                        vitalSigns.Add($"- [系統計算] BMI: {bmi:F1} ({status}) - 基於身高 {h*100:F0}cm, 體重 {w:F1}kg");
                    }
                }
            }

            // 確保有找到任何vital signs，如果沒有就加默認
            if (!vitalSigns.Any())
                vitalSigns.Add("- 無記錄");

            sb.AppendLine("\n[基本體徵]");
            vitalSigns.ForEach(v => sb.AppendLine(v));

            sb.AppendLine("\n[檢查報告]");
            if (reports != null && reports.Any())
            {
                foreach(var r in reports)
                {
                    // 優先使用 ConclusionCode 的文字描述，若無則使用 Conclusion
                    var conclusion = r.ConclusionCode?.FirstOrDefault()?.Text ?? r.Conclusion ?? "無結論";

                    var date = !string.IsNullOrEmpty(r.EffectiveDateTime) && DateTime.TryParse(r.EffectiveDateTime, out var dt) 
                        ? dt.ToString("yyyy-MM-dd") : r.EffectiveDateTime;
                    sb.AppendLine($"- {r.Code?.Text ?? "檢查項"}: {conclusion} ({date})");
                }
            }
            else
            {
                sb.AppendLine("- 無檢查報告");
            }

            sb.AppendLine("\n[診斷與病史]");
            if (conds != null && conds.Any())
            {
                foreach (var c in conds)
                {
                    var status = c.ClinicalStatus?.Coding?.FirstOrDefault()?.Code ?? "unknown";
                    var statusDisplay = status switch {
                        "active" => "進行中",
                        "resolved" => "已痊癒",
                        "inactive" => "非活動",
                        _ => status
                    };
                    sb.AppendLine($"- {c.Code?.Text ?? c.Code?.Coding?.FirstOrDefault()?.Display} (狀態: {statusDisplay})");
                }
            }
            else
            {
                sb.AppendLine("- 無診斷記錄");
            }

            sb.AppendLine("\n[用藥記錄]");
            if (meds != null && meds.Any())
            {
                foreach (var m in meds)
                {
                    var statusDisplay = m.Status switch {
                        "active" => "使用中",
                        "completed" => "已完成",
                        "stopped" => "已停止",
                        "on-hold" => "暫停",
                        _ => m.Status
                    };
                    var medName = m.MedicationCodeableConcept?.Text ?? m.MedicationCodeableConcept?.Coding?.FirstOrDefault()?.Display ?? "未知藥物";
                    var dosage = m.DosageInstruction?.FirstOrDefault()?.Text ?? "";
                    if (string.IsNullOrEmpty(dosage))
                    {
                        if (m.DosageInstruction?.FirstOrDefault()?.Timing is { Repeat: { } repeat })
                        {
                            if (repeat.Frequency == 1 && repeat.Period == 1 && repeat.PeriodUnit == "d")
                                dosage = "每日一次";
                        }
                    }
                    var dosageStr = !string.IsNullOrEmpty(dosage) ? $" (用法: {dosage})" : "";
                    sb.AppendLine($"- {medName}{dosageStr} (狀態: {statusDisplay})");
                }
            }
            else
            {
                sb.AppendLine("- 無用藥記錄");
            }

            sb.AppendLine("\n[其他檢驗結果]");
            if (otherObs.Any())
                otherObs.Take(10).ToList().ForEach(o => sb.AppendLine(o));
            else
                sb.AppendLine("- 無其他檢驗記錄");

            return sb.ToString();
        }

        // Response DTOs
        private class AiResponse
        {
            [JsonPropertyName("choices")]
            public List<AiChoice>? Choices { get; set; }
        }

        private class AiChoice
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }

            [JsonPropertyName("done")]
            public bool Done { get; set; }
        }
    }
}
