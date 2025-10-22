using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace WallTrek.Services
{
    public class FileService
    {
        private readonly string outputDirectory;
        private const int MaxFileNamePromptLength = 75;

        public FileService(string outputDirectory)
        {
            this.outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
            Directory.CreateDirectory(outputDirectory);
        }

        public string SaveImageWithMetadata(byte[] imageData, string metadata, ImageFormat format, string title)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            // Create filename from title
            var sanitizedFilename = SanitizePromptForFilename(title);
            var extension = GetExtensionForFormat(format);
            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss} {sanitizedFilename}{extension}";
            var filePath = Path.Combine(outputDirectory, fileName);

            // Save the image with metadata
            using (var imageStream = new MemoryStream(imageData))
            using (var bitmap = new Bitmap(imageStream))
            {
                // Add prompt to image metadata
                var propertyItem = (PropertyItem?)Activator.CreateInstance(typeof(PropertyItem), true);
                if (propertyItem != null)
                {
                    propertyItem.Id = 0x010E; // ImageDescription EXIF tag
                    propertyItem.Type = 2; // ASCII string
                    var metadataBytes = Encoding.UTF8.GetBytes(metadata + "\0");
                    propertyItem.Value = metadataBytes;
                    propertyItem.Len = metadataBytes.Length;

                    bitmap.SetPropertyItem(propertyItem);
                }
                bitmap.Save(filePath, format);
            }

            return filePath;
        }

        private string SanitizePromptForFilename(string prompt)
        {
            var sanitized = string.Join("_", prompt.Split(Path.GetInvalidFileNameChars()));
            if (sanitized.Length > MaxFileNamePromptLength)
            {
                sanitized = sanitized.Substring(0, MaxFileNamePromptLength);
            }
            return sanitized;
        }

        private string GetExtensionForFormat(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Png))
                return ".png";
            if (format.Equals(ImageFormat.Jpeg))
                return ".jpg";
            if (format.Equals(ImageFormat.Bmp))
                return ".bmp";
            if (format.Equals(ImageFormat.Gif))
                return ".gif";

            return ".png"; // Default to PNG
        }
    }
}
