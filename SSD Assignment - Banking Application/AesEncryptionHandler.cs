using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;


namespace Banking_Application
{
    public static class AesEncryptionHandler
    {
        public static Aes GetOrCreateAesEncryptionKeyCBC(byte[] iv)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            String crypto_key_name = "bank_crypto_key_name";
            CngProvider key_storage_provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;


            if (!CngKey.Exists(crypto_key_name, key_storage_provider))
            {

                CngKeyCreationParameters key_creation_parameters = new CngKeyCreationParameters()
                {
                    Provider = key_storage_provider
                };

                CngKey.Create(new CngAlgorithm("AES"), crypto_key_name, key_creation_parameters);

            }
            Aes aes = aes = new AesCng(crypto_key_name, key_storage_provider);

            aes.Mode = CipherMode.CBC;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            return aes;
        }
        public static Aes GetOrCreateAesEncryptionKeyECB()
        {
            String crypto_key_name = "bank_crypto_key_name";

            CngProvider key_storage_provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

            if (!CngKey.Exists(crypto_key_name, key_storage_provider))
            {
                CngKeyCreationParameters key_creation_parameters = new CngKeyCreationParameters()
                {
                    Provider = key_storage_provider
                };

                CngKey.Create(new CngAlgorithm("AES"), crypto_key_name, key_creation_parameters);
            }

            Aes aes = new AesCng(crypto_key_name, key_storage_provider);

            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            return aes;
        }
        public static byte[] Encrypt(byte[] plaintext_data, Aes aes)
        {

            byte[] ciphertext_data;

            ICryptoTransform encryptor = aes.CreateEncryptor();

            MemoryStream msEncrypt = new MemoryStream();

            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            csEncrypt.Write(plaintext_data, 0, plaintext_data.Length);
            csEncrypt.Dispose();

            ciphertext_data = msEncrypt.ToArray();
            msEncrypt.Dispose();

            return ciphertext_data;

        }
        public static string Decrypt(string text, Aes aes)
        {

            byte[] plaintext_data;
            byte[] ciphertext_data = Convert.FromBase64String(text);

            ICryptoTransform decryptor = aes.CreateDecryptor();
            MemoryStream msDecrypt = new MemoryStream();

            CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write);
            csDecrypt.Write(ciphertext_data, 0, ciphertext_data.Length);
            csDecrypt.Dispose();

            plaintext_data = msDecrypt.ToArray();
            msDecrypt.Dispose();

            return Encoding.UTF8.GetString(plaintext_data);

        }
        public static string EncryptAccountNumber(string text)
        {
            Aes aes = GetOrCreateAesEncryptionKeyECB();

            byte[] plaintext_data = Encoding.ASCII.GetBytes(text);
            byte[] ciphertext_data = Encrypt(plaintext_data, aes);
            aes.Dispose();

            return Convert.ToBase64String(ciphertext_data);
        }
        public static string DecryptAccountNumber(string text)
        {
            Aes aes = GetOrCreateAesEncryptionKeyECB();
          //  byte[] ciphertext_data = Convert.FromBase64String(text);
            string plaintext_data = Decrypt(text, aes);
            aes.Dispose();

            return plaintext_data;
        }
    }
}
