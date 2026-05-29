using System.Collections.Generic;
using System.Linq;

namespace WallTrek.Services.ImageGen
{
    /// <summary>
    /// The catalog of image-generation options shown in the model dropdown.
    /// Prices are approximate per-image (in cents) and display-only — edit freely.
    /// </summary>
    public static class ImageModelCatalog
    {
        public static IReadOnlyList<ImageModelOption> Options { get; } = new List<ImageModelOption>
        {
            new() { Id = "gemini-3-pro-image-preview@4K", Provider = ImageProvider.Google, ModelId = "gemini-3-pro-image-preview",
                    Model = "Gemini 3 Pro", Quality = "4K",       Dimensions = "3840×2160", Price = "~24¢", Cents = 24m,
                    ImageSize = "4K", AspectRatio = "16:9" },
            new() { Id = "gemini-3-pro-image-preview@2K", Provider = ImageProvider.Google, ModelId = "gemini-3-pro-image-preview",
                    Model = "Gemini 3 Pro", Quality = "2K",       Dimensions = "2048×1152", Price = "~13¢", Cents = 13m,
                    ImageSize = "2K", AspectRatio = "16:9" },
            new() { Id = "imagen-4.0-ultra-generate-001@2K", Provider = ImageProvider.Google, ModelId = "imagen-4.0-ultra-generate-001",
                    Model = "Imagen 4 Ultra", Quality = "Ultra",  Dimensions = "2K · 16:9", Price = "~6¢", Cents = 6m,
                    ImageSize = "2K", AspectRatio = "16:9" },
            new() { Id = "imagen-4.0-generate-001@2K", Provider = ImageProvider.Google, ModelId = "imagen-4.0-generate-001",
                    Model = "Imagen 4", Quality = "Standard",     Dimensions = "2K · 16:9", Price = "~4¢", Cents = 4m,
                    ImageSize = "2K", AspectRatio = "16:9" },
            new() { Id = "gpt-image-2@high", Provider = ImageProvider.OpenAI, ModelId = "gpt-image-2",
                    Model = "GPT Image", Quality = "High",        Dimensions = "1536×1024", Price = "~30¢", Cents = 30m,
                    OpenAiQuality = "high" },
            new() { Id = "gpt-image-2@medium", Provider = ImageProvider.OpenAI, ModelId = "gpt-image-2",
                    Model = "GPT Image", Quality = "Medium",      Dimensions = "1536×1024", Price = "~8¢", Cents = 8m,
                    OpenAiQuality = "medium" },
            new() { Id = "gpt-image-2@low", Provider = ImageProvider.OpenAI, ModelId = "gpt-image-2",
                    Model = "GPT Image", Quality = "Low",         Dimensions = "1536×1024", Price = "~1¢", Cents = 1m,
                    OpenAiQuality = "low" },
        };

        /// <summary>Default selection for fresh installs — good quality, low cost, native 16:9.</summary>
        public static ImageModelOption Default =>
            Options.FirstOrDefault(o => o.Id == "imagen-4.0-generate-001@2K") ?? Options[0];

        public static ImageModelOption? FindById(string? id) =>
            Options.FirstOrDefault(o => o.Id == id);

        /// <summary>Back-compat: match a legacy settings value that stored only a bare model id.</summary>
        public static ImageModelOption? FindByModelId(string? modelId) =>
            Options.FirstOrDefault(o => o.ModelId == modelId);
    }
}
