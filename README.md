Moniepoint FT Integration — Full Configuration & Security
Guide
This guide documents all required configuration for the Moniepoint / TeamApt FT integration, including:
• PGP configuration
• RSA configuration (legacy/optional)
• Environment-variable secret management
• appsettings.json structure
• Docker deployment examples
• Security hardening recommendations
1. Recommended appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "HttpClient": {
    "TimeoutSeconds": 40
  },
  "Credentials": {
    "BaseUrl":
      "https://aptpay-account-transfer.switch-staging.teamapt.com/eft/v1",
    "ApiKey": "",
    "UniqueReferencePrefix": "APT00015",
    "UseWwwRootKeys": false,
    "TimeoutSeconds": 30,
    "ReQueryDelaySeconds": 10,
    "MaxReQueryAttempts": 36,
    "GLAccountName": "Simulator",
    "GLAccountNumber": "11111111111",
    "GLKycLevel": "1",
    // =====================================================
    // PGP CONFIGURATION
    // =====================================================
    "PgpTeamAptPublicKeyPath":
      "MONIEPOINT_PGP_PUBLIC_KEY",
    "PgpInstitutionPrivateKeyPath":
      "MONIEPOINT_PGP_PRIVATE_KEY",
    "PgpInstitutionPrivateKeyPassphrase": "",
    // =====================================================
    // RSA CONFIGURATION (OPTIONAL / LEGACY)
    // =====================================================
    "RsaTeamAptPublicKeyPath":
      "MONIEPOINT_RSA_PUBLIC_KEY",
    "RsaInstitutionPrivateKeyPath":
      "MONIEPOINT_RSA_PRIVATE_KEY",
    "RsaInstitutionPrivateKeyPassphrase": ""
  }
}
2. Environment Variables
# =========================================================
# API KEY
# =========================================================
Credentials__ApiKey=xxxxxxxxxxxxxxxx
# =========================================================
# PGP KEYS
# =========================================================
MONIEPOINT_PGP_PUBLIC_KEY=-----BEGIN PGP PUBLIC KEY BLOCK----
...-----END PGP PUBLIC KEY BLOCK----
MONIEPOINT_PGP_PRIVATE_KEY=-----BEGIN PGP PRIVATE KEY BLOCK----
...-----END PGP PRIVATE KEY BLOCK----
Credentials__PgpInstitutionPrivateKeyPassphrase=xxxxxxxx
# =========================================================
# RSA KEYS (OPTIONAL / LEGACY)
# =========================================================
MONIEPOINT_RSA_PUBLIC_KEY=-----BEGIN PUBLIC KEY----
...-----END PUBLIC KEY----
MONIEPOINT_RSA_PRIVATE_KEY=-----BEGIN ENCRYPTED PRIVATE KEY----
...-----END ENCRYPTED PRIVATE KEY----
Credentials__RsaInstitutionPrivateKeyPassphrase=xxxxxxxx
3. Configuration Explanation
Setting
Purpose
UseWwwRootKeys
PgpTeamAptPublicKeyPath
false = read keys from environment variables
Environment variable name holding TeamApt PGP public key
PgpInstitutionPrivateKeyPath
RsaTeamAptPublicKeyPath
RsaInstitutionPrivateKeyPath
TimeoutSeconds
ReQueryDelaySeconds
MaxReQueryAttempts
UniqueReferencePrefix
Environment variable name holding institution PGP private key
Environment variable name holding TeamApt RSA public key
Environment variable name holding institution RSA private key
HTTP timeout for TeamApt requests
Delay between status polls
36 attempts × 10 seconds = 6-minute SLA
Transaction reference prefix
TeamApt API authentication key
ApiKey
4. Docker Deployment Example
docker run -d \
  -e Credentials__ApiKey="xxxxxxxx" \
  -e MONIEPOINT_PGP_PUBLIC_KEY="$(cat pgp-public.asc)" \
  -e MONIEPOINT_PGP_PRIVATE_KEY="$(cat pgp-private.asc)" \
  -e Credentials__PgpInstitutionPrivateKeyPassphrase="xxxxxxxx" \
  -e MONIEPOINT_RSA_PUBLIC_KEY="$(cat rsa-public.pem)" \
  -e MONIEPOINT_RSA_PRIVATE_KEY="$(cat rsa-private.pem)" \
  -e Credentials__RsaInstitutionPrivateKeyPassphrase="xxxxxxxx" \
  moniepoint-ftintegration
5. Security Recommendations
Issue
Recommendation
Keys in wwwroot
Secrets in appsettings.json
Retries on transfers
Do not store keys under wwwroot or static folders
Use environment variables or a secrets manager
Disable retries for /ft to avoid duplicate debits
PII logging
Time handling
Random jitter
Never log full account numbers or decrypted payloads
Use DateTime.UtcNow instead of DateTime.Now
Use RandomNumberGenerator instead of Random.Shared
