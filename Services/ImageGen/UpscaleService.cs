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
        private readonly string logFilePath;

        public UpscaleService(string? apiKey)
        {
            this.apiKey = apiKey;
            this.httpClient = new HttpClient();

            // Set up log file path in AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var wallTrekPath = Path.Combine(appDataPath, "WallTrek");
            Directory.CreateDirectory(wallTrekPath);
            this.logFilePath = Path.Combine(wallTrekPath, "upscale.log");
        }

        public async Task<byte[]> UpscaleImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
        {
            var format = ImageFormat.Jpeg;

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
            var upscaledImageData = await response.Content.ReadAsByteArrayAsync();

            // Log final upscaled dimensions
            LogUpscaledDimensions(upscaledImageData);

            return upscaledImageData;
        }

        private byte[] CropImageIfNeeded(byte[] imageData, ImageFormat format)
        {
            using var ms = new MemoryStream(imageData);
            using var image = Image.FromStream(ms);

            int originalWidth = image.Width;
            int originalHeight = image.Height;
            int totalPixels = originalWidth * originalHeight;
            double originalAspectRatio = (double)originalWidth / originalHeight;

            // Log original dimensions
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Original image: {originalWidth}x{originalHeight}, Aspect ratio: {originalAspectRatio:F4}, Total pixels: {totalPixels:N0}";

            // If image is within pixel max, return original
            if (totalPixels <= PixelMax)
            {
                logEntry += " - No processing needed";
                LogToFile(logEntry);
                return imageData;
            }

            // Step 1: Calculate crop dimensions for 16:9 aspect ratio from original image
            const double targetAspectRatio = 16.0 / 9.0;
            int cropWidth, cropHeight;

            if (originalAspectRatio > targetAspectRatio)
            {
                // Image is wider than 16:9, crop width
                cropHeight = originalHeight;
                cropWidth = (int)(cropHeight * targetAspectRatio);
            }
            else
            {
                // Image is taller than 16:9, crop height
                cropWidth = originalWidth;
                cropHeight = (int)(cropWidth / targetAspectRatio);
            }

            // Step 2: Calculate resize dimensions so that width * height < PixelMax
            int resizeWidth, resizeHeight;
            int cropPixels = cropWidth * cropHeight;

            if (cropPixels <= PixelMax)
            {
                // Already under pixel max after crop, no resize needed
                resizeWidth = originalWidth;
                resizeHeight = originalHeight;
            }
            else
            {
                // Calculate scale factor to get under PixelMax
                double scaleFactor = Math.Sqrt((double)PixelMax / cropPixels);
                resizeWidth = (int)(originalWidth * scaleFactor);
                resizeHeight = (int)(originalHeight * scaleFactor);
            }

            logEntry += $"\nTarget 16:9 dimensions: {cropWidth}x{cropHeight}";

            // Step 3: Resize the original image first
            Bitmap resizedImage;
            if (resizeWidth != originalWidth || resizeHeight != originalHeight)
            {
                logEntry += $"\nResized to: {resizeWidth}x{resizeHeight}";
            
                resizedImage = new Bitmap(resizeWidth, resizeHeight);
                using (var g = Graphics.FromImage(resizedImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, resizeWidth, resizeHeight);
                }
            }
            else
            {
                // No resize needed, create a copy
                resizedImage = new Bitmap(image);
            }

            // Step 4: Crop the resized image to 16:9
            // Recalculate crop dimensions based on resized image
            int finalCropWidth, finalCropHeight;
            double resizedAspectRatio = (double)resizeWidth / resizeHeight;

            if (resizedAspectRatio > targetAspectRatio)
            {
                // Image is wider than 16:9, crop width
                finalCropHeight = resizeHeight;
                finalCropWidth = (int)(finalCropHeight * targetAspectRatio);
            }
            else
            {
                // Image is taller than 16:9, crop height
                finalCropWidth = resizeWidth;
                finalCropHeight = (int)(finalCropWidth / targetAspectRatio);
            }

            // Calculate crop offsets to center the crop
            int cropLeft = (resizeWidth - finalCropWidth) / 2;
            int cropTop = (resizeHeight - finalCropHeight) / 2;

            using var croppedImage = new Bitmap(finalCropWidth, finalCropHeight);
            using (var g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(resizedImage,
                    new Rectangle(0, 0, finalCropWidth, finalCropHeight),
                    new Rectangle(cropLeft, cropTop, finalCropWidth, finalCropHeight),
                    GraphicsUnit.Pixel);
            }

            resizedImage.Dispose();

            logEntry += $"\nCropped to: {finalCropWidth}x{finalCropHeight}. Achieving target aspect ratio within pixel limit.";
            LogToFile(logEntry);

            // Convert back to byte array with original format
            using var outputMs = new MemoryStream();
            croppedImage.Save(outputMs, format);
            return outputMs.ToArray();
        }

        private void LogUpscaledDimensions(byte[] upscaledImageData)
        {
            try
            {
                using var ms = new MemoryStream(upscaledImageData);
                using var image = Image.FromStream(ms);

                var logEntry = $"Upscaled to: {image.Width}x{image.Height}, Total pixels: {image.Width * image.Height:N0}\n";
                LogToFile(logEntry);
            }
            catch (Exception ex)
            {
                LogToFile($"Error logging upscaled dimensions: {ex.Message}\n");
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, message + "\n");
            }
            catch
            {
                // Silently fail if logging doesn't work - don't want to break upscaling
            }
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
