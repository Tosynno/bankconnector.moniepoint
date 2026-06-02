using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Tests;

public class TeamAptOptionsTests
{
    private const string WwwRoot = "/app/wwwroot";


    [Theory]
    [InlineData("Y")]
    [InlineData("y")]
    [InlineData(" Y ")]
    [InlineData(" y ")]
    public void EncryptionMode_IsPgpY_CaseInsensitive_ReturnsLegacyPgp(string value)
    {
        Assert.Equal(EncryptionMode.LegacyPgp, new TeamAptOptions { IsPgp = value }.EncryptionMode);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("n")]
    [InlineData("NO")]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("  ")]
    public void EncryptionMode_IsPgpNotY_ReturnsRsaIso20022(string value)
    {
        Assert.Equal(EncryptionMode.RsaIso20022, new TeamAptOptions { IsPgp = value }.EncryptionMode);
    }

    [Fact]
    public void EncryptionMode_IsPgpNull_ReturnsRsaIso20022()
    {
        Assert.Equal(EncryptionMode.RsaIso20022, new TeamAptOptions { IsPgp = null }.EncryptionMode);
    }


    [Fact]
    public void UseWwwRootKeys_DefaultIsFalse()
    {
        Assert.False(new TeamAptOptions().UseWwwRootKeys);
    }


    [Fact]
    public void ResolveKeyPath_NullPath_ReturnsNull()
    {
        Assert.Null(new TeamAptOptions { UseWwwRootKeys = true }.ResolveKeyPath(null, WwwRoot));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveKeyPath_WhitespacePath_ReturnsAsIs(string path)
    {
        Assert.Equal(path, new TeamAptOptions { UseWwwRootKeys = true }.ResolveKeyPath(path, WwwRoot));
    }


    //[Theory]
    //[InlineData("/keys/pub.pem")]
    //[InlineData("keys/pub.pem")]
    //[InlineData("pub.pem")]
    //public void ResolveKeyPath_UseWwwRootFalse_AnyPath_ReturnsUnchanged(string path)
    //{
    //    Assert.Equal(path, new TeamAptOptions { UseWwwRootKeys = false }.ResolveKeyPath(path, WwwRoot));
    //}


    //[Theory]
    //[InlineData("/keys/pub.pem")]
    //[InlineData("/etc/secrets/rsa.pem")]
    //public void ResolveKeyPath_UseWwwRootTrue_AbsolutePath_NotRerouted(string path)
    //{
    //    Assert.Equal(path, new TeamAptOptions { UseWwwRootKeys = true }.ResolveKeyPath(path, WwwRoot));
    //}

    [Fact]
    public void ResolveKeyPath_UseWwwRootTrue_RelativePath_CombinesWithWwwRoot()
    {
        var result = new TeamAptOptions { UseWwwRootKeys = true }
            .ResolveKeyPath("keys/pub.pem", WwwRoot);
        Assert.Equal(Path.Combine(WwwRoot, "keys/pub.pem"), result);
    }

    [Fact]
    public void ResolveKeyPath_UseWwwRootTrue_DeepRelativePath_CombinesCorrectly()
    {
        var result = new TeamAptOptions { UseWwwRootKeys = true }
            .ResolveKeyPath("certs/pgp/pub.asc", WwwRoot);
        Assert.Equal(Path.Combine(WwwRoot, "certs/pgp/pub.asc"), result);
    }


    [Fact]
    public void DefaultBaseUrl_ContainsTeamAptDomain()
    {
        Assert.Contains("teamapt.com", new TeamAptOptions().BaseUrl);
    }

    [Fact]
    public void DefaultReQueryDelaySeconds_IsTen()
    {
        Assert.Equal(10, new TeamAptOptions().ReQueryDelaySeconds);
    }

    [Fact]
    public void DefaultMaxReQueryAttempts_IsThirty()
    {
        Assert.Equal(30, new TeamAptOptions().MaxReQueryAttempts);
    }

    [Fact]
    public void DefaultTimeoutSeconds_IsThirty()
    {
        Assert.Equal(30, new TeamAptOptions().TimeoutSeconds);
    }

    [Fact]
    public void DefaultApiKey_IsEmptyString()
    {
        Assert.Equal(string.Empty, new TeamAptOptions().ApiKey);
    }

    [Fact]
    public void DefaultUniqueReferencePrefix_IsEmptyString()
    {
        Assert.Equal(string.Empty, new TeamAptOptions().UniqueReferencePrefix);
    }
}
