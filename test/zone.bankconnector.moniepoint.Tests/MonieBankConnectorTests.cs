using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using zone.bankconnector.moniepoint.Dtos;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Models;
using zone.bankconnector.moniepoint.Services;
using zone.bankconnector.moniepoint.Tests.Fixtures;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Tests;

public class MonieBankConnectorTests
{
    private static readonly TeamAptOptions DefaultOpts = new()
    {
        GLAccountName   = "ZoneGL",
        GLAccountNumber = "0000000001",
        GLKycLevel      = "1"
    };

    private static MonieBankConnector BuildConnector(Mock<IMonieTransferService> svcMock)
    {
        return new MonieBankConnector(
            svcMock.Object,
            NullLogger<MonieBankConnector>.Instance,
            Options.Create(DefaultOpts));
    }


    [Fact]
    public async Task NameEnquiryAsync_MapsRequestFieldsCorrectly()
    {
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.NameEnquiryAsync(It.IsAny<NameEnquiryDto>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new NameEnquiryResponseDto
           {
               ResponseCode             = "00",
               BeneficiaryAccountName   = "Jane Smith",
               BeneficiaryAccountNumber = "9876543210",
               BeneficiaryKycLevel      = "2",
               BeneficiaryBankVerificationNumber = "55566677789"
           });

        var connector = BuildConnector(svc);

        var result = await connector.NameEnquiryAsync(new NameEnquiryRequest
        {
            AccountNumber      = "9876543210",
            DestinationBankCode= "000013",
            TransactionReference = "APT00015260424NE0001"
        });

        Assert.Equal("00",          result.ResponseCode);
        Assert.Equal("Jane Smith",  result.AccountName);
        Assert.Equal("9876543210",  result.AccountNumber);
        Assert.Equal(2,             result.KYCLevel);
        Assert.Equal("55566677789", result.BVN);
    }

    

    [Fact]
    public async Task IntraBankAsync_HappyPath_ResponseCodeMappedCorrectly()
    {
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.FundsTransferAsync(It.IsAny<FundsTransferDto>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new FundsTransferResponseDto
           {
               ResponseCode     = "00",
               PaymentReference = "PAY-2024-001"
           });

        var connector = BuildConnector(svc);

        var result = await connector.IntraBankAsync(new TransferRequest
        {
            ToAccount            = "0123456789",
            ToAccountName        = "John Doe",
            AmountToDebit        = 500000,
            DestinationBankCode  = "000013",
            TransactionReference = "APT00015260424FT0001",
            NameEnquiryID        = "NE-REF-001",
            Narration            = "Test transfer",
            BeneficiaryKYCLevel  = 1,
            BeneficiaryBVN       = "12345678901",
            OriginatorBVN        = "98765432101"
        });

        Assert.Equal("00", result.ResponseCode);
        Assert.NotNull(result.Data);
        Assert.Equal("00",          result.Data!.ResponseCode);
        Assert.Equal("PAY-2024-001", result.Data.TransactionReference);
        Assert.NotNull(result.Data.TransactionDateTime);
    }

    [Fact]
    public async Task IntraBankAsync_MapsGlAccountFromOptions()
    {
        FundsTransferDto? captured = null;
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.FundsTransferAsync(It.IsAny<FundsTransferDto>(), It.IsAny<CancellationToken>()))
           .Callback<FundsTransferDto, CancellationToken>((dto, _) => captured = dto)
           .ReturnsAsync(new FundsTransferResponseDto { ResponseCode = "00" });

        var connector = BuildConnector(svc);
        await connector.IntraBankAsync(new TransferRequest
        {
            ToAccount            = "0123456789",
            AmountToDebit        = 100,
            TransactionReference = "APT00015ABCDEF123456"
        });

        Assert.NotNull(captured);
        Assert.Equal("ZoneGL",     captured!.OriginatorAccountName);
        Assert.Equal("0000000001", captured.OriginatorAccountNumber);
        Assert.Equal("1",          captured.OriginatorKycLevel);
    }

    [Fact]
    public async Task IntraBankAsync_MapsAllTransferRequestFields()
    {
        FundsTransferDto? captured = null;
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.FundsTransferAsync(It.IsAny<FundsTransferDto>(), It.IsAny<CancellationToken>()))
           .Callback<FundsTransferDto, CancellationToken>((dto, _) => captured = dto)
           .ReturnsAsync(new FundsTransferResponseDto { ResponseCode = "00" });

        var connector = BuildConnector(svc);
        await connector.IntraBankAsync(new TransferRequest
        {
            ToAccount            = "0123456789",
            ToAccountName        = "John Doe",
            AmountToDebit        = 750000,
            DestinationBankCode  = "000013",
            TransactionReference = "APT00015260424FT0099",
            NameEnquiryID        = "NE-XYZ",
            Narration            = "School fees",
            BeneficiaryKYCLevel  = 2,
            BeneficiaryBVN       = "11122233344",
            OriginatorBVN        = "99988877766"
        });

        Assert.Equal("0123456789",           captured!.BeneficiaryAccountNumber);
        Assert.Equal("John Doe",             captured.BeneficiaryAccountName);
        Assert.Equal("750000",               captured.Amount);
        Assert.Equal("000013",               captured.DestinationInstitutionCode);
        Assert.Equal("APT00015260424FT0099", captured.UniqueReference);
        Assert.Equal("APT00015260424FT0099", captured.PaymentReference);
        Assert.Equal("NE-XYZ",              captured.NameEnquiryReference);
        Assert.Equal("School fees",          captured.Narration);
        Assert.Equal("2",                    captured.BeneficiaryKycLevel);
        Assert.Equal("11122233344",          captured.BeneficiaryBankVerificationNumber);
        Assert.Equal("99988877766",          captured.OriginatorBankVerificationNumber);
    }


    [Fact]
    public async Task IntraBankStatusAsync_HappyPath_ResponseCodeMapped()
    {
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.QueryTransactionStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new TransactionStatusQueryResponseDto { ResponseCode = "00" });

        var connector = BuildConnector(svc);

        var result = await connector.IntraBankStatusAsync(new TransactionStatusRequest
        {
            TransactionReference = "APT00015260424TSQ001"
        });

        Assert.Equal("00", result.ResponseCode);
        Assert.Equal("00", result.Data!.ResponseCode);
        Assert.Equal("00", result.Data.Status);
    }

    [Fact]
    public async Task IntraBankStatusAsync_PassesReferenceToService()
    {
        string? capturedRef = null;
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.QueryTransactionStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .Callback<string, CancellationToken>((r, _) => capturedRef = r)
           .ReturnsAsync(new TransactionStatusQueryResponseDto { ResponseCode = "00" });

        var connector = BuildConnector(svc);
        await connector.IntraBankStatusAsync(new TransactionStatusRequest
        {
            TransactionReference = "APT00015260424TSQ002"
        });

        Assert.Equal("APT00015260424TSQ002", capturedRef);
    }

    [Fact]
    public async Task IntraBankStatusAsync_PendingCode_MappedToData()
    {
        var svc = new Mock<IMonieTransferService>();
        svc.Setup(s => s.QueryTransactionStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new TransactionStatusQueryResponseDto { ResponseCode = "09" });

        var connector = BuildConnector(svc);
        var result = await connector.IntraBankStatusAsync(new TransactionStatusRequest
        {
            TransactionReference = "APT00015260424TSQ003"
        });

        Assert.Equal("09", result.ResponseCode);
        Assert.Equal("09", result.Data!.Status);
    }
}
