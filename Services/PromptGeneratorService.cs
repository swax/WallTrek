using System;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services
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
            
            var selectedCategory = randomPrompts.Categories.Count > 0 
                ? randomPrompts.Categories[random.Next(randomPrompts.Categories.Count)]
                : "abstract art";
                
            var selectedStyle = randomPrompts.Styles.Count > 0 
                ? randomPrompts.Styles[random.Next(randomPrompts.Styles.Count)]
                : "digital art";
                
            var selectedMood = randomPrompts.Moods.Count > 0 
                ? randomPrompts.Moods[random.Next(randomPrompts.Moods.Count)]
                : "vibrant";

            var systemPrompt = $"You are a creative assistant that generates diverse desktop wallpaper descriptions. Create a unique wallpaper prompt taking inspiration from {selectedCategory} in a {selectedStyle} style with a {selectedMood} atmosphere. Be creative and avoid generic descriptions. Include specific details about colors, composition, and visual elements. Keep it short and concise to like sentence or two.";

            var userPrompt = $"Generate a creative desktop wallpaper prompt with {selectedCategory} theme, {selectedStyle} style, and {selectedMood} mood. Make it visually striking and unique.";

            return await llmService.GenerateTextAsync(systemPrompt, userPrompt, cancellationToken);
        }
    }
}