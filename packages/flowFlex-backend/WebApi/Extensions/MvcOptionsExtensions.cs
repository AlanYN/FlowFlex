using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using FlowFlex.WebApi.Routes;

namespace FlowFlex.WebApi.Extensions;

public static class MvcOptionsExtensions
{
    public static void UseCentralRoutePrefix(this MvcOptions options, IRouteTemplateProvider routeAttribute)
    {
        options.Conventions.Insert(0, new RouteConvention(routeAttribute));
    }
}
