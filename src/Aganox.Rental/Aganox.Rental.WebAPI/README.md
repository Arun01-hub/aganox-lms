# Aganox Rental WebAPI

Enterprise microservice backend for the Aganox Rental Angular SPA.

## Run

```powershell
dotnet run --launch-profile https
```

- API: `https://localhost:7241`
- Swagger: `https://localhost:7241/swagger`

Requires Aganox Identity Server at `https://localhost:7036`.

## Migrations

```powershell
dotnet ef migrations add <Name> --project ..\Aganox.Rental.Infrastructure --output-dir Migrations
```

Migrations apply automatically on startup.
