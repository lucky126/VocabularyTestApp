using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VocabularyTestApp.Data
{
    public class CozeGradingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CozeGradingService> _logger;
        private readonly IConfiguration _configuration;
        
        // Configuration
        private const string BaseUrl = "https://api.coze.cn/v1";

        public CozeGradingService(HttpClient httpClient, ILogger<CozeGradingService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public class RecognitionResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("number")]
            public int 序号 { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public string 文本 { get; set; } = string.Empty;
        }

        public async Task<List<RecognitionResult>> RecognizePaperAsync(string base64Image)
        {
            try
            {
                // 1. Upload image to Coze
                string fileId = await UploadImageAsync(base64Image);
                if (string.IsNullOrEmpty(fileId))
                {
                    _logger.LogError("Failed to upload image to Coze");
                    return new List<RecognitionResult>();
                }

                // 2. Call Workflow
                var resultJson = await CallWorkflowAsync(fileId);
                if (string.IsNullOrEmpty(resultJson))
                {
                    return new List<RecognitionResult>();
                }

                // 3. Parse Result
                return ParseRecognitionResult(resultJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecognizePaperAsync");
                return new List<RecognitionResult>();
            }
        }

        private async Task<string> UploadImageAsync(string base64Image)
        {
            // Remove prefix if present (e.g., "data:image/jpeg;base64,")
            if (base64Image.Contains(","))
            {
                base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);
            }

            var imageBytes = Convert.FromBase64String(base64Image);
            var uploadUrl = $"{BaseUrl}/files/upload";

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", "paper.png");

            using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = content };
            var token = _configuration["Coze:ApiToken"];
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                _logger.LogError($"Coze upload failed: {resp.StatusCode} - {err}");
                return string.Empty;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
            {
                return id.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private async Task<string> CallWorkflowAsync(string fileId)
        {
            var url = $"{BaseUrl}/workflow/stream_run";
            
            // Construct payload matching the reference CozeService.cs pattern
            var imageParam = JsonSerializer.Serialize(new { file_id = fileId });
            
            var payload = new
            {
                workflow_id = _configuration["Coze:WorkflowId"],
                parameters = new
                {
                    input = imageParam 
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            var token = _configuration["Coze:ApiToken"];
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Use HttpCompletionOption.ResponseHeadersRead to start reading stream immediately
            using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                _logger.LogError($"Workflow call failed: {resp.StatusCode} - {err}");
                return string.Empty;
            }

            // Parse stream response
            return await ParseStreamResponse(resp);
        }

        private async Task<string> ParseStreamResponse(HttpResponseMessage resp)
        {
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string currentEvent = "";
            string? lastMessageContent = null;
            string? finishedData = null;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                
                var trim = line.Trim();
                if (string.IsNullOrEmpty(trim)) continue;

                if (trim.StartsWith("event:"))
                {
                    currentEvent = trim.Substring(6).Trim();
                }
                else if (trim.StartsWith("data:"))
                {
                    var data = trim.Substring(5).Trim();
                    _logger.LogInformation($"Coze Event: {currentEvent}, Data: {data}"); // Log for debug

                    if (currentEvent == "workflow_finished")
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(data);
                            if (doc.RootElement.TryGetProperty("data", out var dataElem))
                            {
                                finishedData = dataElem.GetString();
                            }
                        }
                        catch { /* Ignore */ }
                    }
                    else if (currentEvent == "Message")
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(data);
                            if (doc.RootElement.TryGetProperty("content", out var contentElem))
                            {
                                var contentStr = contentElem.GetString();
                                if (!string.IsNullOrEmpty(contentStr))
                                {
                                    // Check if it looks like our JSON output
                                    if (contentStr.Contains("\"output\"") || contentStr.Trim().StartsWith("[{\""))
                                    {
                                        lastMessageContent = contentStr;
                                    }
                                }
                            }
                        }
                        catch { /* Ignore */ }
                    }
                }
            }

            // Prefer workflow_finished data
            if (!string.IsNullOrEmpty(finishedData)) return finishedData;
            
            // Fallback to last message content
            return lastMessageContent ?? string.Empty;
        }

        private List<RecognitionResult> ParseRecognitionResult(string contentJson)
        {
            try
            {
                if (string.IsNullOrEmpty(contentJson)) return new List<RecognitionResult>();
                
                _logger.LogInformation($"Parsing Coze Result: {contentJson}");

                // Clean markdown if present (e.g., ```json ... ```)
                contentJson = Regex.Replace(contentJson, @"^```json\s*", "", RegexOptions.IgnoreCase);
                contentJson = Regex.Replace(contentJson, @"\s*```$", "");
                contentJson = contentJson.Trim();

                // Unescape JSON string if it's double encoded
                if (contentJson.StartsWith("\"") && contentJson.EndsWith("\""))
                {
                    try 
                    {
                        var unescaped = JsonSerializer.Deserialize<string>(contentJson);
                        if (!string.IsNullOrEmpty(unescaped))
                        {
                            contentJson = unescaped;
                            _logger.LogInformation($"Unescaped JSON: {contentJson}");
                            
                            // Re-clean markdown after unescape
                            contentJson = Regex.Replace(contentJson, @"^```json\s*", "", RegexOptions.IgnoreCase);
                            contentJson = Regex.Replace(contentJson, @"\s*```$", "");
                            contentJson = contentJson.Trim();
                        }
                    }
                    catch { /* Ignore unescape failure */ }
                }

                using var doc = JsonDocument.Parse(contentJson);
                var root = doc.RootElement;
                
                // Case 1: Root is Object with "output"
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("output", out var outputElem))
                    {
                        if (outputElem.ValueKind == JsonValueKind.String)
                        {
                            var outputStr = outputElem.GetString();
                            if (!string.IsNullOrEmpty(outputStr))
                            {
                                // Recursive clean in case inner string is also markdown
                                outputStr = Regex.Replace(outputStr, @"^```json\s*", "", RegexOptions.IgnoreCase);
                                outputStr = Regex.Replace(outputStr, @"\s*```$", "");
                                outputStr = outputStr.Trim();
                                return JsonSerializer.Deserialize<List<RecognitionResult>>(outputStr) ?? new List<RecognitionResult>();
                            }
                        }
                        else if (outputElem.ValueKind == JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<List<RecognitionResult>>(outputElem.GetRawText()) ?? new List<RecognitionResult>();
                        }
                    }
                    // Case 2: Root object IS the result (unlikely based on description but possible)
                    else if (root.TryGetProperty("code", out var code) && code.GetInt32() == 0 && root.TryGetProperty("data", out var data))
                    {
                         // Handle standard Coze response wrapper if raw JSON passed
                         // But usually we get the inner content.
                    }
                }
                // Case 3: Root is Array directly
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<RecognitionResult>>(contentJson) ?? new List<RecognitionResult>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing recognition result");
            }
            return new List<RecognitionResult>();
        }
    }
}
