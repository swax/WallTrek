using OpenAI.Images;

namespace WallTrek.Services
{
    public class ImageGenerator
    {
        private readonly string outputDirectory;
        private readonly ImageClient client;

        public ImageGenerator(string apiKey, string outputDirectory)
        {
            this.outputDirectory = outputDirectory;
            client = new ImageClient("dall-e-3", apiKey);
        }

        public async Task<string> GenerateAndSaveImage(string prompt)
        {
            var sanitizedPrompt = string.Join("_", prompt.Split(Path.GetInvalidFileNameChars()));
            if (sanitizedPrompt.Length > 50) sanitizedPrompt = sanitizedPrompt.Substring(0, 50);

            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                Style = GeneratedImageStyle.Vivid,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            GeneratedImage image = await Task.Run(() => client.GenerateImage(prompt, options));
            BinaryData bytes = image.ImageBytes;

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss} ({sanitizedPrompt}).png";
            var filePath = Path.Combine(outputDirectory, fileName);

            // Save the image
            using (var stream = File.OpenWrite(filePath))
            {
                bytes.ToStream().CopyTo(stream);
            }

            return filePath;
        }
    }
}
