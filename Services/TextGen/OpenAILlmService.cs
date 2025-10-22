using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services.TextGen
{
    public class OpenAILlmService : ILlmService
    {
        private readonly string apiKey;
        private readonly string model;

        public OpenAILlmService(string apiKey, string model)
        {
            this.apiKey = apiKey;
            this.model = model;
        }

        public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            var chatClient = new ChatClient(model, apiKey);
            
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return response.Value.Content[0].Text.Trim();
        }

        public async Task<T?> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken cancellationToken = default) where T : class
        {
            var chatClient = new ChatClient(model, apiKey);

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            // Parse the schema and ensure it has additionalProperties: false for OpenAI strict mode
            var schemaNode = JsonNode.Parse(jsonSchema);
            if (schemaNode != null)
            {
                EnsureAdditionalPropertiesFalse(schemaNode);
            }

            var modifiedSchema = schemaNode?.ToJsonString() ?? jsonSchema;
            var schemaJson = BinaryData.FromBytes(System.Text.Encoding.UTF8.GetBytes(modifiedSchema));

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "structured_response",
                    jsonSchema: schemaJson,
                    jsonSchemaIsStrict: true)
            };

            var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var json = completion.Value.Content[0].Text;

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private void EnsureAdditionalPropertiesFalse(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                // If this object has a "properties" field, it needs "additionalProperties": false
                if (obj.ContainsKey("properties") && obj["properties"] is JsonObject)
                {
                    if (!obj.ContainsKey("additionalProperties"))
                    {
                        obj["additionalProperties"] = false;
                    }
                }

                // Recursively process all nested objects
                foreach (var kvp in obj)
                {
                    if (kvp.Value != null)
                    {
                        EnsureAdditionalPropertiesFalse(kvp.Value);
                    }
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        EnsureAdditionalPropertiesFalse(item);
                    }
                }
            }
        }
    }
}