using WallTrek.Services;

namespace WallTrek
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var mainForm = new MainForm();
            var trayService = new TrayService();

            // Wire up tray service events
            trayService.ShowFormRequested += (sender, e) => mainForm.ShowAndActivate();
            trayService.ShowSettingsRequested += (sender, e) => 
            {
                mainForm.ShowAndActivate();
                mainForm.ShowSettingsDialog();
            };
            trayService.QuitRequested += (sender, e) => 
            {
                trayService.Dispose();
                Application.Exit();
            };

            Application.Run();
        }
    }
}