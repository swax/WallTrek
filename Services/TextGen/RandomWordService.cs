using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WallTrek.Services;

namespace WallTrek.Services.TextGen
{
    /// <summary>
    /// Supplies random "seed" words used to inject entropy into AI prompt generation.
    /// Word lists are flat text files (one word per line; lines starting with '#' are
    /// comments) stored under %APPDATA%\WallTrek\wordlists. The active list is chosen via
    /// <see cref="Settings.SelectedWordList"/>; creating a new list is just dropping a new
    /// .txt file in the folder. A list embedded in the assembly
    /// (Services/TextGen/words.txt) seeds the default file on first run.
    /// </summary>
    public class RandomWordService
    {
        public const string Extension = ".txt";

        public static string FolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WallTrek",
            "wordlists");

        // Single-override path used before word lists became multi-file; migrated on first run.
        private static string LegacyOverridePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WallTrek",
            "words.txt");

        /// <summary>
        /// Ensures the folder exists and holds at least one list, seeding "Default" from a
        /// pre-existing legacy words.txt override (preserving an upgrading user's custom
        /// list) or the embedded list on a fresh install.
        /// </summary>
        public static void EnsureSeeded()
        {
            Directory.CreateDirectory(FolderPath);

            if (ListWordLists().Count > 0)
                return;

            string seedText;
            try
            {
                seedText = File.Exists(LegacyOverridePath)
                    ? File.ReadAllText(LegacyOverridePath)
                    : GetDefaultWordListText();
            }
            catch
            {
                seedText = GetDefaultWordListText();
            }

            WriteText("Default", seedText);
        }

        /// <summary>Word-list names (file stems), sorted case-insensitively.</summary>
        public static IReadOnlyList<string> ListWordLists()
        {
            if (!Directory.Exists(FolderPath))
                return Array.Empty<string>();

            return Directory.EnumerateFiles(FolderPath, "*" + Extension)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool Exists(string name) => File.Exists(PathFor(name));

        public static string PathFor(string name) =>
            Path.Combine(FolderPath, SanitizeName(name) + Extension);

        public static string LoadText(string name)
        {
            try
            {
                var path = PathFor(name);
                return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void WriteText(string name, string text)
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(PathFor(name), text ?? string.Empty);
        }

        public static void Create(string name)
        {
            if (Exists(name))
                throw new IOException($"A word list named '{name}' already exists.");
            WriteText(name, "# One word per line. Lines starting with # are ignored.\n");
        }

        public static void Rename(string oldName, string newName)
        {
            var src = PathFor(oldName);
            var dst = PathFor(newName);
            if (!File.Exists(src))
                throw new FileNotFoundException($"Word list '{oldName}' not found.");
            if (File.Exists(dst))
                throw new IOException($"A word list named '{newName}' already exists.");
            File.Move(src, dst);
        }

        public static void Delete(string name)
        {
            var path = PathFor(name);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>Replaces path separators / invalid filename characters in a list name.</summary>
        public static string SanitizeName(string name)
        {
            name = (name ?? string.Empty).Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> distinct words chosen at random from the
        /// active word list.
        /// </summary>
        public Task<List<string>> GetRandomWordsAsync(int count, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var words = GetActiveWords();
            if (count <= 0 || words.Count == 0)
                return Task.FromResult(new List<string>());

            var take = Math.Min(count, words.Count);

            // Partial Fisher-Yates over an index array: shuffles only the first `take`
            // slots, giving distinct words (no repeats) without copying or sorting the list.
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

        /// <summary>
        /// Parsed words from the active list (<see cref="Settings.SelectedWordList"/>),
        /// falling back to the first available list, then the embedded default.
        /// </summary>
        public static List<string> GetActiveWords()
        {
            var name = Settings.Instance.SelectedWordList;
            string text;
            if (!string.IsNullOrWhiteSpace(name) && Exists(name))
            {
                text = LoadText(name);
            }
            else
            {
                var first = ListWordLists().FirstOrDefault();
                text = first != null ? LoadText(first) : GetDefaultWordListText();
            }
            return ParseWords(text);
        }

        /// <summary>Raw text of the embedded default word list (comments included).</summary>
        public static string GetDefaultWordListText()
        {
            var assembly = typeof(RandomWordService).Assembly;

            // Match by suffix so the resource keeps loading even if the root namespace or
            // folder is renamed.
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
        /// Parses raw list text into distinct words, skipping blank lines and lines that
        /// start with '#'. De-duplicates case-insensitively, preserving order.
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
    }
}
