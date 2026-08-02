using Aganox.Rental.Infrastructure.Seeding;

namespace Aganox.Rental.WebAPI.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseRentalWebApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Aganox Rental API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("SpaClient");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }

    public static async Task SeedRentalDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IRentalDbSeeder>();
        await seeder.SeedAsync();
    }
}
