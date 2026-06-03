using bankconnector.moniepoint.Dtos;
using bankconnector.moniepoint.Models;

namespace bankconnector.moniepoint.Tests;

public class DtoTests
{

    [Fact]
    public void NameEnquiryResponseDto_IsSuccessful_Code00_True()
        => Assert.True(new NameEnquiryResponseDto { ResponseCode = "00" }.IsSuccessful);

    [Theory]
    [InlineData("05")]
    [InlineData("07")]
    [InlineData("09")]
    [InlineData("")]
    public void NameEnquiryResponseDto_IsSuccessful_NonApproved_False(string code)
        => Assert.False(new NameEnquiryResponseDto { ResponseCode = code }.IsSuccessful);


    [Fact]
    public void FundsTransferResponseDto_IsSuccessful_Code00_True()
        => Assert.True(new FundsTransferResponseDto { ResponseCode = "00" }.IsSuccessful);

    [Theory]
    [InlineData("09", true)]
    [InlineData("97", true)]
    public void FundsTransferResponseDto_IsPending_NonFinalCodes_True(string code, bool expected)
        => Assert.Equal(expected, new FundsTransferResponseDto { ResponseCode = code }.IsPending);

    [Theory]
    [InlineData("00", false)]
    [InlineData("05", false)]
    [InlineData("96", false)]
    public void FundsTransferResponseDto_IsPending_FinalCodes_False(string code, bool expected)
        => Assert.Equal(expected, new FundsTransferResponseDto { ResponseCode = code }.IsPending);


    [Theory]
    [InlineData("09", false)]
    [InlineData("97", false)]
    public void TsqResponseDto_IsFinal_PendingAndTimeout_False(string code, bool expectedFinal)
        => Assert.Equal(expectedFinal, new TransactionStatusQueryResponseDto { ResponseCode = code }.IsFinal);

    [Theory]
    [InlineData("00", true)]
    [InlineData("05", true)]
    [InlineData("91", true)]
    [InlineData("96", true)]
    public void TsqResponseDto_IsFinal_OtherCodes_True(string code, bool expectedFinal)
        => Assert.Equal(expectedFinal, new TransactionStatusQueryResponseDto { ResponseCode = code }.IsFinal);

    [Fact]
    public void TsqResponseDto_Code09_IsPendingTrue_IsFinalFalse()
    {
        var dto = new TransactionStatusQueryResponseDto { ResponseCode = "09" };
        Assert.True(dto.IsPending);
        Assert.False(dto.IsFinal);
    }

    [Fact]
    public void TsqResponseDto_Code97_IsPendingTrue_IsFinalFalse()
    {
        var dto = new TransactionStatusQueryResponseDto { ResponseCode = "97" };
        Assert.True(dto.IsPending);
        Assert.False(dto.IsFinal);
    }

    [Fact]
    public void TsqResponseDto_Code00_IsSuccessfulTrue_IsFinalTrue()
    {
        var dto = new TransactionStatusQueryResponseDto { ResponseCode = "00" };
        Assert.True(dto.IsSuccessful);
        Assert.True(dto.IsFinal);
    }


    [Fact]
    public void InstitutionBalanceResponseDto_IsSuccessful_Code00_True()
        => Assert.True(new InstitutionBalanceResponseDto { ResponseCode = "00" }.IsSuccessful);

    [Theory]
    [InlineData("50000.00", 50000.00)]
    [InlineData("0",        0)]
    [InlineData("1234567.89", 1234567.89)]
    public void InstitutionBalanceResponseDto_AmountDecimal_ParsesCorrectly(string raw, decimal expected)
        => Assert.Equal(expected, new InstitutionBalanceResponseDto { Amount = raw }.AmountDecimal);

    [Theory]
    [InlineData("")]
    [InlineData("N/A")]
    [InlineData("abc")]
    public void InstitutionBalanceResponseDto_AmountDecimal_InvalidAmount_ReturnsZero(string raw)
        => Assert.Equal(0m, new InstitutionBalanceResponseDto { Amount = raw }.AmountDecimal);


    [Fact]
    public void FundsTransferDto_AllPropertiesAssignable()
    {
        var dto = new FundsTransferDto
        {
            UniqueReference                  = "APT0001526042412345678",
            DestinationInstitutionCode       = "000013",
            Amount                           = "500000",
            NameEnquiryReference             = "NE-REF-001",
            BeneficiaryKycLevel              = "1",
            BeneficiaryBankVerificationNumber= "12345678901",
            OriginatorBankVerificationNumber = "98765432101",
            PaymentReference                 = "PAY-REF-001",
            Narration                        = "School fees",
            BeneficiaryAccountNumber         = "0123456789",
            BeneficiaryAccountName           = "John Doe",
            OriginatorAccountName            = "ZoneGL",
            OriginatorAccountNumber          = "0000000001",
            OriginatorKycLevel               = "1"
        };

        Assert.Equal("APT0001526042412345678", dto.UniqueReference);
        Assert.Equal("000013",                 dto.DestinationInstitutionCode);
        Assert.Equal("500000",                 dto.Amount);
        Assert.Equal("NE-REF-001",             dto.NameEnquiryReference);
        Assert.Equal("1",                      dto.BeneficiaryKycLevel);
        Assert.Equal("12345678901",            dto.BeneficiaryBankVerificationNumber);
        Assert.Equal("98765432101",            dto.OriginatorBankVerificationNumber);
        Assert.Equal("PAY-REF-001",            dto.PaymentReference);
        Assert.Equal("School fees",            dto.Narration);
        Assert.Equal("0123456789",             dto.BeneficiaryAccountNumber);
        Assert.Equal("John Doe",               dto.BeneficiaryAccountName);
        Assert.Equal("ZoneGL",                 dto.OriginatorAccountName);
        Assert.Equal("0000000001",             dto.OriginatorAccountNumber);
        Assert.Equal("1",                      dto.OriginatorKycLevel);
    }

    [Fact]
    public void GenericResponse_TransferResponse_DataAutoInitialised_NotNull()
        => Assert.NotNull(new GenericResponse<TransferResponse>().Data);

    [Fact]
    public void GenericResponse_TransactionStatusResponse_DataAutoInitialised_NotNull()
        => Assert.NotNull(new GenericResponse<TransactionStatusResponse>().Data);

    [Fact]
    public void GenericResponse_DataPropertiesAssignable()
    {
        var r = new GenericResponse<TransferResponse>
        {
            ResponseCode = "00",
            Data =
            {
                ResponseCode         = "00",
                TransactionReference = "PAY-001",
                Status               = "00"
            }
        };
        Assert.Equal("00",      r.ResponseCode);
        Assert.Equal("PAY-001", r.Data.TransactionReference);
        Assert.Equal("00",      r.Data.Status);
    }

    [Fact]
    public void GenericResponse_ResponseCodeAndMessage_InheritsFromBaseResponse()
    {
        var r = new GenericResponse<TransferResponse>
        {
            ResponseCode    = "07",
            ResponseMessage = "Invalid account"
        };
        Assert.Equal("07",              r.ResponseCode);
        Assert.Equal("Invalid account", r.ResponseMessage);
    }
}
