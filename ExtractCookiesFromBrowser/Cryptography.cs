using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ExtractCookiesFromBrowser
{
    public static class Cryptography
    {
        public static byte[] DecryptInGcmMode(byte[] bytes, string key)
        {
            Span<byte> encryptedData = bytes.AsSpan();

            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            int cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            Span<byte> plainBytes = cipherSize < 1024
                ? stackalloc byte[cipherSize]
                : new byte[cipherSize];
            var k = new Rfc2898DeriveBytes(key, new byte[8], 1000).GetBytes(16);
            using var aes = new AesGcm(k);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return plainBytes.ToArray();
        }

        public static byte[] Decrypt(byte[] bytes, string key)
        {
            byte[] iv = new byte[16];
            Array.Copy(bytes, iv, 16);
            AesManaged algorithm = new AesManaged();
            algorithm.IV = iv;
            algorithm.Key = Encoding.UTF8.GetBytes(key);

            byte[] ret = null;
            using (var decryptor = algorithm.CreateDecryptor())
            {
                using (MemoryStream msDecrypted = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msDecrypted, decryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(bytes, 16, bytes.Length - 16);
                    }
                    ret = msDecrypted.ToArray();
                }
            }
            return ret;
        }
    }
}