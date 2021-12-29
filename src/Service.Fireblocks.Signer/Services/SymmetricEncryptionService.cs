using System.IO;
using System.Text;
using System;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Utilities.Encoders;

namespace Service.Fireblocks.Signer.Services
{
    public class SymmetricEncryptionService
    {
        private const int KeyBitSize = 256;
        private const int NonceBitSize = 128;

        private readonly SecureRandom _random;
        private readonly byte[] _secret;

        public SymmetricEncryptionService(string secret)
        {
            _secret = Convert.FromBase64String(secret);
            _random = new SecureRandom();
        }

        public string Encrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("data required!", nameof(data));

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encryptedData = EncryptWithKey(dataBytes, _secret);

            return Convert.ToBase64String(encryptedData);
        }

        public string Decrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("data is required!", nameof(data));

            var cipherData = Convert.FromBase64String(data);
            var plainText = DecryptWithKey(cipherData, _secret);

            return Encoding.UTF8.GetString(plainText);
        }

        private byte[] DecryptWithKey(byte[] message, byte[] key)
        {
            if (key == null || key.Length != KeyBitSize / 8)
                throw new ArgumentException($"Key needs to be {KeyBitSize} bit!", nameof(key));

            if (message == null || message.Length == 0)
                throw new ArgumentException("Message required!", nameof(message));

            using (var cipherStream = new MemoryStream(message))
            using (var cipherReader = new BinaryReader(cipherStream))
            {
                var cipherNonce = cipherReader.ReadBytes(NonceBitSize / 8);
                var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()), new Pkcs7Padding());
                var parameters = new ParametersWithIV(new KeyParameter(key), cipherNonce);
                cipher.Init(false, parameters);

                var cipherData = cipherReader.ReadBytes(message.Length - cipherNonce.Length);

                var buffer = new byte[cipher.GetOutputSize(cipherData.Length)];
                var length = cipher.ProcessBytes(cipherData, buffer, 0);
                try
                {
                    cipher.DoFinal(buffer, length);
                }
                catch (InvalidCipherTextException)
                {
                    return null;
                }

                return Trim(buffer);
            }
        }

        private byte[] EncryptWithKey(byte[] data, byte[] key)
        {
            if (key == null || key.Length != KeyBitSize / 8)
                throw new ArgumentException($"Key needs to be {KeyBitSize} bit!", nameof(key));

            var cipherNonce = new byte[NonceBitSize / 8];

            if (cipherNonce.Length == 0)
            {
                _random.NextBytes(cipherNonce, 0, cipherNonce.Length);
            }

            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()), new Pkcs7Padding());
            var parameters = new ParametersWithIV(new KeyParameter(key), cipherNonce);
            cipher.Init(true, parameters);
            var buffer = new byte[cipher.GetOutputSize(data.Length)];
            var length = cipher.ProcessBytes(data, buffer, 0);
            try
            {
                cipher.DoFinal(buffer, length);
            }
            catch (InvalidCipherTextException)
            {
                return null;
            }

            using var combinedStream = new MemoryStream();
            using (var binaryWriter = new BinaryWriter(combinedStream))
            {
                binaryWriter.Write(cipherNonce);
                binaryWriter.Write(buffer);
            }

            return combinedStream.ToArray();
        }

        private byte[] Trim(byte[] data)
        {
            int length = 0;

            foreach (var item in data)
            {
                if (item == 0)
                    break;
                length++;
            }

            var result = new byte[length];
            Array.Copy(data, result, length);
            return result;
        }

        public string GetSha256Hash(string input)
        {
            var data = Encoding.UTF8.GetBytes(input);
            var hash = new Sha256Digest();
            hash.BlockUpdate(data, 0, data.Length);
            var result = new byte[hash.GetDigestSize()];
            hash.DoFinal(result, 0);
            return Hex.ToHexString(result);
        }
    }
}
