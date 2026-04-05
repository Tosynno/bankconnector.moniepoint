namespace zone.bankconnector.moniepoint.Exceptions
{
    public class TeamAptEncryptionException : Exception
    {
        public TeamAptEncryptionException(string message) : base(message) { }
        public TeamAptEncryptionException(string message, Exception inner) : base(message, inner) { }
    }

    public class TeamAptApiException : Exception
    {
        public string? StatusCode { get; }
        public string? ResponseBody { get; }

        public TeamAptApiException(string message, string? statusCode = null, string? responseBody = null)
            : base(message) { StatusCode = statusCode; ResponseBody = responseBody; }

        public TeamAptApiException(string message, Exception inner, string? statusCode = null, string? responseBody = null)
            : base(message, inner) { StatusCode = statusCode; ResponseBody = responseBody; }
    }

    public class TeamAptPendingTimeoutException : Exception
    {
        public string UniqueReference { get; }
        public int AttemptsExhausted { get; }

        public TeamAptPendingTimeoutException(string uniqueReference, int attempts)
            : base($"Transaction '{uniqueReference}' did not reach final status after {attempts} re-query attempt(s).")
        {
            UniqueReference = uniqueReference;
            AttemptsExhausted = attempts;
        }
    }

    public class ApiException : Exception
    {
        public int HttpStatusCode { get; }
        public ApiException(string message, int httpStatusCode) : base(message)
            => HttpStatusCode = httpStatusCode;
    }
}
