using OpenAI.Chat;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services
{
    public class TitleResult
    {
        public string Title { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class TitleService
    {
        private readonly string apiKey;

        private const string Model = "gpt-5";

        public TitleService(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<TitleResult?> GenerateTitleAndTagsAsync(
            string imageDescription,
            CancellationToken cancellationToken = default)
        {
            var chatClient = new ChatClient(Model, apiKey);

            var systemPrompt =
                "You are a creative assistant that generates titles and tags for AI-generated images. Create short titles that capture the essence of the image description. The titles should be suitable for art sharing platforms like DeviantArt.";

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(
                    $"Generate a DeviantArt-friendly short title and tags that cover style, mood, colors, subjects, and artistic elements for the description: {imageDescription}")
            };

            // JSON Schema describing the object we want back
            var schemaJson = BinaryData.FromBytes("""
            {
              "type": "object",
              "properties": {
                "title": {
                  "type": "string",
                  "description": "Short, catchy title"
                },
                "tags": {
                  "type": "array",
                  "description": "Relevant tags",
                  "items": { "type": "string" }
                }
              },
              "required": ["title", "tags"],
              "additionalProperties": false
            }
            """u8.ToArray());

            var options = new ChatCompletionOptions
            {
                // Enforce the schema with strict decoding
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "title_and_tags",
                    jsonSchema: schemaJson,
                    jsonSchemaIsStrict: true)
            };

            var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            // With structured outputs, the first content part is guaranteed to be valid JSON per the schema
            var json = completion.Value.Content[0].Text;

            // Optional: deserialize into your POCO
            var result = JsonSerializer.Deserialize<TitleResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
    }
}
