using System;
using System.Net;

namespace FlowFlex.Domain.Shared;

public class CRMException : Exception
{
    public HttpStatusCode? StatusCode { get; set; }

    public ErrorCodeEnum Code { get; }

    public object ErrorData { get; set; }

    public CRMException(ErrorCodeEnum code) : base(ErrorMessage.GetErrorMessage(code))
    {
        Code = code;
    }

    public CRMException(ErrorCodeEnum code, params string[] parameters) : base(ErrorMessage.GetErrorMessage(code, parameters))
    {
        Code = code;
    }

    public CRMException(ErrorCodeEnum code, string message, params (string key, object value)[] datas) : base(message)
    {
        Code = code;
        foreach (var (key, value) in datas)
        {
            Data.Add(key, value);
        }
    }

    public CRMException(string message, ErrorCodeEnum code) : base(message)
    {
        Code = code;
    }

    public CRMException(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }

    public CRMException(HttpStatusCode? statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public CRMException(string errorMessage) : base(ErrorMessage.GetErrorMessage(ErrorCodeEnum.SystemError, errorMessage))
    {
        Code = ErrorCodeEnum.SystemError;
    }
}
