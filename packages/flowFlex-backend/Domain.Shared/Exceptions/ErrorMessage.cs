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
        private static readonly Dictionary<ErrorCodeEnum, string> errorMessages;

        //static ErrorMessage()
        //{
        //    errorMessages = new Dictionary<ErrorCodeEnum, string>
        //    {
        //        [ErrorCodeEnum.SystemError] = "System error",
        //        [ErrorCodeEnum.BadReqeust] = "Bad request",
        //        [ErrorCodeEnum.AuthenticationFail] = "Authentication failed",
        //        [ErrorCodeEnum.ParamInvalid] = "Parameter invalid|{0}",
        //        [ErrorCodeEnum.LoginFail] = "Login fail|Login fail, {0}",
        //        [ErrorCodeEnum.UploadFileTooLarge] = "Uploaded file is too large|{0}",
        //        [ErrorCodeEnum.DataFormatInvalid] = "Data format invalid|{0}",
        //        [ErrorCodeEnum.RefreshTokenFail] = "Token refresh fail",
        //        [ErrorCodeEnum.ParamIsNullError] = "Parameter Is Null Error",
        //        [ErrorCodeEnum.DataIsNullError] = "No relevant information found in the data.|No relevant information found in the data,ID:{0}",
        //        [ErrorCodeEnum.DataStatusError] = "The current data status cannot be merged. The status is.|The current data status cannot be merged. The status is:{0}",
        //        [ErrorCodeEnum.BusinessError] = "Some error occured when excute.|Some error occured when excute.{0}",
        //        [ErrorCodeEnum.DataAlreadyExists] = "Route already exists.|Route already exists:{0}",
        //        [ErrorCodeEnum.Datalimit] = "Some error occured when excute.|The number of uploaded data exceeds the limit of 100",
        //        [ErrorCodeEnum.UnsupportedfileUploadTypes] = "Some error occured when excute.|Unsupported file",
        //        [ErrorCodeEnum.NotFound] = "Not Found",
        //        [ErrorCodeEnum.DataIsNullOrEmpty] = "No relevant information found in the data.",
        //        [ErrorCodeEnum.CustomError] = "{0}",
        //        [ErrorCodeEnum.Conflict] = "{0}",
        //        [ErrorCodeEnum.OperationNotAllowed] = "{0}"
        //    };
        //}

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
