using Microsoft.Extensions.Options;
using zone.bankconnector.moniepoint.Exceptions;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Encryption
{
    public class EncryptionServiceFactory : IEncryptionServiceFactory
    {
        private readonly TeamAptOptions _options;
        private readonly ILogger<EncryptionServiceFactory> _logger;
        private readonly string _wwwRootPath;

        private IEncryptionService? _cached;
        private readonly object _lock = new();

        public EncryptionServiceFactory(
            IOptions<TeamAptOptions> options,
            ILogger<EncryptionServiceFactory> logger,
            IWebHostEnvironment env)
        {
            _options     = options.Value;
            _logger      = logger;
            _wwwRootPath = env.WebRootPath;
        }

        public virtual IEncryptionService GetService()
        {
            if (_cached is not null) return _cached;

            lock (_lock)
            {
                if (_cached is not null) return _cached;

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
            var pubPath  = _options.ResolveKeyPath(_options.PgpTeamAptPublicKeyPath,  _wwwRootPath);
            var privPath = _options.ResolveKeyPath(_options.PgpInstitutionPrivateKeyPath, _wwwRootPath);

            AssertKeyPath(pubPath,  "Credentials:PgpTeamAptPublicKeyPath");
            AssertKeyPath(privPath, "Credentials:PgpInstitutionPrivateKeyPath");

            return new PgpEncryptionService(
                pubPath!,
                privPath!,
                _options.PgpInstitutionPrivateKeyPassphrase, _options.UseWwwRootKeys);
        }

        private RsaEncryptionService CreateRsaService()
        {
            var pubPath  = _options.ResolveKeyPath(_options.RsaTeamAptPublicKeyPath,  _wwwRootPath);
            var privPath = _options.ResolveKeyPath(_options.RsaInstitutionPrivateKeyPath, _wwwRootPath);

            AssertKeyPath(pubPath,  "Credentials:RsaTeamAptPublicKeyPath");
            AssertKeyPath(privPath, "Credentials:RsaInstitutionPrivateKeyPath");

            return new RsaEncryptionService(
                pubPath!,
                privPath!,
                _options.RsaInstitutionPrivateKeyPassphrase, _options.UseWwwRootKeys);
        }

        private static void AssertKeyPath(string? path, string setting)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new TeamAptEncryptionException(
                    $"Missing config '{setting}'.");

            if (!File.Exists(path))
                throw new TeamAptEncryptionException(
                    $"Key file not found for '{setting}': {path}");
        }
    }
}
