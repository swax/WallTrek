using System;

namespace WallTrek.Services.TextGen
{
    public static class LlmServiceFactory
    {
        public static ILlmService CreateService(string model, string openAiApiKey, string anthropicApiKey)
        {
            if (IsAnthropicModel(model))
            {
                if (string.IsNullOrEmpty(anthropicApiKey))
                {
                    throw new ArgumentException("Anthropic API key is required for Claude models");
                }
                return new AnthropicLlmService(anthropicApiKey, model);
            }
            else
            {
                if (string.IsNullOrEmpty(openAiApiKey))
                {
                    throw new ArgumentException("OpenAI API key is required for OpenAI models");
                }
                return new OpenAILlmService(openAiApiKey, model);
            }
        }

        public static bool IsAnthropicModel(string model)
        {
            return model.StartsWith("claude-", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsOpenAIModel(string model)
        {
            return !IsAnthropicModel(model);
        }

        public static string GetProviderName(string model)
        {
            return IsAnthropicModel(model) ? "Anthropic" : "OpenAI";
        }
    }
}