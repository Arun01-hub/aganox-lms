using Aganox.Rental.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRentalWebApi(builder.Configuration);

var app = builder.Build();

app.UseRentalWebApiPipeline();
await app.SeedRentalDatabaseAsync();

app.Run();
