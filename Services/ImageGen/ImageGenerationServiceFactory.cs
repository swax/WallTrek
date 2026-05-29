using WallTrek.Services;

namespace WallTrek.Services.ImageGen
{
    public static class ImageGenerationServiceFactory
    {
        public static IImageGenerationService CreateService(string imageModel)
        {
            var settings = Settings.Instance;

            // OpenAI image models (gpt-image family; "dall-e" kept for any legacy settings).
            if (imageModel.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase)
                || imageModel.StartsWith("dall-e", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(settings.ApiKey))
                    throw new InvalidOperationException("OpenAI API key is required for OpenAI image models");
                return new OpenAiImageGenerator(settings.ApiKey, imageModel);
            }

            // Google image models — Imagen (predict endpoint) and Gemini (generateContent endpoint).
            if (imageModel.StartsWith("imagen", StringComparison.OrdinalIgnoreCase)
                || imageModel.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(settings.GoogleApiKey))
                    throw new InvalidOperationException("Google API key is required for Imagen and Gemini image models");
                return new GoogleImagenService(settings.GoogleApiKey, imageModel);
            }

            throw new ArgumentException($"Unsupported image model: {imageModel}");
        }
    }
}