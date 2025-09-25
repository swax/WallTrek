using OpenAI.Images;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace WallTrek.Services
{
    public class ImageGenerator : IImageGenerationService
    {
        private readonly string outputDirectory;
        private readonly ImageClient client;
        private readonly DatabaseService databaseService;

        public ImageGenerator(string apiKey, string outputDirectory)
        {
            this.outputDirectory = outputDirectory;
            client = new ImageClient("dall-e-3", apiKey);
            databaseService = new DatabaseService();
        }

        public async Task<string> GenerateAndSaveImage(string prompt, CancellationToken cancellationToken = default)
        {
            const int maxFileNamePromptLength = 75;
            var sanitizedPrompt = string.Join("_", prompt.Split(Path.GetInvalidFileNameChars()));
            if (sanitizedPrompt.Length > maxFileNamePromptLength)
            {
                sanitizedPrompt = sanitizedPrompt.Substring(0, maxFileNamePromptLength);
            }
            
            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                Style = GeneratedImageStyle.Vivid,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            GeneratedImage image = await client.GenerateImageAsync(prompt, options, cancellationToken);
            BinaryData bytes = image.ImageBytes;

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss} ({sanitizedPrompt}).png";
            var filePath = Path.Combine(outputDirectory, fileName);

            // Save the image with metadata
            using (var memoryStream = new MemoryStream())
            {
                bytes.ToStream().CopyTo(memoryStream);
                memoryStream.Position = 0;
                
                using (var bitmap = new Bitmap(memoryStream))
                {
                    // Add prompt to image metadata, need to use exif tool to read
                    var propertyItem = (PropertyItem?)Activator.CreateInstance(typeof(PropertyItem), true);
                    if (propertyItem != null)
                    {
                        propertyItem.Id = 0x010E; // ImageDescription EXIF tag
                        propertyItem.Type = 2; // ASCII string
                        var promptBytes = Encoding.UTF8.GetBytes(prompt + "\0");
                        propertyItem.Value = promptBytes;
                        propertyItem.Len = promptBytes.Length;
                        
                        bitmap.SetPropertyItem(propertyItem);
                    }
                    bitmap.Save(filePath, ImageFormat.Png);
                }
            }

            // Add to database after successful generation
            try
            {
                var promptId = await databaseService.AddOrUpdatePromptAsync(prompt);
                await databaseService.AddGeneratedImageAsync(promptId, filePath);
            }
            catch (Exception ex)
            {
                // Log database error but don't fail the image generation
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
            }

            return filePath;
        }
    }
}