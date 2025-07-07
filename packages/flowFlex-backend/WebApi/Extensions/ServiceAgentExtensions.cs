using Item.Common.Lib.Common;

namespace FlowFlex.WebApi.Extensions
{
    public static class ServiceAgentExtensions
    {
        public static WebApplication UseServiceAgent(this WebApplication app)
        {
            ServiceAgent.Provider = app.Services;
            return app;
        }
    }
}
