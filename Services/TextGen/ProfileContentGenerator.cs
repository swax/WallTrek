using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services.TextGen
{
    /// <summary>
    /// Generates random-prompt building blocks from a free-text description using the selected LLM:
    /// either a category set (the JSON a category profile stores) or a flat word list. The system
    /// prompts pin the model to the exact shape each editor expects, and a fixed wrapper schema is
    /// used for structured output so parsing is reliable regardless of provider.
    /// </summary>
    public class ProfileContentGenerator
    {
        private static readonly JsonSerializerOptions CategoryWriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // A fixed wrapper schema: category names are free-form, so we model the dictionary as an
        // array of { name, values[] } pairs (OpenAI strict mode can't express arbitrary keys).
        private const string CategorySchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""categories"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": { ""type"": ""string"" },
          ""values"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
        },
        ""required"": [""name"", ""values""]
      }
    }
  },
  ""required"": [""categories""]
}";

        private const string WordListSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""words"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
  },
  ""required"": [""words""]
}";

        /// <summary>
        /// Produces pretty-printed category JSON (a <c>Dictionary&lt;string, string[]&gt;</c>) ready
        /// to drop into the category editor. Throws if the model returns nothing usable.
        /// </summary>
        public async Task<string> GenerateCategoryJsonAsync(string modelId, string request, CancellationToken cancellationToken = default)
        {
            var llm = CreateLlm(modelId);

            var systemPrompt =
                "You design \"category sets\" for an AI wallpaper generator. A category set is a small " +
                "collection of named categories (such as Subject, Style, Mood, Color Palette, Lighting), " +
                "and each category lists several short, evocative option values the generator randomly " +
                "draws one from. Produce 4-7 categories with 8-15 concise values each. Values are short " +
                "phrases, not sentences. Cover varied, non-overlapping aspects so random combinations stay " +
                "interesting. Base everything on the user's theme.";

            var userPrompt =
                $"Create a category set for this theme: {request}";

            var result = await llm.GenerateStructuredResponseAsync<GeneratedCategorySet>(
                systemPrompt, userPrompt, CategorySchema, cancellationToken);

            var dict = new Dictionary<string, string[]>();
            foreach (var category in result?.Categories ?? new List<GeneratedCategory>())
            {
                if (string.IsNullOrWhiteSpace(category.Name))
                    continue;

                var values = (category.Values ?? new List<string>())
                    .Select(v => v?.Trim() ?? string.Empty)
                    .Where(v => v.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (values.Length == 0)
                    continue;

                dict[category.Name.Trim()] = values;
            }

            if (dict.Count == 0)
                throw new InvalidOperationException("The model did not return any usable categories. Try a more specific description.");

            return JsonSerializer.Serialize(dict, CategoryWriteOptions);
        }

        /// <summary>
        /// Produces a newline-delimited word list ready to drop into the word-list editor. Throws if
        /// the model returns nothing usable.
        /// </summary>
        public async Task<string> GenerateWordListAsync(string modelId, string request, CancellationToken cancellationToken = default)
        {
            var llm = CreateLlm(modelId);

            var systemPrompt =
                "You build word lists for an AI wallpaper generator. A word list is a flat collection of " +
                "single words or very short phrases that get sprinkled into prompts as random seed ideas. " +
                "Produce 30-60 distinct, concrete, evocative entries based on the user's theme. One entry " +
                "per item, no numbering, no descriptions, no duplicates.";

            var userPrompt =
                $"Create a word list for this theme: {request}";

            var result = await llm.GenerateStructuredResponseAsync<GeneratedWordList>(
                systemPrompt, userPrompt, WordListSchema, cancellationToken);

            var words = (result?.Words ?? new List<string>())
                .Select(w => w?.Trim() ?? string.Empty)
                .Where(w => w.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (words.Count == 0)
                throw new InvalidOperationException("The model did not return any words. Try a more specific description.");

            return string.Join(Environment.NewLine, words);
        }

        private static ILlmService CreateLlm(string modelId)
        {
            var settings = Settings.Instance;
            return LlmServiceFactory.CreateService(
                modelId,
                settings.ApiKey ?? string.Empty,
                settings.AnthropicApiKey ?? string.Empty,
                settings.GoogleApiKey ?? string.Empty);
        }

        private class GeneratedCategorySet
        {
            [JsonPropertyName("categories")]
            public List<GeneratedCategory> Categories { get; set; } = new();
        }

        private class GeneratedCategory
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("values")]
            public List<string> Values { get; set; } = new();
        }

        private class GeneratedWordList
        {
            [JsonPropertyName("words")]
            public List<string> Words { get; set; } = new();
        }
    }
}
