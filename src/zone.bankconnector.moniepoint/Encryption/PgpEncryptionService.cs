using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using zone.bankconnector.moniepoint.Exceptions;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Encryption
{
    public sealed class PgpEncryptionService : IEncryptionService
    {
        private readonly PgpPublicKey  _teamAptPublicKey;
        private readonly PgpPrivateKey _institutionPrivateKey;

        public EncryptionMode Mode => EncryptionMode.LegacyPgp;

        public PgpEncryptionService(
            string teamAptPublicKeyPath,
            string institutionPrivateKeyPath,
            string? privateKeyPassphrase = null)
        {
            _teamAptPublicKey      = LoadPublicKey(teamAptPublicKeyPath);
            _institutionPrivateKey = LoadPrivateKey(institutionPrivateKeyPath, privateKeyPassphrase);
        }


        public string EncryptToHex(string plainJson)
        {
            try
            {
                return BytesToHex(PgpEncrypt(Encoding.UTF8.GetBytes(plainJson), _teamAptPublicKey));
            }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException("PGP: Failed to encrypt request payload.", ex);
            }
        }

        public string DecryptFromHex(string hexCipherData)
        {
            try
            {
                return Encoding.UTF8.GetString(
                    PgpDecrypt(HexToBytes(hexCipherData), _institutionPrivateKey));
            }
            catch (TeamAptEncryptionException) { throw; }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException("PGP: Failed to decrypt response payload.", ex);
            }
        }


        private static byte[] PgpEncrypt(byte[] plainData, PgpPublicKey publicKey)
        {
            using var ms  = new MemoryStream();
            var encGen    = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Aes256, withIntegrityPacket: true, new SecureRandom());
            encGen.AddMethod(publicKey);

            using (var encOut = encGen.Open(ms, new byte[1 << 16]))
            {
                var compGen = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                using var compOut = compGen.Open(encOut);
                var litGen = new PgpLiteralDataGenerator();
                using var litOut = litGen.Open(compOut, PgpLiteralData.Binary,
                    PgpLiteralData.Console, plainData.Length, DateTime.UtcNow);
                litOut.Write(plainData, 0, plainData.Length);
            }
            return ms.ToArray();
        }

        private static byte[] PgpDecrypt(byte[] cipherData, PgpPrivateKey privateKey)
        {
            using var ms          = new MemoryStream(cipherData);
            using var decoderStream = PgpUtilities.GetDecoderStream(ms);

            var factory  = new PgpObjectFactory(decoderStream);
            var encList  = FindEncryptedDataList(factory);

            foreach (PgpPublicKeyEncryptedData enc in encList.GetEncryptedDataObjects())
            {
                if (enc.KeyId != privateKey.KeyId) continue;

                using var decStream = enc.GetDataStream(privateKey);
                var decFactory      = new PgpObjectFactory(decStream);
                var obj             = decFactory.NextPgpObject();

                if (obj is PgpCompressedData cd)
                    obj = new PgpObjectFactory(cd.GetDataStream()).NextPgpObject();

                if (obj is PgpLiteralData ld)
                {
                    using var litStream = ld.GetInputStream();
                    using var result    = new MemoryStream();
                    litStream.CopyTo(result);
                    return result.ToArray();
                }
            }
            throw new TeamAptEncryptionException(
                "PGP: No encrypted block matched the institution private key.");
        }

        private static PgpEncryptedDataList FindEncryptedDataList(PgpObjectFactory factory)
        {
            var obj = factory.NextPgpObject();
            if (obj is PgpEncryptedDataList list)  return list;
            obj = factory.NextPgpObject();
            if (obj is PgpEncryptedDataList list2) return list2;
            throw new TeamAptEncryptionException("PGP: No encrypted data list found in message.");
        }


        private static PgpPublicKey LoadPublicKey(string path)
        {
            using var fs     = File.OpenRead(path);
            using var decode = PgpUtilities.GetDecoderStream(fs);
            var bundle       = new PgpPublicKeyRingBundle(decode);

            foreach (PgpPublicKeyRing ring in bundle.GetKeyRings())
                foreach (PgpPublicKey key in ring.GetPublicKeys())
                    if (key.IsEncryptionKey) return key;

            throw new TeamAptEncryptionException(
                $"PGP: No encryption-capable public key found in: {path}");
        }

        private static PgpPrivateKey LoadPrivateKey(string path, string? passphrase)
        {
            using var fs     = File.OpenRead(path);
            using var decode = PgpUtilities.GetDecoderStream(fs);
            var bundle       = new PgpSecretKeyRingBundle(decode);

            foreach (PgpSecretKeyRing ring in bundle.GetKeyRings())
                foreach (PgpSecretKey secret in ring.GetSecretKeys())
                    if (!secret.IsSigningKey)
                        return secret.ExtractPrivateKey((passphrase ?? string.Empty).ToCharArray());

            throw new TeamAptEncryptionException(
                $"PGP: No suitable private key found in: {path}");
        }


        private static string BytesToHex(byte[] bytes) => Convert.ToHexString(bytes);

        private static byte[] HexToBytes(string hex) =>
            Convert.FromHexString(hex.Trim().TrimEnd(';'));
    }
}
