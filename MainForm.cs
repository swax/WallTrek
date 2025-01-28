namespace WallTrek
{
    public partial class MainForm : Form
    {
        private readonly string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallTrek");
        private ImageGenerator? imageGenerator;

        public MainForm()
        {
            InitializeComponent();
            Directory.CreateDirectory(outputDirectory);
            Settings.Instance.Load();
            Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                MessageBox.Show("Please set your OpenAI API key in Settings first.", "API Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowSettingsDialog();
                return;
            }

            try
            {
                GenerateButton.Enabled = false;
                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Marquee;
                
                imageGenerator ??= new ImageGenerator(Settings.Instance.ApiKey, outputDirectory);
                var filePath = await imageGenerator.GenerateAndSaveImage(PromptTextBox.Text);
                Wallpaper.Set(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar1.Visible = false;
                GenerateButton.Enabled = true;
            }
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", outputDirectory);
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && GenerateButton.Enabled)
            {
                e.Handled = true;
                GenerateButton_Click(sender, e);
            }
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            ShowSettingsDialog();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            ShowSettingsDialog();
        }

        private void ShowSettingsDialog()
        {
            using var settingsForm = new SettingsForm();
            settingsForm.ShowDialog(this);
        }
    }
}
