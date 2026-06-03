using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using bankconnector.moniepoint.Dtos;
using bankconnector.moniepoint.Exceptions;

namespace bankconnector.moniepoint.Https
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient            _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<HttpClientService> _logger;

        public HttpClientService(
            HttpClient httpClient,
            ILogger<HttpClientService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger     = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public async Task<TResponse?> GetAsync<TResponse>(
            string endpoint,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            using var resp = await SendRawAsync(HttpMethod.Get, endpoint, null, customHeaders, cancellationToken);
            return await HandleResponseAsync<TResponse>(resp, cancellationToken);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            HttpContent content = request is string raw
                ? new StringContent(raw, System.Text.Encoding.UTF8, "application/json")
                : JsonContent.Create(request, options: _jsonOptions);

            using var resp = await SendRawAsync(HttpMethod.Post, endpoint, content, customHeaders, cancellationToken);
            return await HandleResponseAsync<TResponse>(resp, cancellationToken);
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            using var resp = await SendRawAsync(HttpMethod.Put, endpoint,
                JsonContent.Create(request, options: _jsonOptions), customHeaders, cancellationToken);
            return await HandleResponseAsync<TResponse>(resp, cancellationToken);
        }

        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            using var resp = await SendRawAsync(HttpMethod.Patch, endpoint,
                JsonContent.Create(request, options: _jsonOptions), customHeaders, cancellationToken);
            return await HandleResponseAsync<TResponse>(resp, cancellationToken);
        }

        public async Task DeleteAsync(
            string endpoint,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            using var resp = await SendRawAsync(HttpMethod.Delete, endpoint, null, customHeaders, cancellationToken);
            await HandleResponseAsync<object>(resp, cancellationToken);
        }

        public async Task<TResponse?> SendJsonAsync<TRequest, TResponse>(
            HttpMethod method,
            string endpoint,
            TRequest? request = default,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            HttpContent? content = request is not null
                ? JsonContent.Create(request, options: _jsonOptions)
                : null;

            using var resp = await SendRawAsync(method, endpoint, content, customHeaders, cancellationToken);
            return await HandleResponseAsync<TResponse>(resp, cancellationToken);
        }


        private async Task<HttpResponseMessage> SendRawAsync(
            HttpMethod method,
            string endpoint,
            HttpContent? content,
            IDictionary<string, string>? customHeaders,
            CancellationToken ct)
        {
            var req = new HttpRequestMessage(method, endpoint) { Content = content };
            if (customHeaders is not null)
                foreach (var (k, v) in customHeaders)
                    req.Headers.TryAddWithoutValidation(k, v);

            return await _httpClient.SendAsync(req, ct);
        }

        private async Task<TResponse?> HandleResponseAsync<TResponse>(
            HttpResponseMessage response,
            CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "HTTP {Status} from {Url} | Body: {Body}",
                    (int)response.StatusCode, response.RequestMessage?.RequestUri, body);

                ApiErrorDto? apiError = null;
                try { apiError = JsonSerializer.Deserialize<ApiErrorDto>(body, _jsonOptions); }
                catch { /* ignore */ }

                throw new ApiException(apiError?.StatusMessage ?? body, (int)response.StatusCode);
            }

            if (typeof(TResponse) == typeof(string))  return (TResponse?)(object)body;
            if (typeof(TResponse) == typeof(byte[]))  return (TResponse?)(object)
                await response.Content.ReadAsByteArrayAsync(ct);
            if (typeof(TResponse) == typeof(object) || string.IsNullOrWhiteSpace(body))
                return default;

            return JsonSerializer.Deserialize<TResponse>(body, _jsonOptions);
        }
    }
}
