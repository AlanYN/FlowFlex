
using Item.Common.Lib.JsonConverts;
using Newtonsoft.Json;

namespace FlowFlex.Domain.Shared.Models
{
    public class BaseUserInfoModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Phone area code identifier
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long? PhoneNumberPrefixesId { get; set; }

    }
}
