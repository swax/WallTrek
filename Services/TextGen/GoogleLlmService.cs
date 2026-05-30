using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services.TextGen
{
    /// <summary>
    /// Gemini text generation via the Generative Language generateContent endpoint.
    /// Mirrors <see cref="WallTrek.Services.ImageGen.GoogleImagenService"/>'s raw-HTTP approach
    /// and shares the same Google API key. Structured responses use Gemini's
    /// responseMimeType + responseSchema, with the incoming JSON Schema massaged into the
    /// OpenAPI-subset shape Gemini accepts.
    /// </summary>
    public class GoogleLlmService : ILlmService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

        private readonly string apiKey;
        private readonly string model;

        public GoogleLlmService(string apiKey, string model)
        {
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            var requestPayload = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userPrompt } } }
                },
                generationConfig = new { temperature = 0.7 }
            };

            var responseText = await PostAsync(JsonSerializer.Serialize(requestPayload), cancellationToken);
            return ExtractText(responseText).Trim();
        }

        public async Task<T?> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken cancellationToken = default) where T : class
        {
            var schemaNode = JsonNode.Parse(jsonSchema);
            if (schemaNode == null)
            {
                throw new ArgumentException("Invalid JSON schema provided", nameof(jsonSchema));
            }

            // Gemini's responseSchema is an OpenAPI subset: it uses uppercase type names and
            // rejects JSON Schema artifacts like additionalProperties / $schema.
            var geminiSchema = ToGeminiSchema(schemaNode);

            var requestPayload = new JsonObject
            {
                ["system_instruction"] = new JsonObject
                {
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = systemPrompt } }
                },
                ["contents"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray { new JsonObject { ["text"] = userPrompt } }
                    }
                },
                ["generationConfig"] = new JsonObject
                {
                    ["temperature"] = 0.7,
                    ["responseMimeType"] = "application/json",
                    ["responseSchema"] = geminiSchema
                }
            };

            var responseText = await PostAsync(requestPayload.ToJsonString(), cancellationToken);
            var json = ExtractText(responseText).Trim();

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private async Task<string> PostAsync(string jsonContent, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var url = $"{BaseUrl}/models/{model}:generateContent?key={apiKey}";
            var response = await httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Google Gemini API request failed: {response.StatusCode} - {errorContent}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        private static string ExtractText(string responseContent)
        {
            using var jsonDoc = JsonDocument.Parse(responseContent);

            if (!jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No candidates returned by Google Gemini");
            }

            if (!candidates[0].TryGetProperty("content", out var contentElement)
                || !contentElement.TryGetProperty("parts", out var parts))
            {
                throw new InvalidOperationException("No content parts found in Google Gemini response");
            }

            var sb = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    sb.Append(textElement.GetString());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert a standard JSON Schema node into the form Gemini's responseSchema accepts:
        /// uppercase the "type" values and drop unsupported keywords (additionalProperties, $schema).
        /// </summary>
        private static JsonNode ToGeminiSchema(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var result = new JsonObject();
                foreach (var kvp in obj)
                {
                    if (kvp.Key is "additionalProperties" or "$schema")
                    {
                        continue;
                    }

                    if (kvp.Key == "type" && kvp.Value is JsonValue typeValue && typeValue.TryGetValue<string>(out var typeName))
                    {
                        result["type"] = typeName.ToUpperInvariant();
                    }
                    else if (kvp.Value != null)
                    {
                        result[kvp.Key] = ToGeminiSchema(kvp.Value);
                    }
                }
                return result;
            }

            if (node is JsonArray array)
            {
                var result = new JsonArray();
                foreach (var item in array)
                {
                    result.Add(item == null ? null : ToGeminiSchema(item));
                }
                return result;
            }

            // Leaf value — clone so it can be re-parented under the new tree.
            return node.DeepClone();
        }
    }
}
