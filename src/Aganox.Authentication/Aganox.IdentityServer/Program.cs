using Aganox.IdentityServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServerHost(builder.Configuration);

var app = builder.Build();

app.UseIdentityServerPipeline();
await app.SeedIdentityDatabaseAsync();

app.Run();
