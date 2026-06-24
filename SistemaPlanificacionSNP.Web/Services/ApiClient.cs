using System.Text.Json;

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
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
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
                var content = data != null 
                    ? new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PostAsync(endpoint, content);
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
                var content = data != null
                    ? new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PutAsync(endpoint, content);
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
                var response = await _httpClient.DeleteAsync(endpoint);
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
                return await _httpClient.GetStringAsync(endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetStringAsync: {ex.Message}", ex);
                return null;
            }
        }
    }
}
