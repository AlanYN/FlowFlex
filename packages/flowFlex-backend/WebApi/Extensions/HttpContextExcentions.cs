namespace FlowFlex.WebApi.Extensions
{
    public static class HttpContextExcentions
    {
        public static bool CurrentRouterIsNewRouter(this HttpContext httpContext)
        {
            var endpoint = httpContext.GetEndpoint() as RouteEndpoint;
            var routeNameMetadata = endpoint?.Metadata.OfType<RouteNameMetadata>().SingleOrDefault();
            var routeName = routeNameMetadata?.RouteName;
            return !string.IsNullOrEmpty(routeName);
        }
    }
}
