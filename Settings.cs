using System.Text.Json;

namespace WallTrek
{
    public class SettingsModel
    {
        public string? ApiKey { get; set; }
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
