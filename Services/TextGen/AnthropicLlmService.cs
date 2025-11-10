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
            // Parse the provided JSON schema
            var schemaNode = JsonNode.Parse(jsonSchema);
            if (schemaNode == null)
            {
                throw new ArgumentException("Invalid JSON schema provided", nameof(jsonSchema));
            }

            // Use the schema directly as a tool input schema
            var toolName = $"Generate{typeof(T).Name}";
            var toolDescription = $"Generate structured data for {typeof(T).Name}";

            var tools = new List<Tool>
            {
                (Tool)new Function(toolName, toolDescription, schemaNode)
            };

            var messages = new List<Message>
            {
                new Message(RoleType.User, userPrompt)
            };

            bool useThinking = true;

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = model,
                MaxTokens = 10000,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                Tools = tools,
                ToolChoice = useThinking ? new ToolChoice
                {
                    Type = ToolChoiceType.Auto
                } : new ToolChoice
                {
                    Type = ToolChoiceType.Any,
                    Name = toolName
                }
            };

            if (useThinking)
            {
                parameters.Thinking = new ThinkingParameters
                {
                    BudgetTokens = 5000
                };
            }

            var result = await client.Messages.GetClaudeMessageAsync(parameters);

            // Look for the tool use in the response
            var toolUse = result.Content?
                .OfType<ToolUseContent>()
                .FirstOrDefault(t => string.Equals(t.Name, toolName, StringComparison.Ordinal));

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
    }
}