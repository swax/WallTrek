namespace WallTrek
{
    public partial class MainForm : Form
    {
        private readonly string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallTrek");
        private ImageGenerator? imageGenerator;
        private System.Windows.Forms.Timer? autoGenerateTimer;
        private DateTime? nextGenerateTime;
        private bool isLoadingSettings = true;

        public MainForm()
        {
            InitializeComponent();
            Directory.CreateDirectory(outputDirectory);
            Settings.Instance.Load();

            // Load saved settings
            PromptTextBox.Text = Settings.Instance.LastPrompt;
            autoGenerateCheckbox.Checked = Settings.Instance.AutoGenerateEnabled;
            autoGenerateMinutes.Value = Settings.Instance.AutoGenerateMinutes > 0 ?
                Settings.Instance.AutoGenerateMinutes : autoGenerateMinutes.Minimum;

            isLoadingSettings = false;
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

        private void StopAutoGenerate()
        {
            autoGenerateTimer?.Stop();
            autoGenerateTimer?.Dispose();
            autoGenerateTimer = null;
            nextGenerateTime = null;
            nextGenerateLabel.Text = "";
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
            StopAutoGenerate();

            try
            {
                GenerateButton.Enabled = false;
                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Marquee;

                imageGenerator ??= new ImageGenerator(Settings.Instance.ApiKey, outputDirectory);
                var filePath = await imageGenerator.GenerateAndSaveImage(PromptTextBox.Text);
                Wallpaper.Set(filePath);

                // Setup auto-generate timer if enabled and minutes > 0
                if (autoGenerateCheckbox.Checked && autoGenerateMinutes.Value > 0)
                {
                    SetupAutoGenerateTimer();
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

        private void SetupAutoGenerateTimer()
        {
            StopAutoGenerate();

            if (autoGenerateMinutes.Value <= 0)
            {
                return;
            }

            int minutes = (int)autoGenerateMinutes.Value;
            autoGenerateTimer = new System.Windows.Forms.Timer();
            autoGenerateTimer.Interval = minutes * 60 * 1000; // Convert minutes to milliseconds
            autoGenerateTimer.Tick += async (s, e) => await Task.Run(() => BeginInvoke(GenerateButton_Click, s, e));
            autoGenerateTimer.Start();

            nextGenerateTime = DateTime.Now.AddMinutes(minutes);
            UpdateNextGenerateLabel();

            // Start a timer to update the "next generate" label
            var updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000; // Update every second
            updateTimer.Tick += (s, e) => UpdateNextGenerateLabel();
            updateTimer.Start();
        }

        private void UpdateNextGenerateLabel()
        {
            if (!nextGenerateTime.HasValue)
            {
                nextGenerateLabel.Text = "";
                return;
            }

            var timeLeft = nextGenerateTime.Value - DateTime.Now;
            if (timeLeft.TotalSeconds <= 0)
            {
                nextGenerateLabel.Text = "";
                return;
            }

            nextGenerateLabel.Text = $"Next generation in: {timeLeft.Hours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
        }

        private void AutoGenerateCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            autoGenerateMinutes.Enabled = autoGenerateCheckbox.Checked;

            // Save auto-generate settings only if not loading
            if (!isLoadingSettings)
            {
                Settings.Instance.AutoGenerateEnabled = autoGenerateCheckbox.Checked;
                Settings.Instance.Save();
            }

            if (!autoGenerateCheckbox.Checked)
            {
                StopAutoGenerate();
            }
            else if (GenerateButton.Enabled)
            {
                SetupAutoGenerateTimer();
            }
        }

        private void AutoGenerateMinutes_ValueChanged(object sender, EventArgs e)
        {
            // Save minutes value only if not loading
            if (!isLoadingSettings)
            {
                Settings.Instance.AutoGenerateMinutes = (int)autoGenerateMinutes.Value;
                Settings.Instance.Save();
            }

            if (autoGenerateCheckbox.Checked && GenerateButton.Enabled)
            {
                SetupAutoGenerateTimer();
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

                    if (autoGenerateCheckbox.Checked && GenerateButton.Enabled)
                    {
                        SetupAutoGenerateTimer();
                    }
                }
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
