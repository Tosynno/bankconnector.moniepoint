using Microsoft.Extensions.Options;
using zone.bankconnector.moniepoint.Exceptions;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Encryption
{
    public sealed class EncryptionServiceFactory
    {
        private readonly TeamAptOptions _options;
        private readonly ILogger<EncryptionServiceFactory> _logger;

        private IEncryptionService? _cached;
        private readonly object _lock = new();

        public EncryptionServiceFactory(
            IOptions<TeamAptOptions> options,
            ILogger<EncryptionServiceFactory> logger)
        {
            _options = options.Value;
            _logger  = logger;
        }

        public IEncryptionService GetService()
        {
            if (_cached is not null) return _cached;

            lock (_lock)
            {
                if (_cached is not null) return _cached;

                var isPgpFlag = _options.IsPgp?.Trim().ToUpperInvariant();

                _logger.LogInformation(
                    "TeamApt IsPgp flag = '{Flag}' → resolved mode = {Mode}",
                    _options.IsPgp ?? "(null)",
                    _options.EncryptionMode);

                _cached = _options.EncryptionMode switch
                {
                    EncryptionMode.LegacyPgp   => CreatePgpService(),
                    EncryptionMode.RsaIso20022 => CreateRsaService(),
                    _ => throw new TeamAptEncryptionException(
                        $"Unknown EncryptionMode: {_options.EncryptionMode}")
                };

                _logger.LogInformation(
                    "Encryption provider initialised: {Provider}",
                    _cached.GetType().Name);

                return _cached;
            }
        }

        private PgpEncryptionService CreatePgpService()
        {
            AssertKeyPath(_options.PgpTeamAptPublicKeyPath,
                "Credentials:PgpTeamAptPublicKeyPath",
                "Download TeamApt's PGP public key: portal → Encryption Keys → Public Key (Legacy PGP) → DOWNLOAD");

            AssertKeyPath(_options.PgpInstitutionPrivateKeyPath,
                "Credentials:PgpInstitutionPrivateKeyPath",
                "Your institution's PGP private key. Upload matching public key via portal → UPLOAD YOUR PUBLIC PGP KEY");

            return new PgpEncryptionService(
                _options.PgpTeamAptPublicKeyPath!,
                _options.PgpInstitutionPrivateKeyPath!,
                _options.PgpInstitutionPrivateKeyPassphrase);
        }

        private RsaEncryptionService CreateRsaService()
        {
            AssertKeyPath(_options.RsaTeamAptPublicKeyPath,
                "Credentials:RsaTeamAptPublicKeyPath",
                "Download TeamApt's RSA public key: portal → Encryption Keys → RSA Public Key (for ISO20022) → DOWNLOAD");

            AssertKeyPath(_options.RsaInstitutionPrivateKeyPath,
                "Credentials:RsaInstitutionPrivateKeyPath",
                "Your institution's RSA private key (PEM). Upload matching public key via portal → UPLOAD YOUR PUBLIC RSA KEY");

            return new RsaEncryptionService(
                _options.RsaTeamAptPublicKeyPath!,
                _options.RsaInstitutionPrivateKeyPath!,
                _options.RsaInstitutionPrivateKeyPassphrase);
        }

        private static void AssertKeyPath(string? path, string setting, string hint)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new TeamAptEncryptionException(
                    $"Missing config '{setting}'. Hint: {hint}");

            if (!File.Exists(path))
                throw new TeamAptEncryptionException(
                    $"Key file not found at '{setting}': {path}");
        }
    }
}
