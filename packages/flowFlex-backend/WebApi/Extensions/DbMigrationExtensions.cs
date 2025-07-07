
using Microsoft.EntityFrameworkCore;
using FlowFlex.Infrastructure.Data;

namespace FlowFlex.WebApi.Extensions;

public static class DbMigrationExtensions
{
    public static void UpdateDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OWContext>();
        dbContext.Database.Migrate();
    }
}
