using System.Drawing;
using System.Drawing.Imaging;

namespace WallTrek.Services.ImageGen
{
    public class UpscaleService
    {
        private readonly string? apiKey;
        private readonly HttpClient httpClient;
        private const string BaseUrl = "https://api.stability.ai/v2beta/stable-image/upscale/fast";
        private const int PixelMax = 1_048_576; // Maximum pixels allowed by Stability AI API

        public UpscaleService(string? apiKey)
        {
            this.apiKey = apiKey;
            this.httpClient = new HttpClient();
        }

        public async Task<byte[]> UpscaleImageAsync(byte[] imageData, ImageFormat format, CancellationToken cancellationToken = default)
        {
            // If no API key is configured, return the original image
            if (string.IsNullOrEmpty(apiKey))
            {
                return imageData;
            }

            // Crop image if it exceeds the pixel max
            imageData = CropImageIfNeeded(imageData, format);

            // Determine output format based on ImageFormat
            string outputFormat = GetOutputFormat(format);

            // Create multipart form data
            using var formData = new MultipartFormDataContent();

            // Add image with explicit Content-Disposition
            var imageStream = new MemoryStream(imageData);
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(format));
            imageContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "\"image\"",
                FileName = $"\"image.{GetExtension(format)}\""
            };
            formData.Add(imageContent);

            // Add output format with explicit Content-Disposition
            var outputFormatContent = new StringContent(outputFormat);
            outputFormatContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "\"output_format\""
            };
            formData.Add(outputFormatContent);

            // Set authorization header
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "image/*");

            // Make API request
            var response = await httpClient.PostAsync(BaseUrl, formData, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Stability AI upscale API request failed: {response.StatusCode} - {errorContent}");
            }

            // Check for content filtering
            if (response.Headers.TryGetValues("finish-reason", out var finishReasons))
            {
                var finishReason = finishReasons.FirstOrDefault();
                if (finishReason == "CONTENT_FILTERED")
                {
                    throw new InvalidOperationException("Image upscaling failed NSFW classifier");
                }
            }

            // Read and return upscaled image
            return await response.Content.ReadAsByteArrayAsync();
        }

        private byte[] CropImageIfNeeded(byte[] imageData, ImageFormat format)
        {
            using var ms = new MemoryStream(imageData);
            using var image = Image.FromStream(ms);

            int totalPixels = image.Width * image.Height;

            // If image is within pixel max, return original
            if (totalPixels <= PixelMax)
            {
                return imageData;
            }

            // Calculate new width needed to meet pixel max (keeping same height)
            int newWidth = PixelMax / image.Height;

            // Calculate how much to crop from each side
            int totalCrop = image.Width - newWidth;
            int cropLeft = totalCrop / 2;
            int cropRight = totalCrop - cropLeft; // Handle odd numbers

            // Create cropped image
            using var croppedImage = new Bitmap(newWidth, image.Height);
            using (var g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(image,
                    new Rectangle(0, 0, newWidth, image.Height),
                    new Rectangle(cropLeft, 0, newWidth, image.Height),
                    GraphicsUnit.Pixel);
            }

            // Convert back to byte array with original format
            using var outputMs = new MemoryStream();
            croppedImage.Save(outputMs, format);
            return outputMs.ToArray();
        }

        private string GetOutputFormat(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Png))
                return "png";
            if (format.Equals(ImageFormat.Jpeg))
                return "jpeg";
            if (format.Equals(ImageFormat.Webp))
                return "webp";

            return "png"; // Default to PNG
        }

        private string GetMimeType(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Png))
                return "image/png";
            if (format.Equals(ImageFormat.Jpeg))
                return "image/jpeg";
            if (format.Equals(ImageFormat.Webp))
                return "image/webp";

            return "image/png";
        }

        private string GetExtension(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Png))
                return "png";
            if (format.Equals(ImageFormat.Jpeg))
                return "jpg";
            if (format.Equals(ImageFormat.Webp))
                return "webp";

            return "png";
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
