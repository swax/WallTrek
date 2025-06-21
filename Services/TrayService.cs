using System;
using System.Drawing;
using System.Windows.Forms;

namespace WallTrek.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip contextMenu;
        private readonly ToolStripMenuItem settingsMenuItem;
        private readonly ToolStripMenuItem quitMenuItem;

        public event EventHandler? ShowFormRequested;
        public event EventHandler? ShowSettingsRequested;
        public event EventHandler? QuitRequested;

        public TrayService()
        {
            // Create context menu
            settingsMenuItem = new ToolStripMenuItem("Settings");
            quitMenuItem = new ToolStripMenuItem("Quit");
            
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.AddRange(new ToolStripItem[] { settingsMenuItem, quitMenuItem });

            // Create notify icon
            notifyIcon = new NotifyIcon
            {
                Text = "WallTrek",
                Visible = true,
                ContextMenuStrip = contextMenu
            };

            // Set icon
            SetIcon();

            // Wire up events
            notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
            settingsMenuItem.Click += OnSettingsMenuItemClick;
            quitMenuItem.Click += OnQuitMenuItemClick;
        }

        private void SetIcon()
        {
            try
            {
                var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    notifyIcon.Icon = appIcon;
                }
            }
            catch
            {
                // Fallback to default icon if extraction fails
            }
        }

        private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
        {
            ShowFormRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnSettingsMenuItemClick(object? sender, EventArgs e)
        {
            ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnQuitMenuItemClick(object? sender, EventArgs e)
        {
            QuitRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            notifyIcon?.Dispose();
            contextMenu?.Dispose();
            settingsMenuItem?.Dispose();
            quitMenuItem?.Dispose();
        }
    }
}