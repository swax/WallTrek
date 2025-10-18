using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tool = Anthropic.SDK.Common.Tool;

namespace WallTrek.Services.TextGen
{
    public class AnthropicLlmService : ILlmService
    {
        private readonly AnthropicClient client;
        private readonly string model;

        public AnthropicLlmService(string apiKey, string model)
        {
            client = new AnthropicClient(apiKey);
            this.model = model;
        }

        public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            var messages = new List<Message>
            {
                new Message(RoleType.User, userPrompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = model,
                MaxTokens = 1000,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                Temperature = 0.7m
            };

            var result = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Extract text from the response content
            var textContent = result.Content?.FirstOrDefault(c => c is TextContent) as TextContent;
            return textContent?.Text?.Trim() ?? string.Empty;
        }

        public async Task<T?> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken cancellationToken = default) where T : class
        {
            // For TitleResult, create a specific tool with proper schema
            if (typeof(T) == typeof(TitleResult))
            {
                return await GenerateTitleAndTagsAsync<T>(systemPrompt, userPrompt, cancellationToken);
            }

            // Fallback to text-based JSON parsing for other types
            return await GenerateJsonFallbackAsync<T>(systemPrompt, userPrompt, jsonSchema, cancellationToken);
        }

        private async Task<T?> GenerateTitleAndTagsAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken) where T : class
        {
            // Create a tool that matches TitleResult schema using manual approach
            var inputSchema = new
            {
                type = "object",
                properties = new
                {
                    title = new { type = "string", description = "Short, catchy title for the image" },
                    tags = new
                    {
                        type = "array",
                        description = "Relevant tags for the image",
                        items = new { type = "string" }
                    }
                },
                required = new[] { "title", "tags" }
            };

            var jsonSerializationOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
            };

            string jsonString = JsonSerializer.Serialize(inputSchema, jsonSerializationOptions);
            var tools = new List<Tool>
            {
                (Tool)new Function("GenerateTitleAndTags", "Generate a title and tags for an AI-generated image", JsonNode.Parse(jsonString))
            };

            var messages = new List<Message>
            {
                new Message(RoleType.User, $"{userPrompt} Use the GenerateTitleAndTags tool ONLY.")
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = model,
                MaxTokens = 1000,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                Tools = tools,
                ToolChoice = new ToolChoice
                {
                    Type = ToolChoiceType.Tool,
                    Name = "GenerateTitleAndTags"
                }
            };

            var result = await client.Messages.GetClaudeMessageAsync(parameters);

            // Look for the tool use in the response
            var toolUse = result.Content?
                .OfType<ToolUseContent>()
                .FirstOrDefault(t => string.Equals(t.Name, "GenerateTitleAndTags", StringComparison.Ordinal));

            if (toolUse != null)
            {
                var json = toolUse.Input.ToJsonString();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return null;
        }

        private async Task<T?> GenerateJsonFallbackAsync<T>(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken cancellationToken) where T : class
        {
            var enhancedPrompt = $"{userPrompt}\n\nPlease respond with valid JSON that matches this schema:\n{jsonSchema}";
            
            var messages = new List<Message>
            {
                new Message(RoleType.User, enhancedPrompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = model,
                MaxTokens = 1000,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                Temperature = 0.1m // Lower temperature for more consistent JSON
            };

            var result = await client.Messages.GetClaudeMessageAsync(parameters);
            
            var textContent = result.Content?.FirstOrDefault(c => c is TextContent) as TextContent;
            if (textContent?.Text != null)
            {
                var json = textContent.Text.Trim();
                
                // Extract JSON if it's wrapped in code blocks
                if (json.StartsWith("```json"))
                {
                    var start = json.IndexOf('{');
                    var end = json.LastIndexOf('}');
                    if (start >= 0 && end >= 0)
                    {
                        json = json.Substring(start, end - start + 1);
                    }
                }
                else if (json.StartsWith("```"))
                {
                    // Handle other code block types
                    var lines = json.Split('\n');
                    if (lines.Length > 1)
                    {
                        json = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
                    }
                }
                
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            
            return null;
        }
    }
}