using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace RV.InvNew.Common
{
    /// <summary>
    /// Common service for handling AI Transcriptions of Reference Documents.
    /// </summary>
    public static class TranscriptionService
    {
        private const string DefaultModel = "z-ai/glm-4.6v"; // Or specific high-reasoning vision model ID
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Transcribes a RefDoc using the specified LLM via OpenRouter.
        /// </summary>
        public static async Task<RefDocsTranscription> AITranscribe(long refDocId, string llmName, NewinvContext ctx)
        {
            // 1. Fetch the Document
            var doc = await ctx.RefDocs.FindAsync(refDocId);
            if (doc == null) throw new ArgumentException("RefDoc not found.");
            if (string.IsNullOrEmpty(doc.RefImage)) throw new ArgumentException("RefDoc has no image to transcribe.");

            // 2. Prepare the Prompt
            string systemPrompt = "You are a precise document analysis AI. Extract data from the provided image and return it as a valid JSON object. " +
                                  "Dates must be in ISO 8601 format (YYYY-MM-DDTHH:mm:ss). " +
                                  "If a date is not found, return null. " +
                                  "TranscriptionStructured should contain a JSON string of key-value pairs found in the document.";

            string userPrompt = "Analyze this document and extract the following fields: RefDocTitle, RefDocSummary, TranscribedContent, RefDocIssuedAt, RefDocValidFrom, RefDocNotValidAfter. " +
                                "Also provide TranscriptionStructured (a JSON string of extracted line items or entities) and TranscriptionStructureType (e.g. 'Invoice', 'Receipt', 'Form').";

            // 3. Construct API Payload
            var payload = new JsonObject
            {
                ["model"] = string.IsNullOrEmpty(llmName) ? DefaultModel : llmName,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "system",
                        ["content"] = systemPrompt
                    },
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "text", ["text"] = userPrompt },
                            new JsonObject
                            {
                                ["type"] = "image_url",
                                ["image_url"] = new JsonObject
                                {
                                    ["url"] = $"data:image/jpeg;base64,{doc.RefImage}"
                                }
                            }
                        }
                    }
                },
                
                ["reasoning"] = new JsonObject { ["effort"] = "xhigh" },
                ["response_format"] = new JsonObject { ["type"] = "json_object" }
            };

            // 4. Call API
            string apiKey = null;
            try
            {
                if (Config.model != null)
                {
                    if (Config.model.ToDictionary() is System.Collections.IDictionary dict && dict.Contains("OpenRouterKey"))
                    {
                        apiKey = dict["OpenRouterKey"]?.ToString();
                    }
                    else
                    {
                        var prop = Config.model.GetType().GetProperty("OpenRouterKey");
                        if (prop != null) apiKey = prop.GetValue(Config.model)?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve OpenRouterKey from configuration.", ex);
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenRouter API Key is missing in configuration (Config.model['OpenRouterKey']).");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "InvNew");

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI API Error: {response.StatusCode} - {error}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var contentString = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // 5. Parse Result
            RefDocsTranscription transcriptionData;
            try
            {
                transcriptionData = JsonSerializer.Deserialize<RefDocsTranscription>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                throw new Exception("Failed to parse AI response as JSON.");
            }

            // Helper to ensure DateTime is UTC for PostgreSQL
            DateTime? ToUtc(DateTime? dt)
            {
                if (!dt.HasValue) return null;
                if (dt.Value.Kind == DateTimeKind.Utc) return dt.Value;
                return DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
            }

            // 6. Enforce/Overwrite Fields as requested
            var newEntry = new RefDocsTranscription
            {
                Id = 0,
                RefDoc = refDocId,
                TranscriberLlmName = llmName ?? DefaultModel,
                TranscribedAt = DateTime.UtcNow, // Explicitly UTC

                // AI Generated Fields - Ensure UTC
                RefDocTitle = transcriptionData?.RefDocTitle,
                RefDocSummary = transcriptionData?.RefDocSummary,
                TranscribedContent = transcriptionData?.TranscribedContent,
                RefDocIssuedAt = ToUtc(transcriptionData?.RefDocIssuedAt),
                RefDocValidFrom = ToUtc(transcriptionData?.RefDocValidFrom),
                RefDocNotValidAfter = ToUtc(transcriptionData?.RefDocNotValidAfter),
                TranscriptionStructured = transcriptionData?.TranscriptionStructured,
                TranscriptionStructureType = transcriptionData?.TranscriptionStructureType
            };

            // 7. Save to DB
            ctx.RefDocsTranscriptions.Add(newEntry);
            await ctx.SaveChangesAsync();

            return newEntry;
        }

        /// <summary>
        /// Searches transcriptions for a specific RefDoc or by content.
        /// </summary>
        public static IQueryable<RefDocsTranscription> SearchTranscriptionsInRefDocs(NewinvContext ctx, long? refDocId = null, string? keyword = null)
        {
            var query = ctx.RefDocsTranscriptions.AsQueryable();

            if (refDocId.HasValue)
            {
                query = query.Where(t => t.RefDoc == refDocId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t =>
                    (t.TranscribedContent != null && t.TranscribedContent.Contains(keyword)) ||
                    (t.RefDocTitle != null && t.RefDocTitle.Contains(keyword)) ||
                    (t.RefDocSummary != null && t.RefDocSummary.Contains(keyword))
                );
            }

            return query.OrderByDescending(t => t.TranscribedAt);
        }
        /// <summary>
        /// Test method to transcribe 'sample_to_transcribe.jpg' from the Current Working Directory.
        /// Creates a temporary RefDoc entry in the database, transcribes it, and prints the result.
        /// </summary>
        public static async Task TestSampleTranscribe()
        {
            string fileName = "sample_to_transcribe.jpg";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File '{fileName}' not found in CWD: {Directory.GetCurrentDirectory()}");
                return;
            }

            Console.WriteLine($"Reading {fileName}...");
            byte[] imageBytes = File.ReadAllBytes(filePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            using (var ctx = new NewinvContext())
            {
                Console.WriteLine("Creating temporary RefDoc entry...");
                var tempDoc = new RefDoc
                {
                    RefText = "Test Sample Transcription",
                    RefImage = base64Image,
                    CreatedAt = DateTime.UtcNow,
                    AuthoredBy = 0 // System user or test user ID
                };

                ctx.RefDocs.Add(tempDoc);
                await ctx.SaveChangesAsync();
                Console.WriteLine($"Temporary RefDoc created with ID: {tempDoc.RefId}");

                try
                {
                    Console.WriteLine("Starting transcription...");
                    // Use default model (null)
                    var result = await AITranscribe(tempDoc.RefId, null, ctx);

                    Console.WriteLine("Transcription Successful!");
                    Console.WriteLine($"Title: {result.RefDocTitle}");
                    Console.WriteLine($"Summary: {result.RefDocSummary}");
                    Console.WriteLine($"Content: {result.TranscribedContent}");
                    Console.WriteLine($"Structured Type: {result.TranscriptionStructureType}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Transcription Failed: {ex.Message}");
                }
                finally
                {
                    // Cleanup: Optional - remove the test entry
                    // ctx.RefDocs.Remove(tempDoc);
                    // await ctx.SaveChangesAsync();
                    // Console.WriteLine("Temporary RefDoc removed.");
                }
            }
        }
    }
}