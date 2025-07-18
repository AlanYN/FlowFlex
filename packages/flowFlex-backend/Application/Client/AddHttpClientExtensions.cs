using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.Application.Client
{
    public static class AddHttpClientExtensions
    {
        public static IServiceCollection AddClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            AddIdeClient(serviceCollection, configuration);
            return serviceCollection;
        }

        private static void AddIdeClient(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddHttpClient<IdeClient>(c =>
            {
                c.BaseAddress = new Uri(configuration["IdeApis:BaseUrl"]);
            });
        }
    }
}
