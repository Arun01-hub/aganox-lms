using Aganox.IdentityServer.Infrastructure.Seeding;

namespace Aganox.IdentityServer.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseIdentityServerPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("SpaClients");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapRazorPages();

        return app;
    }

    public static async Task SeedIdentityDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder>();
        await seeder.SeedAsync();
    }
}
