using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using System.Text;
using bankconnector.moniepoint.Exceptions;
using bankconnector.moniepoint.Interfaces;
using bankconnector.moniepoint.Utilities;

namespace bankconnector.moniepoint.Encryption
{
    public sealed class PgpEncryptionService : IEncryptionService
    {
        private readonly PgpPublicKey _teamAptPublicKey;

        private readonly PgpPrivateKey _institutionPrivateKey;

        private readonly bool _useWwwRootKeys;

        public EncryptionMode Mode => EncryptionMode.LegacyPgp;

        public PgpEncryptionService(
            string teamAptPublicKey,
            string institutionPrivateKey,
            string? privateKeyPassphrase = null,
            bool useWwwRootKeys = false)
        {
            _useWwwRootKeys = useWwwRootKeys;

            _teamAptPublicKey = _useWwwRootKeys
                ? LoadPublicKeyFromFile(teamAptPublicKey)
                : LoadPublicKeyFromString(
                    GetRequiredEnvironmentVariable(teamAptPublicKey));

            _institutionPrivateKey = _useWwwRootKeys
                ? LoadPrivateKeyFromFile(
                    institutionPrivateKey,
                    privateKeyPassphrase)
                : LoadPrivateKeyFromString(
                    GetRequiredEnvironmentVariable(institutionPrivateKey),
                    privateKeyPassphrase);
        }

        public string EncryptToHex(string plainJson)
        {
            try
            {
                return BytesToHex(
                    PgpEncrypt(
                        Encoding.UTF8.GetBytes(plainJson),
                        _teamAptPublicKey));
            }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException(
                    "PGP: Failed to encrypt request payload.",
                    ex);
            }
        }

        public string DecryptFromHex(string hexCipherData)
        {
            try
            {
                return Encoding.UTF8.GetString(
                    PgpDecrypt(
                        HexToBytes(hexCipherData),
                        _institutionPrivateKey));
            }
            catch (TeamAptEncryptionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TeamAptEncryptionException(
                    "PGP: Failed to decrypt response payload.",
                    ex);
            }
        }

        private static byte[] PgpEncrypt(
            byte[] plainData,
            PgpPublicKey publicKey)
        {
            using var ms = new MemoryStream();

            var encGen = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Aes256,
                withIntegrityPacket: true,
                new SecureRandom());

            encGen.AddMethod(publicKey);

            using (var encOut = encGen.Open(ms, new byte[1 << 16]))
            {
                var compGen =
                    new PgpCompressedDataGenerator(
                        CompressionAlgorithmTag.Zip);

                using var compOut = compGen.Open(encOut);

                var litGen = new PgpLiteralDataGenerator();

                using var litOut = litGen.Open(
                    compOut,
                    PgpLiteralData.Binary,
                    PgpLiteralData.Console,
                    plainData.Length,
                    DateTime.UtcNow);

                litOut.Write(plainData, 0, plainData.Length);
            }

