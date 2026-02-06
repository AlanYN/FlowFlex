using Item.Common.Lib.Attr;
using Microsoft.OpenApi.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Domain.Shared
{
    /// <summary>
    /// Error message helper class. Each error code corresponds to an error message.
    /// Message format: default error message|language1 error message|language2 error message|...
    /// </summary>
    public class ErrorMessage
    {
        public static string GetErrorMessage(ErrorCodeEnum code, params string[] parameters)
        {
            var enumValue = code.GetAttributeOfType<EnumValueAttribute>();

            if (enumValue == null)
            {
                return string.Empty;
            }

            var validParameters = parameters.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            string[] messages = enumValue.Description?.Split('|') ?? [];
            return string.Format(messages.LastOrDefault(), validParameters);
        }
    }
}
