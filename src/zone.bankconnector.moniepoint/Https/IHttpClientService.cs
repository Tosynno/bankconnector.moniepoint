namespace zone.bankconnector.moniepoint.Https
{
    public interface IHttpClientService
    {
        Task<TResponse?> GetAsync<TResponse>(
            string endpoint,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default);

        Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default);

        Task<TResponse?> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default);

        Task<TResponse?> PatchAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string endpoint,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default);

        Task<TResponse?> SendJsonAsync<TRequest, TResponse>(
            HttpMethod method,
            string endpoint,
            TRequest? request = default,
            IDictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default);
    }
}
