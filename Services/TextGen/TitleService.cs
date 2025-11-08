using System;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using WallTrek.Services;

namespace WallTrek.Services.TextGen
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
          "You are a creative assistant that generates titles and tags for AI-generated images. Create a short title, that captures the essence of the image description. The title should be suitable for art sharing platforms like DeviantArt.";

      var userPrompt = $"Generate a DeviantArt-friendly short title (less than {Settings.TitleCharacterMax} characters) and tags that cover the style, mood, colors, subjects, and artistic elements for an image with this description: {imageDescription}. No more than {Settings.MaxTags} tags. Thanks!";

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

      var response = await llmService.GenerateStructuredResponseAsync<TitleResult>(systemPrompt, userPrompt, jsonSchema, cancellationToken);

      if (response != null)
      {
        // Ensure title length constraint
        if (response.Title.Length > Settings.TitleCharacterMax)
        {
          response.Title = response.Title.Substring(0, Settings.TitleCharacterMax);
        }

        // Ensure max tags constraint
        if (response.Tags.Length > Settings.MaxTags)
        {
          var tags = response.Tags;
          Array.Resize(ref tags, Settings.MaxTags);
          response.Tags = tags;
        }
      }

      return response;
    }
  }
}
