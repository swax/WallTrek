using OpenAI.Chat;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services
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

            var schemaJson = BinaryData.FromBytes(System.Text.Encoding.UTF8.GetBytes(jsonSchema));
            
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
    }
}