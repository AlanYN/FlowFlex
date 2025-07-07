namespace FlowFlex.Domain.Shared.Enums;

/*
  Status code specification:
    [72][000][0000]
    72 represents crm 
    000 represents business module
    0000 represents specific error under this module
 */

/// <summary>
/// Represents Unis status codes
/// </summary>
public enum CrmStatusCode
{
    Success = 0,
    CustomerError = 721000000,
    AuthenticationFail = 72,
    SystemError = 720010011
}

