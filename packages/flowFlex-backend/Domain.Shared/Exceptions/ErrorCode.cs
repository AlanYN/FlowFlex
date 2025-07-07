using Item.Common.Lib.Attr;

using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.Domain.Shared;

[IgnoreEnum]
public enum ErrorCodeEnum
{
    /// <summary>
    /// System exception
    /// </summary>
    [EnumValue(Description = "System error:{0}")]
    SystemError = 101,

    /// <summary>
    /// Incorrect request, protocol, or parameter
    /// </summary>
    [EnumValue(Description = "Bad request")]
    BadReqeust = 102,

    /// <summary>
    /// User authentication failure
    /// </summary>
    [EnumValue(Description = "Authentication failed")]
    AuthenticationFail = 103,

    /// <summary>
    /// Data decryption error
    /// </summary>
    [EnumValue(Description = "")]
    DecryptError = 104,

    /// <summary>
    /// Incorrect parameter
    /// </summary>
    [EnumValue(Description = "Parameter invalid|{0}")]
    ParamInvalid = 105,

    /// <summary>
    /// The data format is incorrect
    /// </summary>
    [EnumValue(Description = "Data format invalid|{0}")]
    DataFormatInvalid = 106,

    /// <summary>
    /// Upload file too large
    /// </summary>
    [EnumValue(Description = "Uploaded file is too large|{0}")]
    UploadFileTooLarge = 107,

    /// <summary>
    /// Login failure
    /// </summary>
    [EnumValue(Description = "Login fail|Login fail, {0}")]
    LoginFail = 108,

    /// <summary>
    /// Refresh Token
    /// </summary>
    [EnumValue(Description = "Token refresh fail")]
    RefreshTokenFail = 109,

    /// <summary>
    /// The entry object is empty
    /// </summary>
    [EnumValue(Description = "Parameter Is Null Error")]
    ParamIsNullError = 110,

    /// <summary>
    /// No related information is found in the database. Procedure
    /// </summary>
    [EnumValue(Description = "No relevant information found in the data.|No relevant information found in the data,ID:{0}")]
    DataIsNullError = 111,

    /// <summary>
    /// Incorrect data status
    /// </summary>
    [EnumValue(Description = "The current data status cannot be merged. The status is.|The current data status cannot be merged. The status is:{0}")]
    DataStatusError = 112,

    /// <summary>
    /// Data already exists
    /// </summary>
    [EnumValue(Description = "Route already exists.|Route already exists:{0}")]
    DataAlreadyExists = 113,

    /// <summary>
    /// Service exception
    /// </summary>
    [EnumValue(Description = "Some error occured when excute.|Some error occured when excute.{0}")]
    BusinessError = 114,

    /// <summary>
    /// The uploaded data exceeds the limit
    /// </summary>
    [EnumValue(Description = "Some error occured when excute.|The number of uploaded data exceeds the limit of 100")]
    Datalimit = 115,

    /// <summary>
    /// Unsupported file types
    /// </summary>
    [EnumValue(Description = "Some error occured when excute.|Unsupported file")]
    UnsupportedfileUploadTypes = 116,

    /// <summary>
    /// 404
    /// </summary>
    [EnumValue(Description = "{0}")]
    NotFound = 117,

    /// <summary>
    /// 
    /// </summary>
    [EnumValue(Description = "No relevant information found in the data.")]
    DataIsNullOrEmpty = 118,

    /// <summary>
    /// Your custom error description
    /// </summary>
    [EnumValue(Description = "{0}")]
    CustomError = 119,

    /// <summary>
    /// Operation not allowed
    /// </summary>
    [EnumValue(Description = "{0}")]
    OperationNotAllowed = 120,

    /// <summary>
    /// Conflict
    /// </summary>
    [EnumValue(Description = "{0}")]
    Conflict = 121,

    /// <summary>
    /// Validation failed
    /// </summary>
    [EnumValue(Description = "{0}")]
    ValidFail = 122,

    [EnumValue(Description = "{0}")]
    ExternalApiError = 123,

    /// <summary>
    /// File size exceeds the limit
    /// </summary>
    [EnumValue(Description = "File size exceeds the limit|{0}")]
    FileTooLarge = 124,

    /// <summary>
    /// Invalid file type
    /// </summary>
    [EnumValue(Description = "Invalid file type|{0}")]
    InvalidFileType = 125,

    /// <summary>
    /// File contains malicious code
    /// </summary>
    [EnumValue(Description = "Malicious content detected in file|{0}")]
    MaliciousContent = 126,

    [EnumValue(Description = "There are placeholders in the uploaded template that do not exist")]
    TemplateMarkNotfound = 1500,

    [EnumValue(Description = "this data not found")]
    DataNotFound = 1501,
    ThirdPartyError = 1502,
}
