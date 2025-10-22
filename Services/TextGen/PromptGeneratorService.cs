using System;
using System.Threading;
using System.Threading.Tasks;
using WallTrek.Services;

namespace WallTrek.Services.TextGen
{
    public class PromptGeneratorService
    {
        public async Task<PromptGenerationResult?> GenerateRandomPromptAsync(CancellationToken cancellationToken = default)
        {
            var settings = Settings.Instance;
            var randomPrompts = settings.RandomPrompts;

            // Create LLM service based on selected model
            var llmService = LlmServiceFactory.CreateService(
                settings.SelectedLlmModel,
                settings.ApiKey ?? string.Empty,
                settings.AnthropicApiKey ?? string.Empty
            );

            // Randomize the prompt generation approach
            var random = new Random();
            var selectedProperties = new Dictionary<string, string>();

            // Build selections from all available prompt categories
            foreach (var category in randomPrompts)
            {
                if (category.Value?.Length > 0)
                {
                    var selectedValue = category.Value[random.Next(category.Value.Length)];
                    selectedProperties[category.Key] = selectedValue;
                }
            }

            // Build the selections string
            var selections = selectedProperties.Count > 0
                ? string.Join(", ", selectedProperties.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "abstract art, digital art, vibrant";

            var systemPrompt = $"You are a creative assistant that generates diverse desktop wallpaper descriptions. Create a unique wallpaper prompt, title, and tags taking inspiration from these properties: {selections}. Be creative and avoid generic descriptions. The prompt should include specific details about colors, composition, and visual elements. Keep the prompt concise (1-2 sentences). The title should be catchy and descriptive. Provide 5-10 relevant tags for categorization.";

            var userPrompt = $"Generate a creative desktop wallpaper prompt incorporating these elements: {selections}. Make it visually striking and unique. Also provide a suitable title and relevant tags.";

            var jsonSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""prompt"": {
      ""type"": ""string"",
      ""description"": ""The detailed wallpaper generation prompt (1-2 sentences)""
    },
    ""title"": {
      ""type"": ""string"",
      ""description"": ""A catchy, descriptive title for the wallpaper""
    },
    ""tags"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""string""
      },
      ""description"": ""Array of 5-10 relevant tags for categorization""
    }
  },
  ""required"": [""prompt"", ""title"", ""tags""]
}";

            return await llmService.GenerateStructuredResponseAsync<PromptGenerationResult>(systemPrompt, userPrompt, jsonSchema, cancellationToken);
        }
    }
}