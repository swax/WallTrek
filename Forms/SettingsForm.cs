using WallTrek.Services;

namespace WallTrek
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            ApiKeyTextBox.Text = Settings.Instance.ApiKey;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Settings.Instance.ApiKey = ApiKeyTextBox.Text;
            Settings.Instance.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
