using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using zone.bankconnector.moniepoint.Encryption;
using zone.bankconnector.moniepoint.Https;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Tests.Fixtures;

internal static class MockFactory
{

    public static TeamAptOptions DefaultRsaOptions() => new()
    {
        IsPgp                        = "N",
        UseWwwRootKeys               = false,
        UniqueReferencePrefix        = "APT00015",
        RsaTeamAptPublicKeyPath      = "/keys/teamapt-rsa-public.pem",
        RsaInstitutionPrivateKeyPath = "/keys/institution-rsa-private.pem",
        MaxReQueryAttempts           = 3,
        ReQueryDelaySeconds          = 0,
        GLAccountName                = "ZoneGL",
        GLAccountNumber              = "0000000001",
        GLKycLevel                   = "1"
    };

    public static TeamAptOptions DefaultPgpOptions() => new()
    {
        IsPgp                        = "Y",
        UseWwwRootKeys               = false,
        UniqueReferencePrefix        = "APT00015",
        PgpTeamAptPublicKeyPath      = "/keys/teamapt-pgp-public.asc",
        PgpInstitutionPrivateKeyPath = "/keys/institution-pgp-private.asc",
        MaxReQueryAttempts           = 3,
        ReQueryDelaySeconds          = 0
    };

    public static Mock<IEncryptionService> PassthroughEncryption()
    {
        var mock = new Mock<IEncryptionService>();
        mock.Setup(e => e.Mode).Returns(EncryptionMode.RsaIso20022);
        mock.Setup(e => e.EncryptToHex(It.IsAny<string>()))
            .Returns<string>(p => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(p)));
        mock.Setup(e => e.DecryptFromHex(It.IsAny<string>()))
            .Returns<string>(h => System.Text.Encoding.UTF8.GetString(Convert.FromHexString(h)));
        return mock;
    }


    public static Mock<IEncryptionServiceFactory> EncryptionFactory(IEncryptionService svc)
    {
        var factory = new Mock<IEncryptionServiceFactory>();
        factory.Setup(f => f.GetService()).Returns(svc);
        return factory;
    }


    public static EncryptionServiceFactory RealFactory(TeamAptOptions opts, string? wwwRoot = null)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(wwwRoot ?? "/app/wwwroot");
        return new EncryptionServiceFactory(
            Microsoft.Extensions.Options.Options.Create(opts),
            NullLogger<EncryptionServiceFactory>.Instance,
            env.Object);
    }


    public static Mock<IHttpClientService> Http() => new();
}
