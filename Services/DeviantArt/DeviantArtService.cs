using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using Windows.System;

namespace WallTrek.Services.DeviantArt
{
    public class DeviantArtUploadResult
    {
        public bool Success { get; set; }
        public string? DeviantArtUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DeviantArtService
    {
        private static readonly HttpClient httpClient = CreateHttpClient();
        private string? accessToken;
        private string? refreshToken;
        private DateTime tokenExpiry;
        private const string RedirectUri = "http://127.0.0.1:8245/callback"; // Local callback for desktop app

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            
            var client = new HttpClient(handler);
            
            // Set headers to avoid DA quirks
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "WallTrek/1.1");
            
            // Disable Expect: 100-continue which can cause issues with chunked responses
            client.DefaultRequestHeaders.ExpectContinue = false;
            
            return client;
        }

        public async Task<DeviantArtUploadResult> UploadImageAsync(string imagePath, string title, string description, string[] tags)
        {
            try
            {
                var settings = Settings.Instance;
                
                if (string.IsNullOrEmpty(settings.DeviantArtClientId) || string.IsNullOrEmpty(settings.DeviantArtClientSecret))
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = "DeviantArt credentials not configured. Please check settings." 
                    };
                }

                if (!File.Exists(imagePath))
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Image file not found." 
                    };
                }

                // Ensure we have a valid access token (this will trigger user auth if needed)
                if (!await EnsureValidUserTokenAsync(settings.DeviantArtClientId, settings.DeviantArtClientSecret))
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = "DeviantArt user authorization required. Please authorize the application first." 
                    };
                }

                // Upload the image
                return await UploadToDeviantArtAsync(imagePath, title, description, tags);
            }
            catch (Exception ex)
            {
                return new DeviantArtUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Upload failed: {ex.Message}" 
                };
            }
        }

        public string GetAuthorizationUrl(string clientId)
        {
            var state = Guid.NewGuid().ToString("N")[..16]; // Generate random state for security
            var url = $"https://www.deviantart.com/oauth2/authorize" +
                      $"?response_type=code" +
                      $"&client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                      $"&scope=stash%20publish" +
                      $"&state={state}";
            return url;
        }

        private async Task<bool> EnsureValidUserTokenAsync(string clientId, string clientSecret)
        {
            // Check if we have a valid token
            if (!string.IsNullOrEmpty(accessToken) && DateTime.UtcNow < tokenExpiry)
            {
                return true;
            }

            // Try to refresh the token if we have a refresh token
            if (!string.IsNullOrEmpty(refreshToken))
            {
                return await RefreshAccessTokenAsync(clientId, clientSecret);
            }

            // No valid token and no refresh token - user needs to authorize
            return false;
        }

        public async Task<bool> ExchangeCodeForTokenAsync(string code, string clientId, string clientSecret)
        {
            try
            {
                var tokenRequest = CreateTokenRequest(new[]
                {
                    ("grant_type", "authorization_code"),
                    ("code", code),
                    ("client_id", clientId),
                    ("client_secret", clientSecret),
                    ("redirect_uri", RedirectUri)
                });

                var response = await httpClient.PostAsync("https://www.deviantart.com/oauth2/token", tokenRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return ProcessTokenResponse(responseContent);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RefreshAccessTokenAsync(string clientId, string clientSecret)
        {
            try
            {
                var tokenRequest = CreateTokenRequest(new[]
                {
                    ("grant_type", "refresh_token"),
                    ("refresh_token", refreshToken!),
                    ("client_id", clientId),
                    ("client_secret", clientSecret)
                });

                var response = await httpClient.PostAsync("https://www.deviantart.com/oauth2/token", tokenRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    // Clear invalid tokens
                    accessToken = null;
                    refreshToken = null;
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return ProcessTokenResponse(responseContent);
            }
            catch
            {
                return false;
            }
        }

        private bool ProcessTokenResponse(string responseContent)
        {
            try
            {
                var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (tokenData.TryGetProperty("access_token", out var tokenElement))
                {
                    accessToken = tokenElement.GetString();
                    
                    // Get refresh token if provided
                    if (tokenData.TryGetProperty("refresh_token", out var refreshElement))
                    {
                        refreshToken = refreshElement.GetString();
                    }
                    
                    // Set token expiry (default to 1 hour if not specified)
                    var expiresIn = 3600; // Default 1 hour
                    if (tokenData.TryGetProperty("expires_in", out var expiryElement))
                    {
                        expiresIn = expiryElement.GetInt32();
                    }
                    
                    tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 1 minute buffer
                    
                    // Save tokens to settings
                    SaveTokensToSettings();
                    
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public DeviantArtService()
        {
            LoadTokensFromSettings();
        }

        private void SaveTokensToSettings()
        {
            var settings = Settings.Instance;
            settings.DeviantArtAccessToken = accessToken;
            settings.DeviantArtRefreshToken = refreshToken;
            settings.DeviantArtTokenExpiry = tokenExpiry;
            settings.Save();
        }

        private void LoadTokensFromSettings()
        {
            var settings = Settings.Instance;
            accessToken = settings.DeviantArtAccessToken;
            refreshToken = settings.DeviantArtRefreshToken;
            tokenExpiry = settings.DeviantArtTokenExpiry ?? DateTime.MinValue;
        }

        private static FormUrlEncodedContent CreateTokenRequest((string key, string value)[] parameters)
        {
            return new FormUrlEncodedContent(parameters.Select(p => new KeyValuePair<string, string>(p.key, p.value)));
        }

        private async Task<DeviantArtUploadResult> UploadToDeviantArtAsync(string imagePath, string title, string description, string[] tags)
        {
            try
            {
                // Step 1: Submit to stash
                using var submitContent = new MultipartFormDataContent();
                
                // Add the image file
                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var imageContent = new ByteArrayContent(imageBytes);
                var fileName = Path.GetFileName(imagePath);
                imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(GetMimeType(fileName));
                submitContent.Add(imageContent, "test", fileName);

                // Add metadata for submit
                submitContent.Add(new StringContent(accessToken!), "access_token");
                submitContent.Add(new StringContent(title), "title");
                submitContent.Add(new StringContent(description ?? ""), "artist_comments");
                submitContent.Add(new StringContent("false"), "noai");
                submitContent.Add(new StringContent("true"), "is_ai_generated");
                // Add tags - always include walltrek tag
                var allTags = new List<string> { "walltrek", "dalle3" };
                allTags.AddRange(tags);
                
                foreach (var tag in allTags)
                {
                    submitContent.Add(new StringContent(tag), "tags[]");
                }

                // Set authorization header (don't clear default headers since we set Accept/User-Agent in CreateHttpClient)
                if (httpClient.DefaultRequestHeaders.Authorization?.Parameter != accessToken)
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }
                
                // Submit to stash
                var submitResponse = await httpClient.PostAsync("https://www.deviantart.com/api/v1/oauth2/stash/submit", submitContent);
                
                var submitResponseContent = await submitResponse.Content.ReadAsStringAsync();
                
                // Handle submit response errors
                if (!submitResponse.IsSuccessStatusCode)
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Submit failed: {submitResponse.StatusCode} - {submitResponseContent}" 
                    };
                }

                // Handle empty or whitespace-only responses
                if (string.IsNullOrWhiteSpace(submitResponseContent))
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Empty response from submit (Status: {submitResponse.StatusCode})" 
                    };
                }

                // Parse submit response to get itemid
                JsonElement submitData;
                try
                {
                    submitData = JsonSerializer.Deserialize<JsonElement>(submitResponseContent);
                }
                catch (JsonException ex)
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Failed to parse submit response: {ex.Message}" 
                    };
                }

                if (!submitData.TryGetProperty("itemid", out var itemIdElement))
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Unexpected submit response (no itemid): {submitResponseContent.Substring(0, Math.Min(200, submitResponseContent.Length))}..." 
                    };
                }
                var itemId = itemIdElement.GetInt64();

                // Step 2: Publish the stash item
                using var publishContent = new MultipartFormDataContent();
                publishContent.Add(new StringContent(accessToken!), "access_token");
                publishContent.Add(new StringContent(itemId.ToString()), "itemid");
                publishContent.Add(new StringContent("false"), "is_mature");
                publishContent.Add(new StringContent("true"), "agree_submission");
                publishContent.Add(new StringContent("true"), "agree_tos");

                var publishResponse = await httpClient.PostAsync("https://www.deviantart.com/api/v1/oauth2/stash/publish", publishContent);
                var publishResponseContent = await publishResponse.Content.ReadAsStringAsync();

                if (!publishResponse.IsSuccessStatusCode)
                {
                    return new DeviantArtUploadResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Publish failed: {publishResponse.StatusCode} - {publishResponseContent}" 
                    };
                }

                // Parse publish response to get deviation URL if available
                string? deviantArtUrl = null;
                try
                {
                    var publishData = JsonSerializer.Deserialize<JsonElement>(publishResponseContent);
                    if (publishData.TryGetProperty("url", out var urlElement))
                    {
                        deviantArtUrl = urlElement.GetString();
                    }
                }
                catch (JsonException)
                {
                    // If we can't parse the publish response, that's ok - we'll fall back to sta.sh URL
                }

                // If no URL from publish response, build sta.sh URL from itemid
                if (string.IsNullOrEmpty(deviantArtUrl))
                {
                    deviantArtUrl = $"https://sta.sh/1{itemId}";
                }

                return new DeviantArtUploadResult 
                { 
                    Success = true, 
                    DeviantArtUrl = deviantArtUrl 
                };
            }
            catch (Exception ex)
            {
                return new DeviantArtUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Upload error: {ex.Message}" 
                };
            }
        }

        private static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}