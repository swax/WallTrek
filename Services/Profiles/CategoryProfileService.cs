using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using WallTrek.Services;

namespace WallTrek.Services.Profiles
{
    /// <summary>
    /// Manages "category profiles" — named sets of random-prompt categories, each stored
    /// as a JSON file under %APPDATA%\WallTrek\categories. A file is a
    /// Dictionary&lt;string, string[]&gt; (category name → possible values). The active
    /// profile is chosen via <see cref="Settings.SelectedCategoryProfile"/>; creating a
    /// new profile is just dropping a new .json file in the folder.
    /// </summary>
    public static class CategoryProfileService
    {
        public const string Extension = ".json";

        public static string FolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WallTrek",
            "categories");

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Ensures the folder exists and holds at least one profile, seeding "Default"
        /// from the legacy in-settings categories (preserving an upgrading user's
        /// customizations) or the built-in defaults on a fresh install.
        /// </summary>
        public static void EnsureSeeded()
        {
            Directory.CreateDirectory(FolderPath);

            if (ListProfiles().Count > 0)
                return;

            var legacy = Settings.Instance.RandomPrompts;
            Dictionary<string, string[]> seed =
                legacy != null && legacy.Count > 0
                    ? new Dictionary<string, string[]>(legacy)
                    : new DefaultRandomPrompts();

            WriteDictionary("Default", seed);
        }

        /// <summary>Profile names (file stems), sorted case-insensitively.</summary>
        public static IReadOnlyList<string> ListProfiles()
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

        /// <summary>Raw JSON text of a profile (empty string if missing/unreadable).</summary>
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

        /// <summary>Parsed categories for a profile (empty dict if missing/invalid).</summary>
        public static Dictionary<string, string[]> LoadDictionary(string name)
        {
            var text = LoadText(name);
            if (string.IsNullOrWhiteSpace(text))
                return new Dictionary<string, string[]>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string[]>>(text)
                       ?? new Dictionary<string, string[]>();
            }
            catch
            {
                return new Dictionary<string, string[]>();
            }
        }

        /// <summary>
        /// Categories for the active profile (<see cref="Settings.SelectedCategoryProfile"/>),
        /// falling back to the first available profile, then the built-in defaults.
        /// </summary>
        public static Dictionary<string, string[]> LoadActive()
        {
            var name = Settings.Instance.SelectedCategoryProfile;
            if (!string.IsNullOrWhiteSpace(name) && Exists(name))
                return LoadDictionary(name);

            var first = ListProfiles().FirstOrDefault();
            if (first != null)
                return LoadDictionary(first);

            return new DefaultRandomPrompts();
        }

        public static void WriteText(string name, string jsonText)
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(PathFor(name), jsonText);
        }

        public static void WriteDictionary(string name, Dictionary<string, string[]> data) =>
            WriteText(name, JsonSerializer.Serialize(data, WriteOptions));

        /// <summary>Validates raw text as a non-empty category dictionary.</summary>
        public static bool TryValidate(string jsonText, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                error = "Categories JSON cannot be empty.";
                return false;
            }
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string[]>>(jsonText);
                if (dict == null || dict.Count == 0)
                {
                    error = "At least one category is required.";
                    return false;
                }
                foreach (var kvp in dict)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                    {
                        error = "Category names cannot be empty.";
                        return false;
                    }
                    if (kvp.Value == null || kvp.Value.All(string.IsNullOrWhiteSpace))
                    {
                        error = $"Category '{kvp.Key}' must have at least one non-empty value.";
                        return false;
                    }
                }
                return true;
            }
            catch (JsonException ex)
            {
                error = $"Invalid JSON: {ex.Message}";
                return false;
            }
        }

        /// <summary>Creates a new profile seeded with the built-in default categories.</summary>
        public static void Create(string name)
        {
            if (Exists(name))
                throw new IOException($"A profile named '{name}' already exists.");
            WriteDictionary(name, new DefaultRandomPrompts());
        }

        public static void Rename(string oldName, string newName)
        {
            var src = PathFor(oldName);
            var dst = PathFor(newName);
            if (!File.Exists(src))
                throw new FileNotFoundException($"Profile '{oldName}' not found.");
            if (File.Exists(dst))
                throw new IOException($"A profile named '{newName}' already exists.");
            File.Move(src, dst);
        }

        public static void Delete(string name)
        {
            var path = PathFor(name);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>Replaces path separators / invalid filename characters in a profile name.</summary>
        public static string SanitizeName(string name)
        {
            name = (name ?? string.Empty).Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
