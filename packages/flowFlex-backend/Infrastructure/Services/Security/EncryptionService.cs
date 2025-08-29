using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FlowFlex.Infrastructure.Services.Security
{
    /// <summary>
    /// AES加密服务实现
    /// Provides AES encryption and decryption for sensitive data like API keys
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly SecurityOptions _securityOptions;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(IOptions<SecurityOptions> securityOptions, ILogger<EncryptionService> logger)
        {
            _securityOptions = securityOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// 加密文本
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>加密后的密文（Base64编码）</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_securityOptions.EncryptionKey);
                aes.IV = Encoding.UTF8.GetBytes(_securityOptions.EncryptionIV);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                using var writer = new StreamWriter(cryptoStream);

                writer.Write(plainText);
                writer.Flush();
                cryptoStream.FlushFinalBlock();

                var encrypted = memoryStream.ToArray();
                return Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        /// <summary>
        /// 解密文本
        /// </summary>
        /// <param name="cipherText">密文（Base64编码）</param>
        /// <returns>解密后的明文</returns>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return string.Empty;
            }

            try
            {
                var buffer = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_securityOptions.EncryptionKey);
                aes.IV = Encoding.UTF8.GetBytes(_securityOptions.EncryptionIV);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(buffer);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        /// <summary>
        /// 验证加密密钥是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool ValidateEncryptionKey()
        {
            try
            {
                if (string.IsNullOrEmpty(_securityOptions.EncryptionKey) ||
                    string.IsNullOrEmpty(_securityOptions.EncryptionIV))
                {
                    return false;
                }

                if (_securityOptions.EncryptionKey.Length != 32 ||
                    _securityOptions.EncryptionIV.Length != 16)
                {
                    return false;
                }

                // Test encryption/decryption with a sample text
                const string testText = "encryption_test";
                var encrypted = Encrypt(testText);
                var decrypted = Decrypt(encrypted);

                return testText.Equals(decrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption key validation failed");
                return false;
            }
        }
    }
}