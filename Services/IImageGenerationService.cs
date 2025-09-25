namespace WallTrek.Services
{
    public interface IImageGenerationService
    {
        Task<string> GenerateAndSaveImage(string prompt, string llmModel, string imgModel, CancellationToken cancellationToken = default);
    }
}