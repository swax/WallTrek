using System;
using System.Threading;
using System.Threading.Tasks;
using WallTrek.Services;

namespace WallTrek.Services.TextGen
{
    public class PromptGeneratorService
    {
        public async Task<string> GenerateRandomPromptAsync(CancellationToken cancellationToken = default)
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

            var systemPrompt = $"You are a creative assistant that generates diverse desktop wallpaper descriptions. Create a unique wallpaper prompt taking inspiration from these properties: {selections}. Be creative and avoid generic descriptions. Include specific details about colors, composition, and visual elements. Keep it short and concise to like sentence or two.";

            var userPrompt = $"Generate a creative desktop wallpaper prompt incorporating these elements: {selections}. Make it visually striking and unique.";

            return await llmService.GenerateTextAsync(systemPrompt, userPrompt, cancellationToken);
        }
    }
}