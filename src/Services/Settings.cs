using System.Text.Json;

namespace WallTrek.Services
{
    public class SettingsModel
    {
        public string? ApiKey { get; set; }
        public string? LastPrompt { get; set; }
        public bool AutoGenerateEnabled { get; set; }
        public int AutoGenerateMinutes { get; set; }
        public DateTime? NextAutoGenerateTime { get; set; }
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
            get => _model.AutoGenerateEnabled;
            set => _model.AutoGenerateEnabled = value;
        }

        public int AutoGenerateMinutes
        {
            get => _model.AutoGenerateMinutes;
            set => _model.AutoGenerateMinutes = value;
        }

        public DateTime? NextAutoGenerateTime
        {
            get => _model.NextAutoGenerateTime;
            set => _model.NextAutoGenerateTime = value;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                string jsonString = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
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
                    _model = JsonSerializer.Deserialize<SettingsModel>(jsonString) ?? new SettingsModel();
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