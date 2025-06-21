using WallTrek.Services;

namespace WallTrek
{
    public partial class MainForm : Form
    {
        private readonly string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallTrek");

        public MainForm()
        {
            InitializeComponent();
            Directory.CreateDirectory(outputDirectory);
            Settings.Instance.Load();

            // Connect to singleton auto-generate service
            AutoGenerateService.Instance.AutoGenerateTriggered += OnAutoGenerateTriggered;
            AutoGenerateService.Instance.NextGenerateTimeUpdated += OnNextGenerateTimeUpdated;

            // Set form icon to main app icon
            var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Icon = appIcon;

            // Load saved settings
            PromptTextBox.Text = Settings.Instance.LastPrompt;

            // Restore auto-generate timer if it was running
            if (Settings.Instance.AutoGenerateEnabled && Settings.Instance.NextAutoGenerateTime.HasValue)
            {
                AutoGenerateService.Instance.StartFromSavedTime();
            }

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

        public void ShowAndActivate()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
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
            // Save prompt when generating
            Settings.Instance.LastPrompt = PromptTextBox.Text;
            Settings.Instance.Save();

            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                MessageBox.Show("Please set your OpenAI API key in Settings first.", "API Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowSettingsDialog();
                return;
            }

            // Stop any existing auto-generate timer before starting new generation
            AutoGenerateService.Instance.Stop();

            try
            {
                GenerateButton.Enabled = false;
                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Marquee;

                var imageGenerator = new ImageGenerator(Settings.Instance.ApiKey, outputDirectory);
                var filePath = await imageGenerator.GenerateAndSaveImage(PromptTextBox.Text);
                Wallpaper.Set(filePath);

                // Setup auto-generate timer if enabled and minutes > 0
                if (Settings.Instance.AutoGenerateEnabled && Settings.Instance.AutoGenerateMinutes > 0)
                {
                    AutoGenerateService.Instance.Start(Settings.Instance.AutoGenerateMinutes);
                }
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

        public void ShowSettingsDialog()
        {
            using var settingsForm = new SettingsForm();
            settingsForm.ShowDialog(this);
        }

        private async void OnAutoGenerateTriggered(object? sender, EventArgs e)
        {
            await Task.Run(() => BeginInvoke(GenerateButton_Click, sender, e));
        }

        private void OnNextGenerateTimeUpdated(object? sender, string timeText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => nextGenerateLabel.Text = timeText);
            }
            else
            {
                nextGenerateLabel.Text = timeText;
            }
        }
    }
}
