using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace WallTrek.Services.ImageGen
{
    public class UpscaleService
    {
        private readonly string? apiKey;
        private readonly HttpClient httpClient;
        private const string FastUrl = "https://api.stability.ai/v2beta/stable-image/upscale/fast";
        private const string ConservativeUrl = "https://api.stability.ai/v2beta/stable-image/upscale/conservative";
        private const string CreativeUrl = "https://api.stability.ai/v2beta/stable-image/upscale/creative";
        private const string ResultsUrl = "https://api.stability.ai/v2beta/results/";
        private const int PixelMax = 1_048_576; // Stability fast/creative upscalers require <= 1 MP input
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

        public async Task<byte[]> UpscaleImageAsync(byte[] imageData, UpscaleMode mode, string prompt, CancellationToken cancellationToken = default)
        {
            // No upscaling requested, or no key configured: return the original image untouched.
            if (mode == UpscaleMode.None || string.IsNullOrEmpty(apiKey))
            {
                return imageData;
            }

            var format = ImageFormat.Jpeg;

            // Fast and creative upscalers require <= 1 MP input; conservative tolerates larger, but
            // downscaling first is harmless. Crop to 16:9 within the limit in all cases.
            imageData = CropImageIfNeeded(imageData, format);
            string outputFormat = GetOutputFormat(format);

            return mode switch
            {
                UpscaleMode.Fast => await PostSyncUpscaleAsync(FastUrl, imageData, format, outputFormat, prompt: null, cancellationToken),
                UpscaleMode.Conservative => await PostSyncUpscaleAsync(ConservativeUrl, imageData, format, outputFormat, prompt, cancellationToken),
                UpscaleMode.Creative => await CreativeUpscaleAsync(imageData, format, outputFormat, prompt, cancellationToken),
                _ => imageData
            };
        }

        // Builds the multipart form (image + output_format, plus an optional guiding prompt that the
        // conservative and creative upscalers require).
        private MultipartFormDataContent BuildForm(byte[] imageData, ImageFormat format, string outputFormat, string? prompt)
        {
            var formData = new MultipartFormDataContent();

            var imageStream = new MemoryStream(imageData);
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(format));
            imageContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "\"image\"",
                FileName = $"\"image.{GetExtension(format)}\""
            };
            formData.Add(imageContent);

            AddField(formData, "output_format", outputFormat);

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                AddField(formData, "prompt", prompt);
            }

            return formData;
        }

        private static void AddField(MultipartFormDataContent form, string name, string value)
        {
            var content = new StringContent(value);
            content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\""
            };
            form.Add(content);
        }

        private void SetAuthHeaders(string accept)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", accept);
        }

        // Fast and conservative upscalers return the image synchronously.
        private async Task<byte[]> PostSyncUpscaleAsync(string url, byte[] imageData, ImageFormat format, string outputFormat, string? prompt, CancellationToken cancellationToken)
        {
            using var formData = BuildForm(imageData, format, outputFormat, prompt);
            SetAuthHeaders("image/*");

            var response = await httpClient.PostAsync(url, formData, cancellationToken);
            await EnsureNotFilteredOrFailed(response);

            var upscaledImageData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            LogUpscaledDimensions(upscaledImageData);
            return upscaledImageData;
        }

        // The creative upscaler is asynchronous: POST returns a generation id, then we poll
        // /v2beta/results/{id} (202 = still running, 200 = finished image) until it is ready.
        private async Task<byte[]> CreativeUpscaleAsync(byte[] imageData, ImageFormat format, string outputFormat, string prompt, CancellationToken cancellationToken)
        {
            using var formData = BuildForm(imageData, format, outputFormat, prompt);
            SetAuthHeaders("application/json");

            var startResponse = await httpClient.PostAsync(CreativeUrl, formData, cancellationToken);
            if (!startResponse.IsSuccessStatusCode)
            {
                var err = await startResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Stability AI creative upscale request failed: {startResponse.StatusCode} - {err}");
            }

            var startJson = await startResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(startJson);
            if (!doc.RootElement.TryGetProperty("id", out var idElement) || idElement.GetString() is not string id)
            {
                throw new InvalidOperationException("Stability AI creative upscale did not return a generation id");
            }

            // Poll for the result (creative upscales typically finish within ~10-40s).
            for (int attempt = 0; attempt < 40; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                SetAuthHeaders("image/*");
                var poll = await httpClient.GetAsync($"{ResultsUrl}{id}", cancellationToken);

                if (poll.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    continue; // still generating
                }

                await EnsureNotFilteredOrFailed(poll);
                var upscaledImageData = await poll.Content.ReadAsByteArrayAsync(cancellationToken);
                LogUpscaledDimensions(upscaledImageData);
                return upscaledImageData;
            }

            throw new InvalidOperationException("Stability AI creative upscale timed out");
        }

        private static async Task EnsureNotFilteredOrFailed(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Stability AI upscale API request failed: {response.StatusCode} - {errorContent}");
            }

            if (response.Headers.TryGetValues("finish-reason", out var finishReasons)
                && finishReasons.FirstOrDefault() == "CONTENT_FILTERED")
            {
                throw new InvalidOperationException("Image upscaling failed NSFW classifier");
            }
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
