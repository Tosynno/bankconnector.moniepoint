namespace zone.bankconnector.moniepoint.Utilities
{
    public enum EncryptionMode
    {
        LegacyPgp   = 0,
        RsaIso20022 = 1
    }

    public class TeamAptOptions
    {
        public const string SectionName = "Credentials";

        public string BaseUrl { get; set; } =
            "https://aptpay-account-transfer.switch-staging.teamapt.com/eft/v1";

        public string ApiKey { get; set; } = string.Empty;

        public string UniqueReferencePrefix { get; set; } = string.Empty;

        public string? IsPgp { get; set; }

        public EncryptionMode EncryptionMode =>
            IsPgp?.Trim().ToUpperInvariant() == "Y"
                ? EncryptionMode.LegacyPgp
                : EncryptionMode.RsaIso20022;

        public bool UseWwwRootKeys { get; set; } = false;

        public string? PgpTeamAptPublicKeyPath           { get; set; }
        public string? PgpInstitutionPrivateKeyPath       { get; set; }
        public string? PgpInstitutionPrivateKeyPassphrase { get; set; }

        public string? RsaTeamAptPublicKeyPath           { get; set; }
        public string? RsaInstitutionPrivateKeyPath       { get; set; }
        public string? RsaInstitutionPrivateKeyPassphrase { get; set; }

        public int TimeoutSeconds       { get; set; } = 30;
        public int ReQueryDelaySeconds  { get; set; } = 10;
        public int MaxReQueryAttempts   { get; set; } = 30;

        public string? GLAccountName   { get; set; }
        public string? GLAccountNumber { get; set; }
        public string? GLKycLevel      { get; set; }

        public string? ResolveKeyPath(string? configuredPath, string wwwRootPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return configuredPath;

            if (!UseWwwRootKeys || Path.IsPathRooted(configuredPath))
                return configuredPath;

            return Path.Combine(wwwRootPath, configuredPath.TrimStart('/').TrimStart('\\'));
        }
    }
}