            return ms.ToArray();
        }

        private static byte[] PgpDecrypt(
            byte[] cipherData,
            PgpPrivateKey privateKey)
        {
            using var ms =
                new MemoryStream(cipherData);

            using var decoderStream =
                PgpUtilities.GetDecoderStream(ms);

            var factory =
                new PgpObjectFactory(decoderStream);

            var encList =
                FindEncryptedDataList(factory);

            foreach (PgpPublicKeyEncryptedData enc
                     in encList.GetEncryptedDataObjects())
            {
                if (enc.KeyId != privateKey.KeyId)
                    continue;

                using var decStream =
                    enc.GetDataStream(privateKey);

                var decFactory =
                    new PgpObjectFactory(decStream);

                var obj =
                    decFactory.NextPgpObject();

                if (obj is PgpCompressedData cd)
                {
                    obj = new PgpObjectFactory(
                        cd.GetDataStream())
                        .NextPgpObject();
                }

                if (obj is PgpLiteralData ld)
                {
                    using var litStream =
                        ld.GetInputStream();

                    using var result =
                        new MemoryStream();

                    litStream.CopyTo(result);

                    if (enc.IsIntegrityProtected() &&
                        !enc.Verify())
                    {
                        throw new TeamAptEncryptionException(
                            "PGP integrity verification failed.");
                    }

                    return result.ToArray();
                }
            }

            throw new TeamAptEncryptionException(
                "PGP: No encrypted block matched the institution private key.");
        }

        private static PgpEncryptedDataList FindEncryptedDataList(
            PgpObjectFactory factory)
        {
            var obj = factory.NextPgpObject();

            if (obj is PgpEncryptedDataList list)
                return list;

            obj = factory.NextPgpObject();

            if (obj is PgpEncryptedDataList list2)
                return list2;

            throw new TeamAptEncryptionException(
                "PGP: No encrypted data list found in message.");
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


        private static PgpPublicKey LoadPublicKeyFromFile(
            string path)
        {
            using var fs = File.OpenRead(path);

            return ReadPublicKey(fs, path);
        }

        private static PgpPublicKey LoadPublicKeyFromString(
            string armoredKey)
        {
            var bytes =
                Encoding.UTF8.GetBytes(armoredKey);

            using var ms =
                new MemoryStream(bytes);

            return ReadPublicKey(ms, "environment variable");
        }

        private static PgpPublicKey ReadPublicKey(
            Stream input,
            string source)
        {
            using var decode =
                PgpUtilities.GetDecoderStream(input);

            var bundle =
                new PgpPublicKeyRingBundle(decode);

            foreach (PgpPublicKeyRing ring
                     in bundle.GetKeyRings())
            {
                foreach (PgpPublicKey key
                         in ring.GetPublicKeys())
                {
                    if (key.IsEncryptionKey)
                        return key;
                }
            }

            throw new TeamAptEncryptionException(
                $"PGP: No encryption-capable public key found in: {source}");
        }


        private static PgpPrivateKey LoadPrivateKeyFromFile(
            string path,
            string? passphrase)
        {
            using var fs = File.OpenRead(path);

            return ReadPrivateKey(fs, path, passphrase);
        }

        private static PgpPrivateKey LoadPrivateKeyFromString(
            string armoredKey,
            string? passphrase)
        {
            var bytes =
                Encoding.UTF8.GetBytes(armoredKey);

            using var ms =
                new MemoryStream(bytes);

            return ReadPrivateKey(
                ms,
                "environment variable",
                passphrase);
        }

        private static PgpPrivateKey ReadPrivateKey(
            Stream input,
            string source,
            string? passphrase)
        {
            using var decoder =
                PgpUtilities.GetDecoderStream(input);

            PgpSecretKeyRingBundle bundle;

            try
            {
                bundle =
                    new PgpSecretKeyRingBundle(decoder);
            }
            catch (IOException ex)
            {
                throw new TeamAptEncryptionException(
                    "Invalid or unsupported PGP key format. " +
                    "Ensure the key is RSA and not ECC/EdDSA.",
                    ex);
            }

            foreach (PgpSecretKeyRing ring
                     in bundle.GetKeyRings())
            {
                foreach (PgpSecretKey secret
                         in ring.GetSecretKeys())
                {
                    try
                    {
                        if (!secret.IsSigningKey)
                        {
                            var privateKey =
                                secret.ExtractPrivateKey(
                                    (passphrase ?? string.Empty)
                                    .ToCharArray());

                            if (privateKey != null)
                                return privateKey;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            throw new TeamAptEncryptionException(
                $"No suitable RSA private key found in: {source}");
        }


        private static string BytesToHex(byte[] bytes) =>
            Convert.ToHexString(bytes);

        private static byte[] HexToBytes(string hex) =>
            Convert.FromHexString(
                hex.Trim().TrimEnd(';'));


        public static void GeneratePgpKeyPair(
            string identity,
            string passphrase,
            string privateKeyPath,
            string publicKeyPath)
        {
            var random = new SecureRandom();

            var rsaGen = new RsaKeyPairGenerator();

            rsaGen.Init(
                new KeyGenerationParameters(
                    random,
                    2048));

            var masterKeyPair =
                rsaGen.GenerateKeyPair();

            var encKeyPair =
                rsaGen.GenerateKeyPair();

            var masterPgpKeyPair =
                new PgpKeyPair(
                    PublicKeyAlgorithmTag.RsaSign,
                    masterKeyPair,
                    DateTime.UtcNow);

            var encPgpKeyPair =
                new PgpKeyPair(
                    PublicKeyAlgorithmTag.RsaEncrypt,
                    encKeyPair,
                    DateTime.UtcNow);

            var keyRingGenerator =
                new PgpKeyRingGenerator(
                    PgpSignature.DefaultCertification,
                    masterPgpKeyPair,
                    identity,
                    SymmetricKeyAlgorithmTag.Aes256,
                    passphrase.ToCharArray(),
                    true,
                    null,
                    null,
                    random);

            keyRingGenerator.AddSubKey(encPgpKeyPair);

            var secretRing =
                keyRingGenerator.GenerateSecretKeyRing();

            var publicRing =
                keyRingGenerator.GeneratePublicKeyRing();

            using (var fs = File.Create(privateKeyPath))
            using (var armored = new ArmoredOutputStream(fs))
            {
                secretRing.Encode(armored);
            }

            using (var fs = File.Create(publicKeyPath))
            using (var armored = new ArmoredOutputStream(fs))
            {
                publicRing.Encode(armored);
            }
        }
    }
}