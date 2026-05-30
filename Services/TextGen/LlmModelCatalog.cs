using System.Collections.Generic;
using System.Linq;

namespace WallTrek.Services.TextGen
{
    /// <summary>
    /// The catalog of text/LLM models shown in the LLM dropdown.
    /// The per-prompt estimate assumes a paragraph-sized generation of roughly 700 input +
    /// 300 output tokens (prompt + title + tags) at list prices. Approximate and display-only.
    /// </summary>
    public static class LlmModelCatalog
    {
        public static IReadOnlyList<LlmModelOption> Options { get; } = new List<LlmModelOption>
        {
            new() { ModelId = "gpt-5.5",      Model = "OpenAI GPT-5.5",      PerPrompt = "~1.3¢", Cents = 1.3m },
            new() { ModelId = "gpt-5.4",      Model = "OpenAI GPT-5.4",      PerPrompt = "~0.6¢", Cents = 0.6m },
            new() { ModelId = "gpt-5.4-mini", Model = "OpenAI GPT-5.4 mini", PerPrompt = "~0.2¢", Cents = 0.2m },
            new() { ModelId = "claude-opus-4-8",           Model = "Claude Opus 4.8",   PerPrompt = "~1.1¢", Cents = 1.1m },
            new() { ModelId = "claude-sonnet-4-6",         Model = "Claude Sonnet 4.6", PerPrompt = "~0.7¢", Cents = 0.7m },
            new() { ModelId = "claude-haiku-4-5-20251001", Model = "Claude Haiku 4.5",  PerPrompt = "~0.2¢", Cents = 0.2m },
            new() { ModelId = "gemini-3.1-pro-preview",   Model = "Gemini 3 Pro",   PerPrompt = "~0.9¢", Cents = 0.9m },
            new() { ModelId = "gemini-3-flash-preview", Model = "Gemini 3 Flash", PerPrompt = "~0.1¢", Cents = 0.1m },
        };

        public static LlmModelOption Default =>
            Options.FirstOrDefault(o => o.ModelId == "gpt-5.5") ?? Options[0];

        public static LlmModelOption? FindById(string? modelId) =>
            Options.FirstOrDefault(o => o.ModelId == modelId);
    }
}
