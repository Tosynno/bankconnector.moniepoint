using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using zone.bankconnector.moniepoint.Exceptions;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Encryption
{
    public sealed class RsaEncryptionService : IEncryptionService, IDisposable
    {
        private readonly RSA _teamAptPublicKey;
        private readonly RSA _institutionPrivateKey;

        public EncryptionMode Mode => EncryptionMode.RsaIso20022;

        private const int AesKeySize   = 256; 
        private const int AesBlockSize = 128; 

        public RsaEncryptionService(
            string teamAptPublicKeyPath,
            string institutionPrivateKeyPath,
            string? privateKeyPassphrase = null)
        {
            _teamAptPublicKey      = LoadPublicKeyPem(teamAptPublicKeyPath);
            _institutionPrivateKey = LoadPrivateKeyPem(institutionPrivateKeyPath, privateKeyPassphrase);
        }


        public string EncryptToHex(string plainJson)
        {
            try
            {
                return Convert.ToHexString(
                    HybridEncrypt(Encoding.UTF8.GetBytes(plainJson), _teamAptPublicKey));
            }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException(
                    "RSA/ISO20022: Failed to encrypt request payload.", ex);
            }
        }

        public string DecryptFromHex(string hexCipherData)
        {
            try
            {
                return Encoding.UTF8.GetString(
                    HybridDecrypt(
                        Convert.FromHexString(hexCipherData.Trim().TrimEnd(';')),
                        _institutionPrivateKey));
            }
            catch (TeamAptEncryptionException) { throw; }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException(
                    "RSA/ISO20022: Failed to decrypt response payload.", ex);
            }
        }


        private static byte[] HybridEncrypt(byte[] plainData, RSA rsaPublicKey)
        {
            using var aes   = Aes.Create();
            aes.KeySize     = AesKeySize;
            aes.BlockSize   = AesBlockSize;
            aes.Mode        = CipherMode.CBC;
            aes.Padding     = PaddingMode.PKCS7;
            aes.GenerateKey();
            aes.GenerateIV();

            var encryptedKey = rsaPublicKey.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            using var msData   = new MemoryStream();
            using var encryptor = aes.CreateEncryptor();
            using (var cs = new CryptoStream(msData, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainData, 0, plainData.Length);
                cs.FlushFinalBlock();
            }
            var aesCipher = msData.ToArray();

            using var result = new MemoryStream();
            var keyLen = (ushort)encryptedKey.Length;
            result.WriteByte((byte)(keyLen >> 8));
            result.WriteByte((byte)(keyLen & 0xFF));
            result.Write(encryptedKey, 0, encryptedKey.Length);
            result.Write(aes.IV,       0, aes.IV.Length);
            result.Write(aesCipher,    0, aesCipher.Length);
            return result.ToArray();
        }

        private static byte[] HybridDecrypt(byte[] cipherData, RSA rsaPrivateKey)
        {
            using var reader = new BinaryReader(new MemoryStream(cipherData));

            var keyLen = (reader.ReadByte() << 8) | reader.ReadByte();

            var aesKey = rsaPrivateKey.Decrypt(
                reader.ReadBytes(keyLen), RSAEncryptionPadding.OaepSHA256);

            var iv           = reader.ReadBytes(16);
            var aesCipher    = reader.ReadBytes(
                (int)(reader.BaseStream.Length - reader.BaseStream.Position));

            using var aes    = Aes.Create();
            aes.KeySize      = AesKeySize;
            aes.BlockSize    = AesBlockSize;
            aes.Mode         = CipherMode.CBC;
            aes.Padding      = PaddingMode.PKCS7;
            aes.Key          = aesKey;
            aes.IV           = iv;

            using var msOut    = new MemoryStream();
            using var decryptor = aes.CreateDecryptor();
            using (var cs = new CryptoStream(msOut, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(aesCipher, 0, aesCipher.Length);
                cs.FlushFinalBlock();
            }
            return msOut.ToArray();
        }


        private static RSA LoadPublicKeyPem(string pemPath)
        {
            var pem = File.ReadAllText(pemPath);

            try
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                return rsa;
            }
            catch {}

            using var sr   = new StringReader(pem);
            var keyObj     = new PemReader(sr).ReadObject();

            var rsaParams = keyObj switch
            {
                RsaKeyParameters kp                  => kp,
                AsymmetricCipherKeyPair pair         => (RsaKeyParameters)pair.Public,
                _ => throw new TeamAptEncryptionException(
                    $"RSA: Unexpected PEM type {keyObj?.GetType().Name} in {pemPath}")
            };

            if (rsaParams.IsPrivate)
                throw new TeamAptEncryptionException(
                    $"RSA: Expected public key but got private key in {pemPath}");

            var rsa2 = RSA.Create();
            rsa2.ImportParameters(DotNetUtilities.ToRSAParameters(rsaParams));
            return rsa2;
        }

        private static RSA LoadPrivateKeyPem(string pemPath, string? passphrase)
        {
            var pem = File.ReadAllText(pemPath);

            try
            {
                var rsa = RSA.Create();
                if (!string.IsNullOrEmpty(passphrase))
                    rsa.ImportFromEncryptedPem(pem, passphrase);
                else
                    rsa.ImportFromPem(pem);
                return rsa;
            }
            catch { }

            using var sr  = new StringReader(pem);
            var finder    = passphrase is not null ? new PasswordFinder(passphrase) : null;
            var keyObj    = new PemReader(sr, finder).ReadObject();

            var rsaPrivate = keyObj switch
            {
                RsaPrivateCrtKeyParameters kp => kp,
                AsymmetricCipherKeyPair pair  => (RsaPrivateCrtKeyParameters)pair.Private,
                _ => throw new TeamAptEncryptionException(
                    $"RSA: Unexpected PEM type {keyObj?.GetType().Name} in {pemPath}")
            };

            var rsa2 = RSA.Create();
            rsa2.ImportParameters(DotNetUtilities.ToRSAParameters(rsaPrivate));
            return rsa2;
        }

        private sealed class PasswordFinder(string password) : IPasswordFinder
        {
            public char[] GetPassword() => password.ToCharArray();
        }

        public void Dispose()
        {
            _teamAptPublicKey.Dispose();
            _institutionPrivateKey.Dispose();
        }
    }
}
