using WallTrek.Services;

namespace WallTrek
{
    public partial class SettingsForm : Form
    {
        private bool isLoadingSettings = true;

        public SettingsForm()
        {
            InitializeComponent();
            ApiKeyTextBox.Text = Settings.Instance.ApiKey;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            
            // Load auto-generate settings
            autoGenerateCheckbox.Checked = Settings.Instance.AutoGenerateEnabled;
            autoGenerateMinutes.Value = Settings.Instance.AutoGenerateMinutes > 0 ?
                Settings.Instance.AutoGenerateMinutes : autoGenerateMinutes.Minimum;
            
            // Load startup setting from registry
            startupCheckbox.Checked = StartupManager.IsStartupEnabled();
            
            isLoadingSettings = false;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Settings.Instance.ApiKey = ApiKeyTextBox.Text;
            Settings.Instance.AutoGenerateEnabled = autoGenerateCheckbox.Checked;
            Settings.Instance.AutoGenerateMinutes = (int)autoGenerateMinutes.Value;
            Settings.Instance.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AutoGenerateCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            autoGenerateMinutes.Enabled = autoGenerateCheckbox.Checked;

            if (!isLoadingSettings)
            {
                Settings.Instance.AutoGenerateEnabled = autoGenerateCheckbox.Checked;
                Settings.Instance.Save();
            }

            if (!autoGenerateCheckbox.Checked)
            {
                AutoGenerateService.Instance.Cancel();
            }
            else if (!isLoadingSettings && Settings.Instance.AutoGenerateMinutes > 0)
            {
                AutoGenerateService.Instance.Start(Settings.Instance.AutoGenerateMinutes);
            }
        }

        private void AutoGenerateMinutes_ValueChanged(object sender, EventArgs e)
        {
            if (!isLoadingSettings)
            {
                Settings.Instance.AutoGenerateMinutes = (int)autoGenerateMinutes.Value;
                Settings.Instance.Save();
            }
        }

        private void AutoGenerateMinutes_TextChanged(object sender, EventArgs e)
        {
            if (decimal.TryParse(autoGenerateMinutes.Text, out decimal value))
            {
                if (value >= autoGenerateMinutes.Minimum && value <= autoGenerateMinutes.Maximum)
                {
                    if (!isLoadingSettings)
                    {
                        Settings.Instance.AutoGenerateMinutes = (int)value;
                        Settings.Instance.Save();
                    }
                }
            }
        }

        private void StartupCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!isLoadingSettings)
            {
                StartupManager.SetStartupEnabled(startupCheckbox.Checked);
            }
        }
    }
}
