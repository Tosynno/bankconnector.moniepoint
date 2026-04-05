using zone.bankconnector.moniepoint.Dtos;

namespace zone.bankconnector.moniepoint.Tests;

public class TeamAptResponseCodesTests
{

    [Fact] public void Approved_Is_00()                    => Assert.Equal("00", TeamAptResponseCodes.Approved);
    [Fact] public void Pending_Is_09()                     => Assert.Equal("09", TeamAptResponseCodes.Pending);
    [Fact] public void Timeout_Is_97()                     => Assert.Equal("97", TeamAptResponseCodes.Timeout);
    [Fact] public void DoNotHonor_Is_05()                  => Assert.Equal("05", TeamAptResponseCodes.DoNotHonor);
    [Fact] public void DormantAccount_Is_06()              => Assert.Equal("06", TeamAptResponseCodes.DormantAccount);
    [Fact] public void InvalidAccount_Is_07()              => Assert.Equal("07", TeamAptResponseCodes.InvalidAccount);
    [Fact] public void AccountNameMismatch_Is_08()         => Assert.Equal("08", TeamAptResponseCodes.AccountNameMismatch);
    [Fact] public void InvalidTransaction_Is_12()          => Assert.Equal("12", TeamAptResponseCodes.InvalidTransaction);
    [Fact] public void InvalidAmount_Is_13()               => Assert.Equal("13", TeamAptResponseCodes.InvalidAmount);
    [Fact] public void InvalidUniqueReference_Is_15()      => Assert.Equal("15", TeamAptResponseCodes.InvalidUniqueReference);
    [Fact] public void UnknownBeneficiaryBankCode_Is_16()  => Assert.Equal("16", TeamAptResponseCodes.UnknownBeneficiaryBankCode);
    [Fact] public void UnableToLocateRecord_Is_25()        => Assert.Equal("25", TeamAptResponseCodes.UnableToLocateRecord);
    [Fact] public void DuplicateRecord_Is_26()             => Assert.Equal("26", TeamAptResponseCodes.DuplicateRecord);
    [Fact] public void FormatError_Is_30()                 => Assert.Equal("30", TeamAptResponseCodes.FormatError);
    [Fact] public void TransactionNotPermitted_Is_57()     => Assert.Equal("57", TeamAptResponseCodes.TransactionNotPermitted);
    [Fact] public void TransferLimitExceeded_Is_61()       => Assert.Equal("61", TeamAptResponseCodes.TransferLimitExceeded);
    [Fact] public void SecurityViolation_Is_63()           => Assert.Equal("63", TeamAptResponseCodes.SecurityViolation);
    [Fact] public void BeneficiaryBankNotAvailable_Is_91() => Assert.Equal("91", TeamAptResponseCodes.BeneficiaryBankNotAvailable);
    [Fact] public void RoutingError_Is_92()                => Assert.Equal("92", TeamAptResponseCodes.RoutingError);
    [Fact] public void SystemMalfunction_Is_96()           => Assert.Equal("96", TeamAptResponseCodes.SystemMalfunction);


    [Theory]
    [InlineData("00", "Approved")]
    [InlineData("09", "pending")]
    [InlineData("97", "Timeout")]
    [InlineData("05", "honor")]
    [InlineData("06", "Dormant")]
    [InlineData("07", "Invalid account")]
    [InlineData("08", "mismatch")]
    [InlineData("12", "Invalid transaction")]
    [InlineData("13", "Invalid amount")]
    [InlineData("15", "unique reference")]
    [InlineData("16", "bank code")]
    [InlineData("25", "locate")]
    [InlineData("26", "Duplicate")]
    [InlineData("30", "Format")]
    [InlineData("57", "permitted")]
    [InlineData("61", "limit")]
    [InlineData("63", "Security")]
    [InlineData("91", "not available")]
    [InlineData("92", "Routing")]
    [InlineData("96", "malfunction")]
    public void GetDescription_KnownCode_ContainsKeyword(string code, string keyword)
    {
        var desc = TeamAptResponseCodes.GetDescription(code);
        Assert.Contains(keyword, desc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetDescription_UnknownCode_ContainsCodeInMessage()
    {
        var desc = TeamAptResponseCodes.GetDescription("99");
        Assert.Contains("99", desc);
    }

    [Fact]
    public void GetDescription_EmptyCode_ReturnsUnknownMessage()
    {
        var desc = TeamAptResponseCodes.GetDescription("");
        Assert.NotEmpty(desc);
    }


    [Theory]
    [InlineData("09", false)]   
    [InlineData("97", false)]   
    public void IsFinal_NonFinalCodes_ReturnFalse(string code, bool expected)
    {
        Assert.Equal(expected, TeamAptResponseCodes.IsFinal(code));
    }

    [Theory]
    [InlineData("00", true)]
    [InlineData("05", true)]
    [InlineData("06", true)]
    [InlineData("07", true)]
    [InlineData("08", true)]
    [InlineData("12", true)]
    [InlineData("13", true)]
    [InlineData("25", true)]
    [InlineData("26", true)]
    [InlineData("91", true)]
    [InlineData("96", true)]
    public void IsFinal_FinalCodes_ReturnTrue(string code, bool expected)
    {
        Assert.Equal(expected, TeamAptResponseCodes.IsFinal(code));
    }
}
