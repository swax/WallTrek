namespace WallTrek.Services.ImageGen
{
    public enum UpscaleMode
    {
        None,
        Fast,
        Conservative,
        Creative
    }

    /// <summary>
    /// A selectable upscaling option shown in the upscale dropdown, with display columns
    /// (name / detail / price). The paid tiers use the Stability AI upscale API and require a
    /// Stability key; <see cref="UpscaleMode.None"/> skips upscaling entirely.
    /// </summary>
    public class UpscalerOption
    {
        public UpscaleMode Mode { get; init; }

        /// <summary>Stable key persisted in settings: none / fast / conservative / creative.</summary>
        public string Id { get; init; } = "";

        // --- Display columns ---
        public string Name { get; init; } = "";
        public string Detail { get; init; } = "";
        public string Price { get; init; } = "";

        /// <summary>Approximate per-image cost in US cents (drives the total-cost estimate).</summary>
        public decimal Cents { get; init; }
    }
}
