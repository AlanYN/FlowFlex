using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.Application.Client
{
    public static class AddHttpClientExtensions
    {
        public static IServiceCollection AddClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            AddIdeClient(serviceCollection, configuration);
            AddHttpApiClient(serviceCollection, configuration);
            return serviceCollection;
        }

        private static void AddIdeClient(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddHttpClient<IdeClient>(c =>
            {
                c.BaseAddress = new Uri(configuration["IdeApis:BaseUrl"]);
            });
        }

        private static void AddHttpApiClient(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddHttpClient("HttpApiExecutor", c =>
            {
                c.DefaultRequestHeaders.Add("User-Agent", "FlowFlex-HttpApiExecutor");
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 100,
                EnableMultipleHttp2Connections = true
            });
        }
    }
}
