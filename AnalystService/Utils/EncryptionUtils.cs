using System.Security.Cryptography;
using System.Text;

namespace ClientService.Utils
{

    public class EncryptionUtils
    {
        private readonly string _secretKey;

        public EncryptionUtils(IConfiguration configuration)
        {
            _secretKey = configuration["Encryption:SecretKey"]; 
        }

        public string Encrypt(string plainText, Guid userId)
        {
            using var aes = Aes.Create();
            aes.Key = GetKey(userId);
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length); // сохраняем IV в начале
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs))
            {
                writer.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText, Guid userId)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = GetKey(userId);

            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);

            return reader.ReadToEnd();
        }

        private byte[] GetKey(Guid userId)
        {
            using var sha256 = SHA256.Create();
            var keyMaterial = Encoding.UTF8.GetBytes(_secretKey + userId.ToString());
            return sha256.ComputeHash(keyMaterial);
        }
    }
}

