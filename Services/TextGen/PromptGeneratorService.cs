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
      var selectedProperties = new List<string>();

      // Check if we should use random words
      if (settings.AddRandomWords)
      {
        var randomWordService = new RandomWordService();
        var randomWords = await randomWordService.GetRandomWordsAsync(settings.RandomWordCount, cancellationToken);
        selectedProperties.AddRange(randomWords);
      }

      // Build selections from all available prompt categories
      foreach (var category in randomPrompts)
      {
        if (category.Value?.Length > 0)
        {
          var selectedValue = category.Value[random.Next(category.Value.Length)];
          selectedProperties.Add($"{category.Key}: {selectedValue}");
        }
      }

      // Build the selections string
      var selections = selectedProperties.Count > 0
          ? string.Join(", ", selectedProperties)
          : "pink flamingos, tropical beach, sunset with a sign in the sand saying 'configuration error'";

      var systemPrompt = $"You are a creative assistant that generates diverse desktop wallpaper descriptions. " +
        $"Create a unique wallpaper prompt, title, and tags taking inspiration from these properties: {selections}. " +
        $"Be creative and avoid generic descriptions. The prompt should include specific details about colors, composition, and visual elements. " +
        $"Keep the prompt concise (1-2 sentences). The title should be catchy and descriptive, less than {Settings.TitleCharacterMax} characters. Provide 5-10 relevant tags for categorization.";

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

      var result = await llmService.GenerateStructuredResponseAsync<PromptGenerationResult>(systemPrompt, userPrompt, jsonSchema, cancellationToken);

      if (result != null)
      {
        result.SelectedProperties = selectedProperties;
      }

      return result;
    }
  }
}