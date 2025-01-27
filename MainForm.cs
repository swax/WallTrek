using OpenAI.Images;

namespace WallTrek
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
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

                var filePath = $"{Guid.NewGuid()}.png";
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
    }
}
