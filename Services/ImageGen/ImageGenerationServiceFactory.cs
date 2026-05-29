using WallTrek.Services;

namespace WallTrek.Services.ImageGen
{
    public static class ImageGenerationServiceFactory
    {
        public static IImageGenerationService CreateService(ImageModelOption option)
        {
            var settings = Settings.Instance;

            switch (option.Provider)
            {
                // OpenAI gpt-image family.
                case ImageProvider.OpenAI:
                    if (string.IsNullOrEmpty(settings.ApiKey))
                        throw new InvalidOperationException("OpenAI API key is required for OpenAI image models");
                    return new OpenAiImageGenerator(settings.ApiKey, option.ModelId, option.OpenAiQuality);

                // Google — Imagen (predict endpoint) and Gemini (generateContent endpoint).
                case ImageProvider.Google:
                    if (string.IsNullOrEmpty(settings.GoogleApiKey))
                        throw new InvalidOperationException("Google API key is required for Imagen and Gemini image models");
                    return new GoogleImagenService(settings.GoogleApiKey, option.ModelId, option.ImageSize, option.AspectRatio);

                default:
                    throw new ArgumentException($"Unsupported image provider: {option.Provider}");
            }
        }
    }
}