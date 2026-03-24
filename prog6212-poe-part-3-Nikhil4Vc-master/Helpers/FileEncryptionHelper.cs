using System.Security.Cryptography;
using System.Text;

namespace MonthlyClaims_System.Helpers
{
    public class FileEncryptionHelper
    {
        private static readonly string secretKey = "MySuperSecretKey123";

        // Encrypts claims document using SHA256 before it is added to the database
        public static void EncryptFile(string inputPath, string outputPath)
        {
            byte[] key = Encoding.UTF8.GetBytes(secretKey);
            byte[] iv = new byte[16]; 

            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.HashData(key); 
                aes.IV = iv;

                using FileStream inputFileStream = new FileStream(inputPath, FileMode.Open);
                using FileStream outputFileStream = new FileStream(outputPath, FileMode.Create);
                using CryptoStream cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

                inputFileStream.CopyTo(cryptoStream);
            }

            File.Delete(inputPath);
        }

        // decrypts claims document when document after it is retrieved from the database
        public static void DecryptFile(string inputPath, string outputPath)
        {
            byte[] key = Encoding.UTF8.GetBytes(secretKey);
            byte[] iv = new byte[16];

            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.HashData(key); 
                aes.IV = iv;

                using FileStream inputFileStream = new FileStream(inputPath, FileMode.Open);
                using FileStream outputFileStream = new FileStream(outputPath, FileMode.Create);
                using CryptoStream cryptoStream = new CryptoStream(inputFileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

                cryptoStream.CopyTo(outputFileStream);
            }
        }
    }
}

