using System.Drawing.Imaging;

namespace WallTrek.Services.ImageGen
{
    public class ImageGenerationResult
    {
        public MemoryStream ImageData { get; set; } = null!;
        public ImageFormat Format { get; set; } = null!;
    }

    public interface IImageGenerationService
    {
        Task<ImageGenerationResult> GenerateImage(string prompt, CancellationToken cancellationToken = default);
    }
}