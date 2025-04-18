using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace AINicheGen.Services
{
    public class ContentService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private readonly string _baseUrl = "http://localhost:5000/generate";

        public ContentService(IHttpClientFactory httpClientFactory, AuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _authService = authService;
        }

        public async Task<(bool Success, string Content, string Error)> GenerateContent(
            string niche, string contentType, bool useOpenAI, string language)
        {
            try
            {
                var token = await _authService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return (false, null, "Not authenticated");
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-Token", token);

                var payload = new
                {
                    niche = niche,
                    type = contentType,
                    engine = useOpenAI ? "openai" : "ollama",
                    language = language
                };

                var response = await _httpClient.PostAsJsonAsync(_baseUrl, payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AIResponse>();
                    return (true, result?.Content ?? "No content returned.", null);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return (false, null, "Authentication required. Please log in.");
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    return (false, null, $"Error: {response.StatusCode} - {errorText}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Error: {ex.Message}");
            }
        }

        public class AIResponse
        {
            public string Content { get; set; }
        }
    }
}