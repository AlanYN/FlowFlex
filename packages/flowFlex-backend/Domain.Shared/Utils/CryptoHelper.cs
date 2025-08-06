using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlowFlex.Domain.Shared.Utils
{
    /// <summary>
    /// Cryptographic helper for secure token generation and link encryption
    /// </summary>
    public static class CryptoHelper
    {
        private static readonly string DefaultKey = "FlowFlex2025SecureKey!!"; // Should be from configuration

        /// <summary>
        /// Encrypt portal access data to prevent ID exposure
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="invitationToken">Invitation token</param>
        /// <param name="expiryTime">Token expiry time</param>
        /// <returns>Encrypted portal access token</returns>
        public static string EncryptPortalAccessData(long onboardingId, string invitationToken, DateTimeOffset expiryTime)
        {
            var data = new
            {
                OnboardingId = onboardingId,
                InvitationToken = invitationToken,
                ExpiryTime = expiryTime,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var jsonData = JsonSerializer.Serialize(data);
            return EncryptString(jsonData);
        }

        /// <summary>
        /// Decrypt portal access data
        /// </summary>
        /// <param name="encryptedToken">Encrypted token</param>
        /// <returns>Decrypted portal access data</returns>
        public static PortalAccessData? DecryptPortalAccessData(string encryptedToken)
        {
            try
            {
                var decryptedJson = DecryptString(encryptedToken);
                var data = JsonSerializer.Deserialize<PortalAccessDataInternal>(decryptedJson);
                
                if (data == null)
                    return null;

                return new PortalAccessData
                {
                    OnboardingId = data.OnboardingId,
                    InvitationToken = data.InvitationToken,
                    ExpiryTime = data.ExpiryTime,
                    CreatedAt = data.CreatedAt
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate secure invitation token
        /// </summary>
        /// <returns>Secure random token</returns>
        public static string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
        }

        /// <summary>
        /// Generate short URL identifier using MD5 hash
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="email">Email address</param>
        /// <param name="invitationToken">Invitation token</param>
        /// <returns>32-character MD5 hash for short URL</returns>
        public static string GenerateShortUrlId(long onboardingId, string email, string invitationToken)
        {
            var input = $"{onboardingId}:{email}:{invitationToken}:{DateTimeOffset.UtcNow:yyyyMMdd}";
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Encrypt string using AES
        /// </summary>
        /// <param name="plainText">Plain text to encrypt</param>
        /// <param name="key">Encryption key (optional)</param>
        /// <returns>Encrypted string</returns>
        private static string EncryptString(string plainText, string? key = null)
        {
            key ??= DefaultKey;
            
            using var aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(key, aes.KeySize / 8);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var iv = aes.IV;
            var encrypted = msEncrypt.ToArray();
            var result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

            return Convert.ToBase64String(result).Replace("/", "_").Replace("+", "-");
        }

        /// <summary>
        /// Decrypt string using AES
        /// </summary>
        /// <param name="cipherText">Encrypted text</param>
        /// <param name="key">Decryption key (optional)</param>
        /// <returns>Decrypted string</returns>
        private static string DecryptString(string cipherText, string? key = null)
        {
            key ??= DefaultKey;
            
            // Restore base64 padding
            cipherText = cipherText.Replace("_", "/").Replace("-", "+");
            while (cipherText.Length % 4 != 0)
                cipherText += "=";

            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(key, aes.KeySize / 8);

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }

        /// <summary>
        /// Derive key from password using PBKDF2
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="keyLength">Key length in bytes</param>
        /// <returns>Derived key</returns>
        private static byte[] DeriveKeyFromPassword(string password, int keyLength)
        {
            var salt = Encoding.UTF8.GetBytes("FlowFlexSalt2025"); // Should be from configuration
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keyLength);
        }

        /// <summary>
        /// Internal data structure for JSON serialization
        /// </summary>
        private class PortalAccessDataInternal
        {
            public long OnboardingId { get; set; }
            public string InvitationToken { get; set; } = string.Empty;
            public DateTimeOffset ExpiryTime { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }
    }

    /// <summary>
    /// Portal access data structure
    /// </summary>
    public class PortalAccessData
    {
        public long OnboardingId { get; set; }
        public string InvitationToken { get; set; } = string.Empty;
        public DateTimeOffset ExpiryTime { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
} 