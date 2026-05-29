using System.Collections.Generic;
using System.Linq;

namespace WallTrek.Services.ImageGen
{
    /// <summary>
    /// Upscaling options shown in the dropdown. The paid tiers use the Stability AI upscale API and
    /// require a Stability key (set in Settings). Prices are approximate per-image and display-only.
    /// </summary>
    public static class UpscalerCatalog
    {
        public static IReadOnlyList<UpscalerOption> Options { get; } = new List<UpscalerOption>
        {
            new() { Mode = UpscaleMode.None,         Id = "none",         Name = "None",         Detail = "No upscaling",   Price = "—",    Cents = 0m },
            new() { Mode = UpscaleMode.Fast,         Id = "fast",         Name = "Fast",         Detail = "4× → ~4K",       Price = "~2¢",  Cents = 2m },
            new() { Mode = UpscaleMode.Conservative, Id = "conservative", Name = "Conservative", Detail = "→4K · faithful", Price = "~40¢", Cents = 40m },
            new() { Mode = UpscaleMode.Creative,     Id = "creative",     Name = "Creative",     Detail = "→4K · enhanced", Price = "~60¢", Cents = 60m },
        };

        /// <summary>Default = Fast, matching the previous "auto-upscale when a Stability key is set" behavior.</summary>
        public static UpscalerOption Default =>
            Options.FirstOrDefault(o => o.Id == "fast") ?? Options[0];

        public static UpscalerOption? FindById(string? id) =>
            Options.FirstOrDefault(o => o.Id == id);
    }
}
