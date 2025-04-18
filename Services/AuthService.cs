using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace AINicheGen.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly string _baseUrl = "http://localhost:5000/auth";

        public AuthService(IHttpClientFactory httpClientFactory, ProtectedSessionStorage sessionStorage)
        {
            _httpClient = httpClientFactory.CreateClient();
            _sessionStorage = sessionStorage;
        }

        public async Task<bool> IsAuthenticated()
        {
            try
            {
                var token = await GetToken();
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetToken()
        {
            var result = await _sessionStorage.GetAsync<string>("auth_token");
            return result.Success ? result.Value : string.Empty;
        }

        public async Task<(bool Success, string Message)> Register(string email, string password)
        {
            try
            {
                var payload = new
                {
                    email,
                    password
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/register", payload);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Registration successful. Please log in.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                    return (false, error?.Error ?? "Registration failed.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> Login(string email, string password)
        {
            try
            {
                var payload = new
                {
                    email,
                    password
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/login", payload);
                
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (loginResponse?.Token != null)
                    {
                        await _sessionStorage.SetAsync("auth_token", loginResponse.Token);
                        await _sessionStorage.SetAsync("user_email", email);
                        return (true, "Login successful");
                    }
                    
                    return (false, "No token received");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                    return (false, error?.Error ?? "Login failed");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task Logout()
        {
            try
            {
                var token = await GetToken();
                if (!string.IsNullOrEmpty(token))
                {
                    // Add the token to request headers
                    _httpClient.DefaultRequestHeaders.Add("X-API-Token", token);
                    await _httpClient.PostAsync($"{_baseUrl}/logout", null);
                }
            }
            catch
            {
                // Ignore errors during logout
            }
            finally
            {
                await _sessionStorage.DeleteAsync("auth_token");
                await _sessionStorage.DeleteAsync("user_email");
            }
        }

        public class LoginResponse
        {
            public string Message { get; set; }
            public string Token { get; set; }
            public UserInfo User { get; set; }
        }

        public class UserInfo
        {
            public string Email { get; set; }
            public bool Is_paid { get; set; }
        }

        public class ErrorResponse
        {
            public string Error { get; set; }
        }
    }
}