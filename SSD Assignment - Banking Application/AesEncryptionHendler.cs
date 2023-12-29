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
    public static class AesEncryptionHendler
    {
        public static Aes GetOrCreateAesEncryptionKey(byte[] iv)
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
            Aes aes = aes = new AesCng(crypto_key_name, key_storage_provider); // Declare aes outside the if block

            aes.Mode = CipherMode.CBC;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            return aes;
        }

        public static byte[] Encrypt(byte[] plaintext_data, Aes aes)
        {

            byte[] ciphertext_data;//Byte Array Where Result Of Encryption Operation Will Be Stored.

            ICryptoTransform encryptor = aes.CreateEncryptor();//Object That Contains The AES Encryption Algorithm (Using The Key and IV Value Specified In The AES Object). 

            MemoryStream msEncrypt = new MemoryStream();//MemoryStream That Will Store The Output Of The Encryption Operation.

            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            csEncrypt.Write(plaintext_data, 0, plaintext_data.Length);//Writes All Data Contained In plaintext_data Byte Array To CryptoStream (Which Then Gets Encrypted And Gets Written to the msEncrypt MemoryStream).
            csEncrypt.Dispose();//Closes CryptoStream

            ciphertext_data = msEncrypt.ToArray();//Output Result Of Encryption Operation In Byte Array Form.
            msEncrypt.Dispose();//Closes MemoryStream

            return ciphertext_data;

        }

        public static byte[] Decrypt(byte[] ciphertext_data, Aes aes)
        {

            byte[] plaintext_data;//Byte Array Where Result Of Decryption Operation Will Be Stored.

            ICryptoTransform decryptor = aes.CreateDecryptor();//Object That Contains The AES Decryption Algorithm (Using The Key and IV Value Specified In The AES Object). 

            MemoryStream msDecrypt = new MemoryStream();//MemoryStream That Will Store The Output Of The Decryption Operation.

            CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write);//Writes All Data Contained In Byte Array To CryptoStream (Which Then Gets Decrypted).
            csDecrypt.Write(ciphertext_data, 0, ciphertext_data.Length);//Writes All Data Contained In ciphertext_data Byte Array To CryptoStream (Which Then Gets Decrypted And Gets Written to the msDecrypt MemoryStream).
            csDecrypt.Dispose();//Closes CryptoStream

            plaintext_data = msDecrypt.ToArray();//Output Result Of Decryption Operation In Byte Array Form.
            msDecrypt.Dispose();//Closes MemoryStream

            return plaintext_data;

        }
    }
}