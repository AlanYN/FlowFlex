using Aliyun.OSS.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Application.Contracts;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Filter;

public class SaveRequestDataRecordAttribute : ActionFilterAttribute
{
    public static readonly string Key = Guid.NewGuid().ToString();
    public static readonly string ReferenceId = Guid.NewGuid().ToString();

    public RecordSource Source { get; set; }

    public string Type { get; set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var requestDataService = httpContext.RequestServices.GetService<IRequestDataRecordService>();

        var url = HttpUtils.DecodeUri(httpContext.Request.GetEncodedUrl());
        if (url.Length > 8000)
            url = url[..8000];

        var model = new RequestDataRecordModel
        {
            Type = Type,
            Url = url,
            CreateTime = DateTime.Now,
            Source = Source,
            Method = httpContext.Request.Method,
            RequestData = await ReadRequestContent(httpContext.Request),
            TenantId = GetTenantId(httpContext.Request)
        };

        var id = await requestDataService.InsertRequestRecordAsync(model);

        context.HttpContext.Items.Add(Key, id);

        await next();
    }

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();

        var httpContext = context.HttpContext;
        var requestDataService = httpContext.RequestServices.GetService<IRequestDataRecordService>();

        if (httpContext.Items.TryGetValue(Key, out var id))
        {
            var content = await ReadResponseContent(httpContext.Response);
            var referenceId = GetReferenceId(context.HttpContext);
            await requestDataService.UpdateResponseAsync(Convert.ToInt64(id), content, referenceId);
        }
    }

    private long? GetReferenceId(HttpContext httpContext)
    {
        long? targetId = null;
        if (httpContext.Items.TryGetValue(ReferenceId, out var value))
        {
            targetId = Convert.ToInt64(value);
        }

        return targetId;
    }

    private static string GetTenantId(HttpRequest request)
    {
        if (request.Headers.TryGetValue("tenant_id", out var value))
        {
            return value.ToString();
        }

        var userContext = request.HttpContext.RequestServices.GetService<UserContext>();
        return userContext.TenantId;
    }

    private async Task<string> ReadRequestContent(HttpRequest request)
    {
        Stream stream = request.Body;
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        var bs = new byte[stream.Length];
        await stream.ReadAsync(bs, 0, bs.Length);
        var content = System.Text.Encoding.UTF8.GetString(bs);
        return content;
    }

    private async Task<string> ReadResponseContent(HttpResponse response)
    {
        Stream stream = response.Body;
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        var bs = new byte[stream.Length];
        await stream.ReadAsync(bs, 0, bs.Length);
        var content = System.Text.Encoding.UTF8.GetString(bs);

        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        return content;
    }
}
