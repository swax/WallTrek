namespace WallTrek.Services.ImageGen
{
    public enum ImageProvider
    {
        OpenAI,
        Google
    }

    /// <summary>
    /// A selectable image-generation option shown in the model dropdown. Each option pairs an API
    /// model with a specific quality/resolution, carries the parameters needed to drive it, and
    /// provides the display columns (model / quality / dimensions / price) for the grid dropdown.
    /// </summary>
    public class ImageModelOption
    {
        /// <summary>Stable key persisted in settings, e.g. "gemini-3-pro-image-preview@4K".</summary>
        public string Id { get; init; } = "";

        /// <summary>Which generation service handles this option.</summary>
        public ImageProvider Provider { get; init; }

        /// <summary>The API model id passed to the provider.</summary>
        public string ModelId { get; init; } = "";

        // --- Display columns ---
        public string Model { get; init; } = "";
        public string Quality { get; init; } = "";
        public string Dimensions { get; init; } = "";
        public string Price { get; init; } = "";

        /// <summary>Approximate per-image cost in US cents (drives the total-cost estimate).</summary>
        public decimal Cents { get; init; }

        // --- Google generation parameters (Imagen predict / Gemini generateContent) ---
        public string ImageSize { get; init; } = "2K";        // "1K" | "2K" | "4K"
        public string AspectRatio { get; init; } = "16:9";

        // --- OpenAI gpt-image generation parameters ---
        public string OpenAiQuality { get; init; } = "high";  // "low" | "medium" | "high" | "auto"
    }
}
