using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using zone.bankconnector.moniepoint.Encryption;
using zone.bankconnector.moniepoint.Exceptions;
using zone.bankconnector.moniepoint.Tests.Fixtures;
using zone.bankconnector.moniepoint.Utilities;
using MockFactory = zone.bankconnector.moniepoint.Tests.Fixtures.MockFactory;

namespace zone.bankconnector.moniepoint.Tests;

public class EncryptionServiceFactoryTests : IDisposable
{
    private readonly string _tmp;

    public EncryptionServiceFactoryTests()
    {
        _tmp = Path.Combine(Path.GetTempPath(), $"enc_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tmp);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmp)) Directory.Delete(_tmp, recursive: true);
    }

    [Fact]
    public void GetService_Rsa_NullPublicKeyPath_ThrowsWithSettingName()
    {
        var opts = MockFactory.DefaultRsaOptions();
        opts.RsaTeamAptPublicKeyPath = null;
        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts).GetService());
        Assert.Contains("RsaTeamAptPublicKeyPath", ex.Message);
    }

    [Fact]
    public void GetService_Rsa_WhitespacePublicKeyPath_ThrowsWithSettingName()
    {
        var opts = MockFactory.DefaultRsaOptions();
        opts.RsaTeamAptPublicKeyPath = "   ";
        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts).GetService());
        Assert.Contains("RsaTeamAptPublicKeyPath", ex.Message);
    }

    [Fact]
    public void GetService_Rsa_FileNotFound_ThrowsWithPath()
    {
        var opts = MockFactory.DefaultRsaOptions();
        opts.RsaTeamAptPublicKeyPath      = "/nonexistent/pub.pem";
        opts.RsaInstitutionPrivateKeyPath = "/nonexistent/priv.pem";
        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts).GetService());
        Assert.Contains("/nonexistent/pub.pem", ex.Message);
    }


    [Fact]
    public void GetService_Pgp_NullPublicKeyPath_ThrowsWithSettingName()
    {
        var opts = MockFactory.DefaultPgpOptions();
        opts.PgpTeamAptPublicKeyPath = null;
        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts).GetService());
        Assert.Contains("PgpTeamAptPublicKeyPath", ex.Message);
    }

   

    [Fact]
    public void GetService_Pgp_FileNotFound_ThrowsWithPath()
    {
        var opts = MockFactory.DefaultPgpOptions();
        opts.PgpTeamAptPublicKeyPath      = "/nonexistent/pub.asc";
        opts.PgpInstitutionPrivateKeyPath = "/nonexistent/priv.asc";
        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts).GetService());
        Assert.Contains("/nonexistent/pub.asc", ex.Message);
    }


    [Fact]
    public void GetService_UseWwwRootKeys_RelativePath_FilesExistUnderWwwRoot_PassesAssert()
    {
        var keysDir = Path.Combine(_tmp, "keys");
        Directory.CreateDirectory(keysDir);
        File.WriteAllText(Path.Combine(keysDir, "pub.pem"),  "dummy");
        File.WriteAllText(Path.Combine(keysDir, "priv.pem"), "dummy");

        var opts = MockFactory.DefaultRsaOptions();
        opts.UseWwwRootKeys               = true;
        opts.RsaTeamAptPublicKeyPath      = "keys/pub.pem";
        opts.RsaInstitutionPrivateKeyPath = "keys/priv.pem";

        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts, _tmp).GetService());
        Assert.DoesNotContain("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    //[Fact]
    //public void GetService_UseWwwRootKeys_AbsolutePath_BypassesWwwRoot()
    //{
    //    var opts = MockFactory.DefaultRsaOptions();
    //    opts.UseWwwRootKeys               = true;
    //    opts.RsaTeamAptPublicKeyPath      = "/keys/institution-rsa-private.pem";
    //    opts.RsaInstitutionPrivateKeyPath = "/keys/institution-rsa-private.pem";
    //    var ex = Assert.Throws<TeamAptEncryptionException>(() =>
    //        MockFactory.RealFactory(opts, _tmp).GetService());
    //    Assert.Contains("/absolute/pub.pem", ex.Message);
    //}

    [Fact]
    public void GetService_UseWwwRootKeysFalse_RelativePath_NotResolvedToWwwRoot()
    {
        var opts = MockFactory.DefaultRsaOptions();
        opts.UseWwwRootKeys               = false;
        opts.RsaTeamAptPublicKeyPath      = "keys/pub.pem";
        opts.RsaInstitutionPrivateKeyPath = "keys/priv.pem";
        var ex = Assert.Throws<TeamAptEncryptionException>(() =>
            MockFactory.RealFactory(opts, _tmp).GetService());
        Assert.Contains("keys/pub.pem", ex.Message);
    }


    [Fact]
    public void EncryptionServiceFactory_ImplementsIEncryptionServiceFactory()
    {
        var opts = MockFactory.DefaultRsaOptions();
        var factory = MockFactory.RealFactory(opts);
        Assert.IsAssignableFrom<IEncryptionServiceFactory>(factory);
    }
}
