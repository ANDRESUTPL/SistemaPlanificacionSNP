using System.Text.Json;
using System.Net.Http.Headers;

namespace SistemaPlanificacionSNP.Web.Services
{
    /// <summary>
    /// Cliente HTTP para comunicación con API Gateway
    /// </summary>
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object? data = null);
        Task<T?> PutAsync<T>(string endpoint, object? data = null);
        Task<bool> DeleteAsync(string endpoint);
        Task<string?> GetStringAsync(string endpoint);
        Task<HttpResponseMessage?> SendAsync(HttpMethod method, string endpoint, object? data = null);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient httpClient, IAuthService authService, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string endpoint, object? data = null)
        {
            try
            {
                using var request = new HttpRequestMessage(method, endpoint);
                var token = _authService.GetAccessToken();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if (data != null)
                {
                    request.Content = new StringContent(
                        JsonSerializer.Serialize(data),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                }

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendAsync: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await SendAsync(HttpMethod.Get, endpoint);
                if (response == null)
                {
                    return default;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                _logger.LogWarning($"GET {endpoint} returned {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAsync: {ex.Message}", ex);
                return default;
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
        {
            try
            {
                var response = await SendAsync(HttpMethod.Post, endpoint, data);
                if (response == null)
                {
                    return default;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                _logger.LogWarning($"POST {endpoint} returned {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in PostAsync: {ex.Message}", ex);
                return default;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, object? data = null)
        {
            try
            {
                var response = await SendAsync(HttpMethod.Put, endpoint, data);
                if (response == null)
                {
                    return default;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                _logger.LogWarning($"PUT {endpoint} returned {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in PutAsync: {ex.Message}", ex);
                return default;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await SendAsync(HttpMethod.Delete, endpoint);
                if (response == null)
                {
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteAsync: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<string?> GetStringAsync(string endpoint)
        {
            try
            {
                var response = await SendAsync(HttpMethod.Get, endpoint);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetStringAsync: {ex.Message}", ex);
                return null;
            }
        }
    }
}
