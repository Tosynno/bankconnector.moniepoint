using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using bankconnector.moniepoint.Dtos;
using bankconnector.moniepoint.Encryption;
using bankconnector.moniepoint.Exceptions;
using bankconnector.moniepoint.Helpers;
using bankconnector.moniepoint.Https;
using bankconnector.moniepoint.Interfaces;
using bankconnector.moniepoint.Utilities;

namespace bankconnector.moniepoint.Services
{
    public class MonieTransferService : IMonieTransferService
    {
        private readonly IHttpClientService            _http;
        private readonly TeamAptOptions                _options;
        private readonly IEncryptionServiceFactory     _encryption;
        private readonly ILogger<MonieTransferService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented          = false
        };

        public MonieTransferService(
            IHttpClientService             http,
            IOptions<TeamAptOptions>       options,
            IEncryptionServiceFactory      encryption,
            ILogger<MonieTransferService>  logger)
        {
            _http       = http;
            _options    = options.Value;
            _encryption = encryption;
            _logger     = logger;
        }

        public async Task<NameEnquiryResponseDto> NameEnquiryAsync(
            NameEnquiryDto request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.UniqueReference))
                request.UniqueReference = UniqueReferenceGenerator.Generate(_options.UniqueReferencePrefix);

            _logger.LogInformation(
                "[TeamApt] NameEnquiry → account={Account} institution={Institution} ref={Ref}",
                request.BeneficiaryAccountNumber,
                request.DestinationInstitutionCode,
                request.UniqueReference);

            return await PostEncryptedAsync<NameEnquiryDto, NameEnquiryResponseDto>("ne", request, ct);
        }


        public async Task<FundsTransferResponseDto> FundsTransferAsync(
            FundsTransferDto request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.UniqueReference))
                request.UniqueReference = UniqueReferenceGenerator.Generate(_options.UniqueReferencePrefix);

            _logger.LogInformation(
                "[TeamApt] FundsTransfer → amount={Amount} beneficiary={Account} ref={Ref}",
                request.Amount,
                request.BeneficiaryAccountNumber,
                request.UniqueReference);

            return await PostEncryptedAsync<FundsTransferDto, FundsTransferResponseDto>("ft", request, ct);
        }


        public async Task<TransactionStatusQueryResponseDto> QueryTransactionStatusAsync(
            string uniqueReference,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[TeamApt] TSQ → ref={Ref}", uniqueReference);

            return await PostEncryptedAsync<TransactionStatusQueryDto, TransactionStatusQueryResponseDto>(
                "tsq",
                new TransactionStatusQueryDto { UniqueReference = uniqueReference },
                ct);
        }

        public async Task<TransactionStatusQueryResponseDto> QueryTransactionStatusWithRetryAsync(
            string uniqueReference,
            CancellationToken ct = default)
        {
            for (var attempt = 1; attempt <= _options.MaxReQueryAttempts; attempt++)
            {
                var result = await QueryTransactionStatusAsync(uniqueReference, ct);

                if (result.IsFinal)
                {
                    _logger.LogInformation(
                        "[TeamApt] TSQ final → ref={Ref} code={Code} ({Desc}) attempts={Attempt}",
                        uniqueReference,
                        result.ResponseCode,
                        TeamAptResponseCodes.GetDescription(result.ResponseCode),
                        attempt);
                    return result;
                }

                _logger.LogWarning(
                    "[TeamApt] TSQ pending → ref={Ref} code={Code} attempt={Attempt}/{Max} — waiting {Delay}s",
                    uniqueReference,
                    result.ResponseCode,
                    attempt,
                    _options.MaxReQueryAttempts,
                    _options.ReQueryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.ReQueryDelaySeconds), ct);
            }

            throw new TeamAptPendingTimeoutException(uniqueReference, _options.MaxReQueryAttempts);
        }


        public async Task<InstitutionBalanceResponseDto> GetInstitutionBalanceAsync(
            string? type = null,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[TeamApt] GetInstitutionBalance");
            return await PostEncryptedAsync<InstitutionBalanceDto, InstitutionBalanceResponseDto>(
                "inst/balance",
                new InstitutionBalanceDto { Type = type ?? string.Empty },
                ct);
        }

        private async Task<TResponse> PostEncryptedAsync<TRequest, TResponse>(
            string endpoint,
            TRequest payload,
            CancellationToken ct)
        {
            var enc = _encryption.GetService();

            _logger.LogDebug("[TeamApt] Encryption provider: {Mode}", enc.Mode);

            var innerJson = JsonSerializer.Serialize(payload, JsonOpts);
            _logger.LogDebug("[TeamApt] Outbound JSON ({Endpoint}): {Json}", endpoint, innerJson);

            var hexCipher    = enc.EncryptToHex(innerJson);
            var envelope     = new EncryptedRequestEnvelope { Request = hexCipher };
            var envelopeJson = JsonSerializer.Serialize(envelope, JsonOpts);

            var outerEnvelope = await _http.PostAsync<string, EncryptedResponseEnvelope>(
                endpoint, envelopeJson, cancellationToken: ct);

            if (outerEnvelope is null)
                throw new TeamAptApiException(
                    $"[TeamApt] Null envelope returned for endpoint '{endpoint}'.");

            if (string.IsNullOrWhiteSpace(outerEnvelope.Data))
                throw new TeamAptApiException(
                    $"[TeamApt] Empty 'data' in response envelope for '{endpoint}'. " +
                    $"statusCode={outerEnvelope.StatusCode} message={outerEnvelope.Message}",
                    statusCode: outerEnvelope.StatusCode);

            var plainJson = enc.DecryptFromHex(outerEnvelope.Data);
            _logger.LogDebug("[TeamApt] Decrypted JSON ({Endpoint}): {Json}", endpoint, plainJson);

            return JsonSerializer.Deserialize<TResponse>(plainJson, JsonOpts)
                ?? throw new TeamAptApiException(
                    $"[TeamApt] Failed to deserialize inner response for '{endpoint}'.");
        }
    }
}
