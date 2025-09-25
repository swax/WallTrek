namespace WallTrek.Services
{
    public static class ImageGenerationServiceFactory
    {
        public static IImageGenerationService CreateService(string imageModel, string outputDirectory)
        {
            var settings = Settings.Instance;
            
            switch (imageModel)
            {
                case "dalle-3":
                    if (string.IsNullOrEmpty(settings.ApiKey))
                        throw new InvalidOperationException("OpenAI API key is required for DALL-E 3");
                    return new ImageGenerator(settings.ApiKey, outputDirectory);
                
                case "imagen-4.0-generate-001":
                case "imagen-4.0-ultra-generate-001":
                    if (string.IsNullOrEmpty(settings.GoogleApiKey))
                        throw new InvalidOperationException("Google API key is required for Imagen models");
                    return new GoogleImagenService(settings.GoogleApiKey, outputDirectory);
                
                default:
                    throw new ArgumentException($"Unsupported image model: {imageModel}");
            }
        }
    }
}