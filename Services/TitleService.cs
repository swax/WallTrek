using System;
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
        public async Task<TitleResult?> GenerateTitleAndTagsAsync(
            string imageDescription,
            CancellationToken cancellationToken = default)
        {
            var settings = Settings.Instance;
            
            // Create LLM service based on selected model
            var llmService = LlmServiceFactory.CreateService(
                settings.SelectedLlmModel,
                settings.ApiKey ?? string.Empty,
                settings.AnthropicApiKey ?? string.Empty
            );

            var systemPrompt =
                "You are a creative assistant that generates titles and tags for AI-generated images. Create a short title that captures the essence of the image description. The title should be suitable for art sharing platforms like DeviantArt.";

            var userPrompt = $"Generate a DeviantArt-friendly short title and tags that cover the style, mood, colors, subjects, and artistic elements for an image with this description: {imageDescription}. No more than 15 tags. Thanks!";

            // JSON Schema describing the object we want back
            var jsonSchema = """
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
            """;

            return await llmService.GenerateStructuredResponseAsync<TitleResult>(systemPrompt, userPrompt, jsonSchema, cancellationToken);
        }
    }
}
