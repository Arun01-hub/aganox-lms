# Aganox Rental

Angular SPA for the Aganox Rental product, authenticated via **Aganox Identity Server** (OpenIddict).

## Prerequisites

- Node.js 20+
- Aganox Identity Server running at `https://localhost:7036`
- Aganox Rental WebAPI running at `https://localhost:7241`

## Run locally

From the repo root:

```powershell
.\build\run.ps1
```

Or manually:

1. Start Identity Server and Rental WebAPI
2. Start the rental web app:

```bash
cd aganox-rental-web
npm start
```

3. Open `http://localhost:4200`

## Backend API

The Angular app calls **Aganox Rental WebAPI** (`environment.apiUrl`, default `https://localhost:7241`).

| Setting | Value |
|---|---|
| API base | `https://localhost:7241` |
| Properties | `GET/POST /api/properties` |
| My listings | `GET /api/properties/mine` (Bearer token) |

## Authentication

| Setting | Value |
|---|---|
| Client ID | `aganox-rental` |
| Flow | Authorization Code + PKCE |
| Scopes | `openid profile email offline_access aganox.api` |
| Issuer | `https://localhost:7036` |

Dev seed user: `admin@aganox.local` / `ChangeMe123!`

## Build

```bash
npm run build
```
