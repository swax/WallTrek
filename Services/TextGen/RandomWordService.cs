using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WallTrek.Services.TextGen
{
    /// <summary>
    /// Supplies random "seed" words used to inject entropy into AI prompt
    /// generation. By default words come from a list embedded in the assembly
    /// (Services/TextGen/words.txt). Users can override the list from the Settings
    /// UI; when they do, the custom list is persisted to %APPDATA%\WallTrek\words.txt
    /// and takes precedence over the embedded one.
    /// </summary>
    public class RandomWordService
    {
        private static readonly object _lock = new();
        private static IReadOnlyList<string>? _cache;

        /// <summary>Path to the user's optional custom word list (app data folder).</summary>
        public static string OverrideFilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WallTrek",
            "words.txt");

        /// <summary>
        /// Returns up to <paramref name="count"/> distinct words chosen at random
        /// from the effective word list (custom override if present, else embedded).
        /// </summary>
        public Task<List<string>> GetRandomWordsAsync(int count, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var words = Words;
            if (count <= 0 || words.Count == 0)
                return Task.FromResult(new List<string>());

            var take = Math.Min(count, words.Count);

            // Partial Fisher-Yates over an index array: shuffles only the first
            // `take` slots, giving distinct words (no repeats) without copying or
            // sorting the whole list.
            var indices = new int[words.Count];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;

            var result = new List<string>(take);
            for (int i = 0; i < take; i++)
            {
                int j = Random.Shared.Next(i, indices.Length);
                (indices[i], indices[j]) = (indices[j], indices[i]);
                result.Add(words[indices[i]]);
            }

            return Task.FromResult(result);
        }

        /// <summary>The cached, parsed effective word list.</summary>
        private static IReadOnlyList<string> Words
        {
            get
            {
                lock (_lock)
                {
                    return _cache ??= ParseWords(GetEffectiveWordListText());
                }
            }
        }

        /// <summary>Forces the next access to reload the word list from disk/resource.</summary>
        public static void InvalidateCache()
        {
            lock (_lock)
            {
                _cache = null;
            }
        }

        /// <summary>True when a user-supplied custom word list exists on disk.</summary>
        public static bool HasOverride() => File.Exists(OverrideFilePath);

        /// <summary>
        /// Raw text of the list currently in effect: the custom override file if it
        /// exists, otherwise the embedded default (comments and all).
        /// </summary>
        public static string GetEffectiveWordListText()
        {
            try
            {
                if (HasOverride())
                    return File.ReadAllText(OverrideFilePath);
            }
            catch
            {
                // Fall back to the embedded default if the override can't be read.
            }

            return GetDefaultWordListText();
        }

        /// <summary>Raw text of the embedded default word list (comments included).</summary>
        public static string GetDefaultWordListText()
        {
            var assembly = typeof(RandomWordService).Assembly;

            // Match by suffix so the resource keeps loading even if the root
            // namespace or folder is renamed.
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("words.txt", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
                return string.Empty;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                return string.Empty;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Parses raw list text into distinct words, skipping blank lines and lines
        /// that start with '#'. De-duplicates case-insensitively, preserving order.
        /// </summary>
        public static List<string> ParseWords(string text)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(text))
                return words;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var word = line.Trim();
                if (word.Length == 0 || word[0] == '#')
                    continue;
                if (seen.Add(word))
                    words.Add(word);
            }

            return words;
        }

        /// <summary>Writes a custom word list to disk and refreshes the cache.</summary>
        public static void SaveOverride(string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OverrideFilePath)!);
            File.WriteAllText(OverrideFilePath, text);
            InvalidateCache();
        }

        /// <summary>Removes the custom word list so the embedded default is used again.</summary>
        public static void ClearOverride()
        {
            try
            {
                if (File.Exists(OverrideFilePath))
                    File.Delete(OverrideFilePath);
            }
            finally
            {
                InvalidateCache();
            }
        }
    }
}
