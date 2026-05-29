using OpenAI.Images;
using System.Drawing.Imaging;

// The gpt-image sizes/options in the OpenAI .NET SDK are gated behind the OPENAI001
// "evaluation" diagnostic (treated as an error by default). Suppress it so we can
// target the gpt-image-* models, which replaced DALL-E 3 in the OpenAI API.
#pragma warning disable OPENAI001

namespace WallTrek.Services.ImageGen
{
    public class OpenAiImageGenerator : IImageGenerationService
    {
        private readonly ImageClient client;

        public OpenAiImageGenerator(string apiKey, string model)
        {
            client = new ImageClient(model, apiKey);
        }

        public async Task<ImageGenerationResult> GenerateImage(string prompt, CancellationToken cancellationToken = default)
        {
            // gpt-image models always return base64-encoded image bytes (no URL/response_format option)
            // and don't support DALL-E 3's "vivid" style. 1536x1024 is the widest landscape size they
            // offer (3:2) — there is no native 16:9 option, unlike the Google Imagen/Gemini path.
            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1536xH1024
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