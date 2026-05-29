namespace WallTrek.Services.TextGen
{
    /// <summary>
    /// A selectable text/LLM model shown in the LLM dropdown, with display columns
    /// (model / rate / estimated cost per prompt) for the grid dropdown. The provider is
    /// inferred from <see cref="ModelId"/> by <see cref="LlmServiceFactory"/>.
    /// </summary>
    public class LlmModelOption
    {
        /// <summary>API model id; also the value persisted in settings.</summary>
        public string ModelId { get; init; } = "";

        // --- Display columns ---
        public string Model { get; init; } = "";
        public string PerPrompt { get; init; } = "";  // estimated cost for one generation, e.g. "~1.3¢"

        /// <summary>Approximate cost per prompt in US cents (drives the total-cost estimate).</summary>
        public decimal Cents { get; init; }
    }
}
