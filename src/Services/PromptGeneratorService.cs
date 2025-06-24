using OpenAI.Chat;
using System;
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

        public async Task<string> GenerateRandomPromptAsync()
        {
            var chatClient = new ChatClient("o3", apiKey);
            
            // Randomize the prompt generation approach
            var random = new Random();
            var categories = new[]
            {
                "abstract geometric patterns and mathematical art",
                "natural landscapes and organic forms", 
                "futuristic cyberpunk and sci-fi environments",
                "minimalist design and negative space",
                "fantasy worlds and magical creatures",
                "architectural marvels and urban scenes",
                "cosmic and astronomical phenomena",
                "vintage and retro aesthetic designs",
                "surreal and dreamlike compositions",
                "seasonal and weather-based imagery"
            };

            var styles = new[]
            {
                "oil painting", "watercolor", "digital art", "photography", "3D render",
                "vector illustration", "pixel art", "paper cut-out", "neon lighting",
                "pencil sketch", "abstract expressionism", "art nouveau", "bauhaus design"
            };

            var moods = new[]
            {
                "serene and calming", "energetic and vibrant", "mysterious and moody",
                "bright and cheerful", "dramatic and intense", "peaceful and meditative",
                "bold and striking", "subtle and elegant", "warm and cozy", "cool and refreshing"
            };

            var selectedCategory = categories[random.Next(categories.Length)];
            var selectedStyle = styles[random.Next(styles.Length)];
            var selectedMood = moods[random.Next(moods.Length)];

            var systemPrompt = $"You are a creative assistant that generates diverse desktop wallpaper descriptions. Create a unique wallpaper prompt taking inspiration from {selectedCategory} in a {selectedStyle} style with a {selectedMood} atmosphere. Be creative and avoid generic descriptions. Include specific details about colors, composition, and visual elements. Keep it short and concise to like sentence or two.";

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Generate a creative desktop wallpaper prompt with {selectedCategory} theme, {selectedStyle} style, and {selectedMood} mood. Make it visually striking and unique.")
            };

            var response = await chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
    }
}