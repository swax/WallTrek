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

        private void GenerateButton_Click(object sender, EventArgs e)
        {

            ImageClient client = new("dall-e-3", apiKey); // Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            var prompt = PromptTextBox.Text;

            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                Style = GeneratedImageStyle.Vivid,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            GeneratedImage image = client.GenerateImage(prompt, options);
            BinaryData bytes = image.ImageBytes;

            var filePath = $"{Guid.NewGuid()}.png";
            using (var stream = File.OpenWrite(filePath))
            {
                bytes.ToStream().CopyTo(stream);
            }

            // Set the desktop background to this image
            Wallpaper.Set(filePath);
        }
    }
}
