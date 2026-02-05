using System.Security.Cryptography;
using System.Linq;
using System.IO;

namespace LootLocker.Utilities.Encryption
{
    public class LootLockerEncryptionUtilities
    {
        private static readonly byte[] Key = {
            0x81, 0x71, 0xF7, 0xD6, 0xE5, 0xC4, 0xB3, 0xA2,
            0x8A, 0x9B, 0xAC, 0xBD, 0xCE, 0xDF, 0xE0, 0xF1
        };
        
        public static string SimpleEncryptToBase64(string message)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;                        
                byte[] iv = aes.IV;
                using (MemoryStream ms = new())
                {
                    using (CryptoStream cryptoStream = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer = new(cryptoStream))
                        {
                            writer.Write(message);
                        }
                    }
                    byte[] encryptedBytes = ms.ToArray();
                    string encryptedBase64 = System.Convert.ToBase64String(iv.Concat(encryptedBytes).ToArray());
                    return encryptedBase64;
                }
            }
        }

        public static string SimpleDecryptFromBase64(string base64Message)
        {
            byte[] fullCipher = System.Convert.FromBase64String(base64Message);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;                        
                byte[] iv = fullCipher.Take(aes.BlockSize / 8).ToArray();
                byte[] cipherText = fullCipher.Skip(aes.BlockSize / 8).ToArray();
                using (MemoryStream ms = new(cipherText))
                {
                    using (CryptoStream cryptoStream = new(ms, aes.CreateDecryptor(Key, iv), CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new(cryptoStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static bool IsValidBase64String(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return false;

            try
            {
                System.Convert.FromBase64String(base64String);
                return true;
            }
            catch (System.FormatException)
            {
                return false;
            }
        }
    }
}