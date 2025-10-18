using OpenAI.Images;
using System.Drawing.Imaging;

namespace WallTrek.Services.ImageGen
{
    public class OpenAiImageGenerator : IImageGenerationService
    {
        private readonly ImageClient client;

        public OpenAiImageGenerator(string apiKey)
        {
            client = new ImageClient("dall-e-3", apiKey);
        }

        public async Task<ImageGenerationResult> GenerateImage(string prompt, CancellationToken cancellationToken = default)
        {
            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                Style = GeneratedImageStyle.Vivid,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            GeneratedImage image = await client.GenerateImageAsync(prompt, options, cancellationToken);
            BinaryData bytes = image.ImageBytes;

            return new ImageGenerationResult
            {
                ImageData = bytes.ToArray(),
                Format = ImageFormat.Png
            };
        }
    }
}