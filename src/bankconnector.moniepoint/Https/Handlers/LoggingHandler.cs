using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace zone.bankconnector.moniepoint.Https.Handlers
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;

        private static readonly HashSet<HttpMethod> BodyMethods =
            new() { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch };

        private static readonly HashSet<string> SensitiveFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "token", "password", "apikey", "api_key",
                "accountnumber", "bvn", "msisdn", "request", "data"
            };

        public LoggingHandler(ILogger<LoggingHandler> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var sw   = Stopwatch.StartNew();
            var body = BodyMethods.Contains(request.Method) && request.Content is not null
                ? await SafeReadAsync(request.Content, cancellationToken)
                : null;

            _logger.LogInformation(
                "→ {Method} {Url}  Body: {Body}",
                request.Method,
                request.RequestUri,
                body is null ? "(no body)" : MaskAndTruncate(body, 500));

            try
            {
                var response     = await base.SendAsync(request, cancellationToken);
                var responseBody = await SafeReadAsync(response.Content, cancellationToken);
                sw.Stop();

                var level = response.IsSuccessStatusCode
                    ? Microsoft.Extensions.Logging.LogLevel.Information
                    : Microsoft.Extensions.Logging.LogLevel.Warning;

                _logger.Log(level,
                    "← {StatusCode} {Method} {Url}  {Elapsed}ms  Body: {Body}",
                    (int)response.StatusCode,
                    request.Method,
                    request.RequestUri,
                    sw.ElapsedMilliseconds,
                    MaskAndTruncate(responseBody ?? string.Empty, 500));

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "✗ FAILED {Method} {Url} after {Elapsed}ms",
                    request.Method, request.RequestUri, sw.ElapsedMilliseconds);
                throw;
            }
        }


        private static async Task<string?> SafeReadAsync(HttpContent? content, CancellationToken ct)
        {
            if (content is null) return null;
            try { return await content.ReadAsStringAsync(ct); }
            catch { return "(read failed)"; }
        }

        private static string MaskAndTruncate(string json, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            string masked;
            try
            {
                using var doc    = JsonDocument.Parse(json);
                using var ms     = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false });
                WriteObject(doc.RootElement, writer);
                writer.Flush();
                masked = Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
                masked = json;
            }

            return masked.Length <= maxLen ? masked : masked[..maxLen] + "…";
        }

        private static void WriteObject(JsonElement el, Utf8JsonWriter w)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    w.WriteStartObject();
                    foreach (var p in el.EnumerateObject())
                    {
                        w.WritePropertyName(p.Name);
                        if (SensitiveFields.Contains(p.Name))
                            w.WriteStringValue("***");
                        else
                            WriteObject(p.Value, w);
                    }
                    w.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    w.WriteStartArray();
                    foreach (var item in el.EnumerateArray())
                        WriteObject(item, w);
                    w.WriteEndArray();
                    break;

                default:
                    el.WriteTo(w);
                    break;
            }
        }
    }
}
