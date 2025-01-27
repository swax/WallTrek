using OpenAI.Images;

namespace WallTrek
{
    public partial class MainForm : Form
    {
        private readonly string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallTrek");

        public MainForm()
        {
            InitializeComponent();
            Directory.CreateDirectory(outputDirectory);
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

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            try
            {
                GenerateButton.Enabled = false;
                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Marquee;
                

                ImageClient client = new("dall-e-3", apiKey);

                var prompt = PromptTextBox.Text;

                ImageGenerationOptions options = new()
                {
                    Quality = GeneratedImageQuality.High,
                    Size = GeneratedImageSize.W1792xH1024,
                    Style = GeneratedImageStyle.Vivid,
                    ResponseFormat = GeneratedImageFormat.Bytes
                };

                GeneratedImage image = await Task.Run(() => client.GenerateImage(prompt, options));
                BinaryData bytes = image.ImageBytes;

                var fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
                var filePath = Path.Combine(outputDirectory, fileName);
                using (var stream = File.OpenWrite(filePath))
                {
                    bytes.ToStream().CopyTo(stream);
                }

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
    }
}
