using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services.TextGen
{
    public class RandomWordService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<List<string>> GetRandomWordsAsync(int count, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"https://random-word-api.vercel.app/api?words={count}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var words = JsonSerializer.Deserialize<string[]>(jsonContent);

                return words != null ? new List<string>(words) : new List<string>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching random words: {ex.Message}");
            }
        }
    }
}
