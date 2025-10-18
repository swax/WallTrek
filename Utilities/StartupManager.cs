using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace WallTrek.Utilities
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
                    var executablePath = GetExecutablePath();
                    key.SetValue(ApplicationName, $"\"{executablePath}\"");
                }
                else
                {
                    key.DeleteValue(ApplicationName, false);
                }
            }
            catch (Exception ex)
            {
                // In WinUI, we can't use MessageBox, so we'll just ignore the error
                // The Settings UI will show if the setting was actually applied
                System.Diagnostics.Debug.WriteLine($"Failed to update startup settings: {ex.Message}");
            }
        }

        private static string GetExecutablePath()
        {
            // For WinUI applications, get the executable path differently
            var assembly = Assembly.GetExecutingAssembly();
            var location = assembly.Location;
            
            // If running from the build output, get the actual executable
            if (location.EndsWith(".dll"))
            {
                var directory = Path.GetDirectoryName(location);
                var fileName = Path.GetFileNameWithoutExtension(location) + ".exe";
                return Path.Combine(directory!, fileName);
            }
            
            return location;
        }
    }
}