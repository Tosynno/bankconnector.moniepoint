using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using bankconnector.moniepoint.Exceptions;
using bankconnector.moniepoint.Interfaces;
using bankconnector.moniepoint.Utilities;

namespace bankconnector.moniepoint.Encryption
{
    public sealed class RsaEncryptionService :
        IEncryptionService,
        IDisposable
    {
        private readonly RSA _teamAptPublicKey;

        private readonly RSA _institutionPrivateKey;

        private readonly bool _useWwwRootKeys;

        public EncryptionMode Mode =>
            EncryptionMode.RsaIso20022;

        private const int AesKeySize = 256;

        private const int AesBlockSize = 128;

        public RsaEncryptionService(
            string teamAptPublicKey,
            string institutionPrivateKey,
            string? privateKeyPassphrase = null,
            bool useWwwRootKeys = false)
        {
            _useWwwRootKeys = useWwwRootKeys;

            _teamAptPublicKey = _useWwwRootKeys
                ? LoadPublicKeyPemFromFile(teamAptPublicKey)
                : LoadPublicKeyPemFromString(
                    GetRequiredEnvironmentVariable(teamAptPublicKey));

            _institutionPrivateKey = _useWwwRootKeys
                ? LoadPrivateKeyPemFromFile(
                    institutionPrivateKey,
                    privateKeyPassphrase)
                : LoadPrivateKeyPemFromString(
                    GetRequiredEnvironmentVariable(institutionPrivateKey),
                    privateKeyPassphrase);
        }

        public string EncryptToHex(string plainJson)
        {
            try
            {
                return Convert.ToHexString(
                    HybridEncrypt(
                        Encoding.UTF8.GetBytes(plainJson),
                        _teamAptPublicKey));
            }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException(
                    "RSA/ISO20022: Failed to encrypt request payload.",
                    ex);
            }
        }

        public string DecryptFromHex(string hexCipherData)
        {
            try
            {
                return Encoding.UTF8.GetString(
                    HybridDecrypt(
                        Convert.FromHexString(
                            hexCipherData.Trim().TrimEnd(';')),
                        _institutionPrivateKey));
            }
            catch (TeamAptEncryptionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException(
                    "RSA/ISO20022: Failed to decrypt response payload.",
                    ex);
            }
        }


        private static byte[] HybridEncrypt(
            byte[] plainData,
            RSA rsaPublicKey)
        {
            using var aes = Aes.Create();

            aes.KeySize = AesKeySize;

            aes.BlockSize = AesBlockSize;

            aes.Mode = CipherMode.CBC;

            aes.Padding = PaddingMode.PKCS7;

            aes.GenerateKey();

            aes.GenerateIV();

            var encryptedKey =
                rsaPublicKey.Encrypt(
                    aes.Key,
                    RSAEncryptionPadding.OaepSHA256);

            using var msData =
                new MemoryStream();

            using var encryptor =
                aes.CreateEncryptor();

            using (var cs = new CryptoStream(
                       msData,
                       encryptor,
                       CryptoStreamMode.Write))
            {
                cs.Write(
                    plainData,
                    0,
                    plainData.Length);

                cs.FlushFinalBlock();
            }

            var aesCipher =
                msData.ToArray();

            using var result =
                new MemoryStream();

            var keyLen =
                (ushort)encryptedKey.Length;

            result.WriteByte((byte)(keyLen >> 8));

            result.WriteByte((byte)(keyLen & 0xFF));

            result.Write(
                encryptedKey,
                0,
                encryptedKey.Length);

            result.Write(
                aes.IV,
                0,
                aes.IV.Length);

            result.Write(
                aesCipher,
                0,
                aesCipher.Length);

            return result.ToArray();
        }

        private static byte[] HybridDecrypt(
            byte[] cipherData,
            RSA rsaPrivateKey)
        {
            using var reader =
                new BinaryReader(
                    new MemoryStream(cipherData));

            var keyLen =
                (reader.ReadByte() << 8) |
                reader.ReadByte();

            var aesKey =
                rsaPrivateKey.Decrypt(
                    reader.ReadBytes(keyLen),
                    RSAEncryptionPadding.OaepSHA256);

            var iv =
                reader.ReadBytes(16);

            var aesCipher =
                reader.ReadBytes(
                    (int)(reader.BaseStream.Length -
                          reader.BaseStream.Position));

            using var aes =
                Aes.Create();

            aes.KeySize = AesKeySize;

            aes.BlockSize = AesBlockSize;

            aes.Mode = CipherMode.CBC;

            aes.Padding = PaddingMode.PKCS7;

            aes.Key = aesKey;

            aes.IV = iv;

            using var msOut =
                new MemoryStream();

            using var decryptor =
                aes.CreateDecryptor();

            using (var cs = new CryptoStream(
                       msOut,
                       decryptor,
                       CryptoStreamMode.Write))
            {
                cs.Write(
                    aesCipher,
                    0,
                    aesCipher.Length);

                cs.FlushFinalBlock();
            }

            return msOut.ToArray();
        }


