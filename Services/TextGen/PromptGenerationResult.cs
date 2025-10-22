namespace WallTrek.Services.TextGen
{
    public class PromptGenerationResult
    {
        public string Prompt { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
