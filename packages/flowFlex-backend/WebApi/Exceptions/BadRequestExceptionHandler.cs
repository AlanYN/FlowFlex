using Item.Internal.StandardApi;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using FlowFlex.Domain.Shared;
using Serilog;

using System.Diagnostics;
using System.Net;

namespace FlowFlex.WebApi.Exceptions;

public class BadRequestExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Generate unique identifier
        string logId = Guid.NewGuid().ToString("N");

        var options = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<StandardApiConfig>>();
        var ex = exception;

        // Generate an error code based on the exception
        var errorCode = StandardApiCodeProvider.GenerateApiCode(ex, options.CurrentValue);

        var defaultcode = options.CurrentValue.Moudle_Codes
                        .FirstOrDefault(m => m.Name.Any(n => n.Key.Equals("Default", StringComparison.OrdinalIgnoreCase)))?.Code;

        // records unconfigured modules
        if (errorCode.Contains(defaultcode))
        {
            var stackTrace = new StackTrace(exception, fNeedFileInfo: false);
            var frame = stackTrace.GetFrame(0);
            var method = frame?.GetMethod();
            Type declaringType = method?.DeclaringType;
            if (declaringType is not null)
            {
                if (declaringType.FullName.StartsWith("Unis."))
                {
                    Log.Error($"logId:{logId} - {declaringType.DeclaringType?.Name}£ºthe current module is not configured");
                }
            }
        }

        if (ex is CRMException crmException)
        {
            // Set the response status code based on the CRMException
            httpContext.Response.StatusCode = crmException.StatusCode != null
                ? (int)crmException.StatusCode
                : (int)HttpStatusCode.OK;

            // Update the error code based on the CRMException
            errorCode += crmException.StatusCode != null
                ? (int)crmException.StatusCode
                : (int)crmException.Code;

            var message = crmException.Message;
            var error = ErrorResponse.Create(errorCode, message);
            error.Data = crmException.ErrorData;

            await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        }
        else
        {
            // Handle other exceptions
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            errorCode += (int)ErrorCodeEnum.SystemError; // Append system error code
            var error = ErrorResponse.Create(errorCode, $"{ErrorCodeEnum.SystemError.ToString()}(logId:{logId})");
            await httpContext.Response.WriteAsJsonAsync(error);
        }
        // Debug: Add error ID
        Console.Write($"logId:{logId} - crmerrormake:Message:[{exception.Message}] " +
            $"StackTrace:¡¾{exception.StackTrace}¡¿" +
            $"END¡¾{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}¡¿");
        Log.Error(exception, $"logId:{logId} - {exception.Message}");
        return true;
    }
}
