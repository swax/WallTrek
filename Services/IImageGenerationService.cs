namespace WallTrek.Services
{
    public interface IImageGenerationService
    {
        Task<string> GenerateAndSaveImage(string prompt, CancellationToken cancellationToken = default);
    }
}