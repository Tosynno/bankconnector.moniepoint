using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using zone.bankconnector.moniepoint.Dtos;
using zone.bankconnector.moniepoint.Exceptions;
using zone.bankconnector.moniepoint.Https;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Services;
using zone.bankconnector.moniepoint.Tests.Fixtures;
using zone.bankconnector.moniepoint.Utilities;
using MockFactory = zone.bankconnector.moniepoint.Tests.Fixtures.MockFactory;

namespace zone.bankconnector.moniepoint.Tests;

public class MonieTransferServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    private static string ToHex(string plain) =>
        Convert.ToHexString(Encoding.UTF8.GetBytes(plain));

    private static Mock<IHttpClientService> HttpReturning<T>(T inner)
    {
        var innerJson = JsonSerializer.Serialize(inner, JsonOpts);
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EncryptedResponseEnvelope
            {
                StatusCode = "00",
                Message    = "Success",
                Data       = ToHex(innerJson)
            });
        return http;
    }

    private static Mock<IHttpClientService> HttpReturningNull()
    {
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EncryptedResponseEnvelope?)null);
        return http;
    }

    private static Mock<IHttpClientService> HttpReturningEnvelope(EncryptedResponseEnvelope env)
    {
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(env);
        return http;
    }

    private static (MonieTransferService svc, Mock<IEncryptionService> enc) BuildService(
        Mock<IHttpClientService> http, TeamAptOptions? opts = null)
    {
        var options    = opts ?? MockFactory.DefaultRsaOptions();
        var encMock    = MockFactory.PassthroughEncryption();
        var encFactory = MockFactory.EncryptionFactory(encMock.Object);

        var svc = new MonieTransferService(
            http.Object,
            Options.Create(options),
            encFactory.Object,
            NullLogger<MonieTransferService>.Instance);

        return (svc, encMock);
    }


    [Fact]
    public async Task NameEnquiry_HappyPath_ReturnsCorrectFieldsAndIsSuccessful()
    {
        var (svc, _) = BuildService(HttpReturning(new NameEnquiryResponseDto
        {
            ResponseCode              = "00",
            BeneficiaryAccountName    = "John Doe",
            BeneficiaryAccountNumber  = "0123456789",
            BeneficiaryKycLevel       = "1",
            BeneficiaryBankVerificationNumber = "12345678901",
            UniqueReference           = "APT00015260424NE0001"
        }));

        var result = await svc.NameEnquiryAsync(new NameEnquiryDto
        {
            UniqueReference            = "APT00015260424NE0001",
            BeneficiaryAccountNumber   = "0123456789",
            DestinationInstitutionCode = "000013"
        });

        Assert.Equal("00",          result.ResponseCode);
        Assert.Equal("John Doe",    result.BeneficiaryAccountName);
        Assert.Equal("0123456789",  result.BeneficiaryAccountNumber);
        Assert.Equal("1",           result.BeneficiaryKycLevel);
        Assert.Equal("12345678901", result.BeneficiaryBankVerificationNumber);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task NameEnquiry_EmptyUniqueReference_GeneratedWith32CharsAndCorrectPrefix()
    {
        var (svc, _) = BuildService(HttpReturning(new NameEnquiryResponseDto { ResponseCode = "00" }));
        var req = new NameEnquiryDto { UniqueReference = "" };

        await svc.NameEnquiryAsync(req);

        Assert.Equal(32, req.UniqueReference.Length);
        Assert.StartsWith("APT00015", req.UniqueReference);
    }

    [Fact]
    public async Task NameEnquiry_WhitespaceUniqueReference_IsAutoReplaced()
    {
        var (svc, _) = BuildService(HttpReturning(new NameEnquiryResponseDto { ResponseCode = "00" }));
        var req = new NameEnquiryDto { UniqueReference = "   " };

        await svc.NameEnquiryAsync(req);

        Assert.False(string.IsNullOrWhiteSpace(req.UniqueReference));
    }

    [Fact]
    public async Task NameEnquiry_PostsToNeEndpoint_ExactlyOnce()
    {
        var http = HttpReturning(new NameEnquiryResponseDto { ResponseCode = "00" });
        var (svc, _) = BuildService(http);

        await svc.NameEnquiryAsync(new NameEnquiryDto { UniqueReference = "APT00015260424NE0002" });

        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "ne", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NameEnquiry_EncryptAndDecryptEachCalledOnce()
    {
        var (svc, enc) = BuildService(HttpReturning(new NameEnquiryResponseDto { ResponseCode = "00" }));

        await svc.NameEnquiryAsync(new NameEnquiryDto { UniqueReference = "APT00015260424NE0003" });

        enc.Verify(e => e.EncryptToHex(It.IsAny<string>()), Times.Once);
        enc.Verify(e => e.DecryptFromHex(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NameEnquiry_NullHttpEnvelope_ThrowsTeamAptApiException()
    {
        var (svc, _) = BuildService(HttpReturningNull());

        await Assert.ThrowsAsync<TeamAptApiException>(() =>
            svc.NameEnquiryAsync(new NameEnquiryDto { UniqueReference = "APT00015260424NE0004" }));
    }

    [Fact]
    public async Task NameEnquiry_NullDataInEnvelope_ThrowsTeamAptApiExceptionWithStatusCode()
    {
        var (svc, _) = BuildService(HttpReturningEnvelope(
            new EncryptedResponseEnvelope { StatusCode = "96", Message = "System error", Data = null }));

        var ex = await Assert.ThrowsAsync<TeamAptApiException>(() =>
            svc.NameEnquiryAsync(new NameEnquiryDto { UniqueReference = "APT00015260424NE0005" }));

        Assert.Equal("96", ex.StatusCode);
    }

    [Fact]
    public async Task NameEnquiry_WhitespaceDataInEnvelope_ThrowsTeamAptApiException()
    {
        var (svc, _) = BuildService(HttpReturningEnvelope(
            new EncryptedResponseEnvelope { StatusCode = "00", Data = "   " }));

        await Assert.ThrowsAsync<TeamAptApiException>(() =>
            svc.NameEnquiryAsync(new NameEnquiryDto { UniqueReference = "APT00015260424NE0006" }));
    }


    [Fact]
    public async Task FundsTransfer_HappyPath_ReturnsPaymentReferenceAndIsSuccessful()
    {
        var (svc, _) = BuildService(HttpReturning(new FundsTransferResponseDto
        {
            ResponseCode     = "00",
            PaymentReference = "PAY-2024-001",
            UniqueReference  = "APT00015260424FT0001",
            Amount           = "500000"
        }));

        var result = await svc.FundsTransferAsync(new FundsTransferDto
        {
            UniqueReference            = "APT00015260424FT0001",
            Amount                     = "500000",
            BeneficiaryAccountNumber   = "0123456789",
            BeneficiaryAccountName     = "John Doe",
            DestinationInstitutionCode = "000013",
            Narration                  = "School fees"
        });

        Assert.Equal("00",           result.ResponseCode);
        Assert.Equal("PAY-2024-001", result.PaymentReference);
        Assert.Equal("500000",       result.Amount);
        Assert.True(result.IsSuccessful);
        Assert.False(result.IsPending);
    }

    [Fact]
    public async Task FundsTransfer_EmptyReference_GeneratesReference()
    {
        var (svc, _) = BuildService(HttpReturning(new FundsTransferResponseDto { ResponseCode = "00" }));
        var req = new FundsTransferDto { UniqueReference = "" };

        await svc.FundsTransferAsync(req);

        Assert.Equal(32, req.UniqueReference.Length);
    }

    [Fact]
    public async Task FundsTransfer_PostsToFtEndpoint_ExactlyOnce()
    {
        var http = HttpReturning(new FundsTransferResponseDto { ResponseCode = "00" });
        var (svc, _) = BuildService(http);

        await svc.FundsTransferAsync(new FundsTransferDto { UniqueReference = "APT00015260424FT0002" });

        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "ft", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("09")]
    [InlineData("97")]
    public async Task FundsTransfer_PendingOrTimeoutCode_IsPendingTrue(string code)
    {
        var (svc, _) = BuildService(HttpReturning(new FundsTransferResponseDto { ResponseCode = code }));

        var result = await svc.FundsTransferAsync(new FundsTransferDto { UniqueReference = "APT00015260424FT0003" });

        Assert.True(result.IsPending);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task FundsTransfer_NullEnvelope_ThrowsTeamAptApiException()
    {
        var (svc, _) = BuildService(HttpReturningNull());

        await Assert.ThrowsAsync<TeamAptApiException>(() =>
            svc.FundsTransferAsync(new FundsTransferDto { UniqueReference = "APT00015260424FT0004" }));
    }


    [Fact]
    public async Task QueryStatus_HappyPath_IsFinalAndIsSuccessful()
    {
        var (svc, _) = BuildService(HttpReturning(new TransactionStatusQueryResponseDto
        {
            ResponseCode    = "00",
            UniqueReference = "APT00015260424TSQ001"
        }));

        var result = await svc.QueryTransactionStatusAsync("APT00015260424TSQ001");

        Assert.Equal("00", result.ResponseCode);
        Assert.True(result.IsSuccessful);
        Assert.True(result.IsFinal);
    }

    [Fact]
    public async Task QueryStatus_PostsToTsqEndpoint_ExactlyOnce()
    {
        var http = HttpReturning(new TransactionStatusQueryResponseDto { ResponseCode = "00" });
        var (svc, _) = BuildService(http);

        await svc.QueryTransactionStatusAsync("APT00015260424TSQ002");

        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "tsq", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("09")]
    [InlineData("97")]
    public async Task QueryStatus_PendingOrTimeout_IsFinalFalse(string code)
    {
        var (svc, _) = BuildService(HttpReturning(new TransactionStatusQueryResponseDto { ResponseCode = code }));

        var result = await svc.QueryTransactionStatusAsync("APT00015260424TSQ003");

        Assert.False(result.IsFinal);
        Assert.True(result.IsPending);
    }


    [Fact]
    public async Task QueryWithRetry_FinalOnFirstCall_ReturnsAndCallsOnce()
    {
        var http = HttpReturning(new TransactionStatusQueryResponseDto { ResponseCode = "00" });
        var (svc, _) = BuildService(http);

        var result = await svc.QueryTransactionStatusWithRetryAsync("APT00015260424TSQ004");

        Assert.Equal("00", result.ResponseCode);
        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "tsq", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueryWithRetry_PendingThenFinal_CallsTwiceAndReturnsApproved()
    {
        var call = 0;
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                "tsq", It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                call++;
                var code = call == 1 ? "09" : "00";
                return new EncryptedResponseEnvelope
                {
                    StatusCode = "00",
                    Data       = ToHex(JsonSerializer.Serialize(
                        new TransactionStatusQueryResponseDto { ResponseCode = code }, JsonOpts))
                };
            });

        var opts = MockFactory.DefaultRsaOptions();
        opts.MaxReQueryAttempts  = 5;
        opts.ReQueryDelaySeconds = 0;
        var (svc, _) = BuildService(http, opts);

        var result = await svc.QueryTransactionStatusWithRetryAsync("APT00015260424TSQ005");

        Assert.Equal("00", result.ResponseCode);
        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "tsq", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task QueryWithRetry_AlwaysPending_ThrowsPendingTimeoutExceptionWithCorrectReference()
    {
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                "tsq", It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new EncryptedResponseEnvelope
            {
                StatusCode = "00",
                Data       = ToHex(JsonSerializer.Serialize(
                    new TransactionStatusQueryResponseDto { ResponseCode = "09" }, JsonOpts))
            });

        var opts = MockFactory.DefaultRsaOptions();
        opts.MaxReQueryAttempts  = 2;
        opts.ReQueryDelaySeconds = 0;
        var (svc, _) = BuildService(http, opts);

        var ex = await Assert.ThrowsAsync<TeamAptPendingTimeoutException>(() =>
            svc.QueryTransactionStatusWithRetryAsync("APT00015260424TSQ006"));

        Assert.Equal("APT00015260424TSQ006", ex.UniqueReference);
        Assert.Equal(2, ex.AttemptsExhausted);
    }

    [Fact]
    public async Task QueryWithRetry_AlwaysPending_CallsExactlyMaxAttemptsTimes()
    {
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                "tsq", It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new EncryptedResponseEnvelope
            {
                StatusCode = "00",
                Data       = ToHex(JsonSerializer.Serialize(
                    new TransactionStatusQueryResponseDto { ResponseCode = "09" }, JsonOpts))
            });

        var opts = MockFactory.DefaultRsaOptions();
        opts.MaxReQueryAttempts  = 3;
        opts.ReQueryDelaySeconds = 0;
        var (svc, _) = BuildService(http, opts);

        await Assert.ThrowsAsync<TeamAptPendingTimeoutException>(() =>
            svc.QueryTransactionStatusWithRetryAsync("APT00015260424TSQ007"));

        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "tsq", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task QueryWithRetry_TimeoutCode_TreatedAsPendingAndThrows()
    {
        var http = MockFactory.Http();
        http.Setup(h => h.PostAsync<string, EncryptedResponseEnvelope>(
                "tsq", It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new EncryptedResponseEnvelope
            {
                StatusCode = "00",
                Data       = ToHex(JsonSerializer.Serialize(
                    new TransactionStatusQueryResponseDto { ResponseCode = "97" }, JsonOpts))
            });

        var opts = MockFactory.DefaultRsaOptions();
        opts.MaxReQueryAttempts  = 2;
        opts.ReQueryDelaySeconds = 0;
        var (svc, _) = BuildService(http, opts);

        await Assert.ThrowsAsync<TeamAptPendingTimeoutException>(() =>
            svc.QueryTransactionStatusWithRetryAsync("APT00015260424TSQ008"));
    }


    [Fact]
    public async Task GetInstitutionBalance_HappyPath_ReturnsAmountDecimalAndIsSuccessful()
    {
        var (svc, _) = BuildService(HttpReturning(
            new InstitutionBalanceResponseDto { ResponseCode = "00", Amount = "1500000.50" }));

        var result = await svc.GetInstitutionBalanceAsync("CURRENT");

        Assert.Equal("00",         result.ResponseCode);
        Assert.Equal("1500000.50", result.Amount);
        Assert.Equal(1500000.50m,  result.AmountDecimal);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task GetInstitutionBalance_NullType_DoesNotThrow()
    {
        var (svc, _) = BuildService(HttpReturning(
            new InstitutionBalanceResponseDto { ResponseCode = "00", Amount = "0" }));

        var result = await svc.GetInstitutionBalanceAsync(null);

        Assert.NotNull(result);
        Assert.Equal(0m, result.AmountDecimal);
    }

    [Fact]
    public async Task GetInstitutionBalance_PostsToInstBalanceEndpoint_ExactlyOnce()
    {
        var http = HttpReturning(new InstitutionBalanceResponseDto { ResponseCode = "00" });
        var (svc, _) = BuildService(http);

        await svc.GetInstitutionBalanceAsync();

        http.Verify(h => h.PostAsync<string, EncryptedResponseEnvelope>(
            "inst/balance", It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInstitutionBalance_ZeroBalance_AmountDecimalIsZero()
    {
        var (svc, _) = BuildService(HttpReturning(
            new InstitutionBalanceResponseDto { ResponseCode = "00", Amount = "0" }));

        var result = await svc.GetInstitutionBalanceAsync();

        Assert.Equal(0m, result.AmountDecimal);
    }
}
