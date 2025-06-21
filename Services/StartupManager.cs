using Microsoft.Win32;

namespace WallTrek.Services
{
    public static class StartupManager
    {
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string ApplicationName = "WallTrek";

        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                var value = key?.GetValue(ApplicationName) as string;
                return !string.IsNullOrEmpty(value) && File.Exists(value.Trim('"'));
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartupEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enabled)
                {
                    var executablePath = Application.ExecutablePath;
                    key.SetValue(ApplicationName, $"\"{executablePath}\"");
                }
                else
                {
                    key.DeleteValue(ApplicationName, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update startup settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}