using System.Text.Json;
using System.Text.Json.Serialization;

namespace WallTrek.Services
{
    [JsonSerializable(typeof(SettingsModel))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }

    public class AutoGenerateSettings
    {
        public bool Enabled { get; set; }
        public double Hours { get; set; } = 6.0;
        public DateTime? NextGenerateTime { get; set; }
        public string Source { get; set; } = "current";
    }

    public class RandomPromptsSettings
    {
        public List<string> Categories { get; set; } = new()
        {
            "abstract geometric patterns and mathematical art",
            "natural landscapes and organic forms", 
            "futuristic cyberpunk and sci-fi environments",
            "minimalist design and negative space",
            "fantasy worlds and magical creatures",
            "architectural marvels and urban scenes",
            "cosmic and astronomical phenomena",
            "vintage and retro aesthetic designs",
            "surreal and dreamlike compositions",
            "seasonal and weather-based imagery"
        };

        public List<string> Styles { get; set; } = new()
        {
            "oil painting", "watercolor", "digital art", "photography", "3D render",
            "vector illustration", "pixel art", "paper cut-out", "neon lighting",
            "pencil sketch", "abstract expressionism", "art nouveau", "bauhaus design"
        };

        public List<string> Moods { get; set; } = new()
        {
            "serene and calming", "energetic and vibrant", "mysterious and moody",
            "bright and cheerful", "dramatic and intense", "peaceful and meditative",
            "bold and striking", "subtle and elegant", "warm and cozy", "cool and refreshing"
        };
    }

    public class SettingsModel
    {
        public string? ApiKey { get; set; }
        public string? LastPrompt { get; set; }
        public AutoGenerateSettings AutoGenerate { get; set; } = new();
        public RandomPromptsSettings RandomPrompts { get; set; } = new();
        public bool MinimizeToTray { get; set; } = true;
        public bool IsFirstRun { get; set; } = true;
        public string? OutputDirectory { get; set; }
    }

    public class Settings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WallTrek",
            "settings.json");

        public static Settings Instance { get; private set; } = new Settings();

        private SettingsModel _model = new();

        public string? ApiKey
        {
            get => _model.ApiKey;
            set => _model.ApiKey = value;
        }

        public string? LastPrompt
        {
            get => _model.LastPrompt;
            set => _model.LastPrompt = value;
        }

        public bool AutoGenerateEnabled
        {
            get => _model.AutoGenerate.Enabled;
            set => _model.AutoGenerate.Enabled = value;
        }

        public double AutoGenerateHours
        {
            get => _model.AutoGenerate.Hours;
            set => _model.AutoGenerate.Hours = value;
        }

        public DateTime? NextAutoGenerateTime
        {
            get => _model.AutoGenerate.NextGenerateTime;
            set => _model.AutoGenerate.NextGenerateTime = value;
        }

        public bool MinimizeToTray
        {
            get => _model.MinimizeToTray;
            set => _model.MinimizeToTray = value;
        }

        public bool IsFirstRun
        {
            get => _model.IsFirstRun;
            set => _model.IsFirstRun = value;
        }

        public string OutputDirectory
        {
            get => _model.OutputDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallTrek");
            set => _model.OutputDirectory = value;
        }

        public string AutoGenerateSource
        {
            get => _model.AutoGenerate.Source;
            set => _model.AutoGenerate.Source = value;
        }

        public RandomPromptsSettings RandomPrompts
        {
            get => _model.RandomPrompts;
            set => _model.RandomPrompts = value;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = SettingsJsonContext.Default
                };
                string jsonString = JsonSerializer.Serialize(_model, options);
                File.WriteAllText(SettingsPath, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string jsonString = File.ReadAllText(SettingsPath);
                    _model = JsonSerializer.Deserialize(jsonString, SettingsJsonContext.Default.SettingsModel) ?? new SettingsModel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                _model = new SettingsModel();
            }
        }
    }
}