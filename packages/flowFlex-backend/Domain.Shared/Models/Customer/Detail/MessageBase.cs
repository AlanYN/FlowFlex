using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Base class for messages in the CRM system
    /// </summary>
    /// <typeparam name="T">Type of message data, must inherit from MessageDataBase</typeparam>
    public class MessageBase<T> where T : MessageDataBase
    {
        /// <summary>
        /// Hash value of the Data property
        /// </summary>
        public string Hash
        {
            get
            {
                return GetDataHashCode();
            }
        }

        /// <summary>
        /// The actual data of the message
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Computes the SHA256 hash of the serialized Data property
        /// </summary>
        /// <returns>A string representation of the computed hash</returns>
        public string GetDataHashCode()
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
