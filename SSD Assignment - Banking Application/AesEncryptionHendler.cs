using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Banking_Application
{
    public static class AesEncryptionHandler
    {
        private const string CryptoKeyName = "bank_crypto_key_name";
        private static readonly CngProvider KeyStorageProvider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

        public static Aes GetOrCreateAesEncryptionKeyCBC(byte[] iv)
        {
            if (!CngKey.Exists(CryptoKeyName, KeyStorageProvider))
            {
                CreateAesKey();
            }

            Aes aes = new AesCng(CryptoKeyName, KeyStorageProvider);

            ConfigureAesParameters(aes, iv, CipherMode.CBC);

            return aes;
        }

        public static Aes GetOrCreateAesEncryptionKeyECB()
        {
            if (!CngKey.Exists(CryptoKeyName, KeyStorageProvider))
            {
                CreateAesKey();
            }

            Aes aes = new AesCng(CryptoKeyName, KeyStorageProvider);

            ConfigureAesParameters(aes, null, CipherMode.ECB);

            return aes;
        }

        private static void CreateAesKey()
        {
            CngKeyCreationParameters keyCreationParameters = new CngKeyCreationParameters
            {
                Provider = KeyStorageProvider
            };

            CngKey.Create(new CngAlgorithm("AES"), CryptoKeyName, keyCreationParameters);
        }

        private static void ConfigureAesParameters(Aes aes, byte[] iv, CipherMode mode)
        {
            aes.Mode = mode;

            if (iv != null)
            {
                aes.IV = iv;
            }

            aes.Padding = PaddingMode.PKCS7;
        }

        public static byte[] Encrypt(byte[] plaintextData, Aes aes)
        {
            byte[] ciphertextData;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plaintextData, 0, plaintextData.Length);
                    }

                    ciphertextData = msEncrypt.ToArray();
                }
            }

            return ciphertextData;
        }

        public static byte[] Decrypt(byte[] ciphertextData, Aes aes)
        {
            byte[] plaintextData;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(ciphertextData, 0, ciphertextData.Length);
                    }

                    plaintextData = msDecrypt.ToArray();
                }
            }

            return plaintextData;
        }

        public static string EncryptAccountNumber(string text)
        {
            Aes aes = GetOrCreateAesEncryptionKeyECB();
            byte[] plaintextData = Encoding.ASCII.GetBytes(text);
            byte[] ciphertextData = Encrypt(plaintextData, aes);

            aes.Dispose();

            return Convert.ToBase64String(ciphertextData);
        }

        public static string DecryptAccountNumber(string text)
        {
            Aes aes = GetOrCreateAesEncryptionKeyECB();
            byte[] ciphertextData = Convert.FromBase64String(text);
            byte[] plaintextData = Decrypt(ciphertextData, aes);

            aes.Dispose();

            return Encoding.ASCII.GetString(plaintextData);
        }
    }
}
