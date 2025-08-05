using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices
{
    /// <summary>
    /// 加密服务接口
    /// Provides encryption and decryption services for sensitive data
    /// </summary>
    public interface IEncryptionService : IScopedService
    {
        /// <summary>
        /// 加密文本
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>加密后的密文（Base64编码）</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// 解密文本
        /// </summary>
        /// <param name="cipherText">密文（Base64编码）</param>
        /// <returns>解密后的明文</returns>
        string Decrypt(string cipherText);

        /// <summary>
        /// 验证加密密钥是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        bool ValidateEncryptionKey();
    }
}