using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services.TextGen
{
    public interface ILlmService
    {
        Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
        Task<T?> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken cancellationToken = default) where T : class;
    }
}