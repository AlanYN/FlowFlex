using FlowFlex.Domain.Entities.OW;
using SqlSugar;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 加密现有API Key数据迁移
    /// 
    /// 此迁移将所有现有的明文API Key加密存储
    /// 
    /// 安全目标：
    /// - 将现有明文API Key转换为加密存储
    /// - 确保敏感数据的安全性
    /// - 提供回滚机制（如果需要）
    /// </summary>
    public class EncryptExistingApiKeys_20250115000001
    {
        // 临时加密方法，用于数据迁移
        // 注意：在生产环境中，这些密钥应该从配置中读取
        private const string TEMP_ENCRYPTION_KEY = "FlowFlexEncryptionKey12345678901"; // 32字符
        private const string TEMP_ENCRYPTION_IV = "FlowFlexIV123456";  // 16字符

        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting API Key encryption migration...");
                Console.WriteLine("WARNING: Using temporary encryption keys for migration. Please update with production keys in configuration.");

                // 简单加密方法验证
                if (!ValidateEncryptionKeys())
                {
                    throw new InvalidOperationException("Encryption key validation failed.");
                }

                // 获取所有AI模型配置
                var configs = db.Queryable<AIModelConfig>()
                    .Where(c => c.ApiKey != null && c.ApiKey != "")
                    .ToList();

                Console.WriteLine($"Found {configs.Count} configurations to encrypt...");

                int encryptedCount = 0;
                foreach (var config in configs)
                {
                    try
                    {
                        // 检查是否已经是加密的（简单检查：Base64格式且长度合理）
                        var isAlreadyEncrypted = IsLikelyEncrypted(config.ApiKey);
                        
                        if (!isAlreadyEncrypted)
                        {
                            var originalApiKey = config.ApiKey;
                            
                            // 加密API Key
                            config.ApiKey = EncryptString(originalApiKey);
                            
                            // 更新数据库
                            db.Updateable<AIModelConfig>()
                                .SetColumns(c => new AIModelConfig { ApiKey = config.ApiKey })
                                .Where(c => c.Id == config.Id)
                                .ExecuteCommand();
                            
                            encryptedCount++;
                            Console.WriteLine($"Encrypted API key for configuration ID: {config.Id}, Provider: {config.Provider}");
                        }
                        else
                        {
                            Console.WriteLine($"Skipped already encrypted configuration ID: {config.Id}, Provider: {config.Provider}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to encrypt API key for configuration ID: {config.Id}, Provider: {config.Provider}. Error: {ex.Message}");
                        // 继续处理其他配置，不中断整个迁移
                    }
                }

                Console.WriteLine($"API Key encryption migration completed. Encrypted {encryptedCount} configurations.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Key encryption migration failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("API Key encryption migration rollback is not implemented for security reasons.");
            Console.WriteLine("If you need to rollback, please restore from a database backup taken before the migration.");
            
            // 出于安全考虑，不提供自动解密功能
            // 如果需要回滚，应该从备份恢复数据库
        }

        /// <summary>
        /// 加密字符串（简化版本，用于迁移）
        /// </summary>
        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(TEMP_ENCRYPTION_KEY);
            aes.IV = Encoding.UTF8.GetBytes(TEMP_ENCRYPTION_IV);
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

        /// <summary>
        /// 验证加密密钥
        /// </summary>
        private static bool ValidateEncryptionKeys()
        {
            try
            {
                if (TEMP_ENCRYPTION_KEY.Length != 32 || TEMP_ENCRYPTION_IV.Length != 16)
                    return false;

                // 测试加密解密
                const string testText = "test";
                var encrypted = EncryptString(testText);
                return !string.IsNullOrEmpty(encrypted);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 简单检查字符串是否可能已经被加密
        /// </summary>
        /// <param name="apiKey">API Key字符串</param>
        /// <returns>是否可能已加密</returns>
        private static bool IsLikelyEncrypted(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            try
            {
                // 检查是否是有效的Base64字符串
                var buffer = Convert.FromBase64String(apiKey);
                
                // 加密后的字符串通常会比较长（至少32字符）
                // 且包含Base64字符
                return apiKey.Length >= 32 && 
                       buffer.Length >= 16 && 
                       !apiKey.Contains(" ") &&
                       !apiKey.StartsWith("sk-") && // OpenAI API key格式
                       !apiKey.StartsWith("glm-") && // ZhipuAI API key格式
                       !apiKey.Contains(".");
            }
            catch
            {
                return false;
            }
        }
    }
}