        private static string GetRequiredEnvironmentVariable(
            string variableName)
        {
            var value =
                Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new TeamAptEncryptionException(
                    $"Environment variable '{variableName}' was not found.");
            }

            return value;
        }


        private static RSA LoadPublicKeyPemFromFile(
            string pemPath)
        {
            var pem =
                File.ReadAllText(pemPath);

            return ParsePublicKeyPem(
                pem,
                pemPath);
        }

        private static RSA LoadPublicKeyPemFromString(
            string pemContent)
        {
            return ParsePublicKeyPem(
                pemContent,
                "environment variable");
        }

        private static RSA ParsePublicKeyPem(
            string pem,
            string source)
        {
            try
            {
                var rsa = RSA.Create();

                rsa.ImportFromPem(pem);

                return rsa;
            }
            catch
            {
                // Fallback to BouncyCastle
            }

            using var sr =
                new StringReader(pem);

            var keyObj =
                new PemReader(sr).ReadObject();

            var rsaParams =
                keyObj switch
                {
                    RsaKeyParameters kp => kp,

                    AsymmetricCipherKeyPair pair =>
                        (RsaKeyParameters)pair.Public,

                    _ => throw new TeamAptEncryptionException(
                        $"RSA: Unexpected PEM type " +
                        $"{keyObj?.GetType().Name} in {source}")
                };

            if (rsaParams.IsPrivate)
            {
                throw new TeamAptEncryptionException(
                    $"RSA: Expected public key " +
                    $"but got private key in {source}");
            }

            var rsa2 =
                RSA.Create();

            rsa2.ImportParameters(
                DotNetUtilities.ToRSAParameters(rsaParams));

            return rsa2;
        }

        private static RSA LoadPrivateKeyPemFromFile(
            string pemPath,
            string? passphrase)
        {
            var pem =
                File.ReadAllText(pemPath);

            return ParsePrivateKeyPem(
                pem,
                passphrase,
                pemPath);
        }

        private static RSA LoadPrivateKeyPemFromString(
            string pemContent,
            string? passphrase)
        {
            return ParsePrivateKeyPem(
                pemContent,
                passphrase,
                "environment variable");
        }

        private static RSA ParsePrivateKeyPem(
            string pem,
            string? passphrase,
            string source)
        {
            try
            {
                var rsa = RSA.Create();

                if (!string.IsNullOrWhiteSpace(passphrase))
                {
                    rsa.ImportFromEncryptedPem(
                        pem,
                        passphrase);
                }
                else
                {
                    rsa.ImportFromPem(pem);
                }

                return rsa;
            }
            catch
            {
                // Fallback to BouncyCastle
            }

            using var sr =
                new StringReader(pem);

            var finder =
                passphrase is not null
                    ? new PasswordFinder(passphrase)
                    : null;

            var keyObj =
                new PemReader(sr, finder)
                    .ReadObject();

            var rsaPrivate =
                keyObj switch
                {
                    RsaPrivateCrtKeyParameters kp => kp,

                    AsymmetricCipherKeyPair pair =>
                        (RsaPrivateCrtKeyParameters)pair.Private,

                    _ => throw new TeamAptEncryptionException(
                        $"RSA: Unexpected PEM type " +
                        $"{keyObj?.GetType().Name} in {source}")
                };

            var rsa2 =
                RSA.Create();

            rsa2.ImportParameters(
                DotNetUtilities.ToRSAParameters(rsaPrivate));

            return rsa2;
        }


        private sealed class PasswordFinder(string password)
            : IPasswordFinder
        {
            public char[] GetPassword() =>
                password.ToCharArray();
        }

        public void Dispose()
        {
            _teamAptPublicKey.Dispose();

            _institutionPrivateKey.Dispose();
        }
    }
}