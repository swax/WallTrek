using System;

namespace WallTrek.Services.TextGen
{
    public static class LlmServiceFactory
    {
        public static ILlmService CreateService(string model, string openAiApiKey, string anthropicApiKey, string googleApiKey)
        {
            if (IsAnthropicModel(model))
            {
                if (string.IsNullOrEmpty(anthropicApiKey))
                {
                    throw new ArgumentException("Anthropic API key is required for Claude models");
                }
                return new AnthropicLlmService(anthropicApiKey, model);
            }
            else if (IsGoogleModel(model))
            {
                if (string.IsNullOrEmpty(googleApiKey))
                {
                    throw new ArgumentException("Google API key is required for Gemini models");
                }
                return new GoogleLlmService(googleApiKey, model);
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

        public static bool IsGoogleModel(string model)
        {
            return model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsOpenAIModel(string model)
        {
            return !IsAnthropicModel(model) && !IsGoogleModel(model);
        }

        public static string GetProviderName(string model)
        {
            if (IsAnthropicModel(model)) return "Anthropic";
            if (IsGoogleModel(model)) return "Google";
            return "OpenAI";
        }
    }
}
