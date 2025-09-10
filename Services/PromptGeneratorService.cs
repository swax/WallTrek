using OpenAI.Chat;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services
{
    public class PromptGeneratorService
    {
        private readonly string apiKey;

        public PromptGeneratorService(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> GenerateRandomPromptAsync(CancellationToken cancellationToken = default)
        {
            var settings = Settings.Instance;
            var chatClient = new ChatClient(settings.SelectedLlmModel, apiKey);
            var randomPrompts = settings.RandomPrompts;
            
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

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Generate a creative desktop wallpaper prompt with {selectedCategory} theme, {selectedStyle} style, and {selectedMood} mood. Make it visually striking and unique.")
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return response.Value.Content[0].Text.Trim();
        }
    }
}