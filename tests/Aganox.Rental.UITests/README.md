# Aganox Rental UI Automation Tests

End-to-end UI tests for the Aganox Rental product using **Playwright** and **TypeScript**.

## Architecture

```
tests/Aganox.Rental.UITests/
├── specs/                 # Test specifications grouped by feature
│   ├── auth.setup.ts      # Authenticates once; saves browser session
│   ├── smoke/             # Health + basic load checks
│   ├── auth/              # Login flow (unauthenticated browser)
│   ├── tenant/            # Browse & property detail flows
│   └── owner/             # Owner dashboard flows
├── pages/                 # Page Object Model helpers
├── reporters/             # Custom summary HTML + JSON reporter
├── reports/               # Generated reports (gitignored)
├── test-results/          # Screenshots, traces, videos (gitignored)
└── playwright.config.ts   # Playwright configuration
```

### Design principles

| Layer | Purpose |
|-------|---------|
| **Page Objects** | Stable selectors and reusable navigation (`pages/`) |
| **data-testid** | Reliable hooks in the Angular SPA (not CSS/class based) |
| **Auth setup project** | Logs in once via Identity Server; reuses `storageState` |
| **Multi-reporter** | Playwright HTML + custom summary HTML + JUnit + JSON |

## Prerequisites

1. Rental stack running (Identity Server, WebAPI, Angular SPA):

```powershell
.\build\run.ps1
```

2. Dev credentials (default in `.env.example`):

- Email: `admin@aganox.local`
- Password: `ChangeMe123!`

## Run tests

From repo root:

```powershell
# Start stack if needed, run Chromium tests, open summary report
.\build\test.ps1 -EnsureRunning -OpenReport

# Run all browser projects (Chromium + Firefox + unauthenticated auth tests)
.\build\test.ps1 -Project all -EnsureRunning

# Run a filtered subset
.\build\test.ps1 -Filter "browse"

# Debug interactively
cd tests/Aganox.Rental.UITests
npm run test:ui
```

From the test project folder:

```powershell
cd tests/Aganox.Rental.UITests
cp .env.example .env
npm install
npx playwright install chromium firefox
npm test
```

## Reports

After each run:

| Report | Path | Description |
|--------|------|-------------|
| **Summary HTML** | `reports/summary.html` | Run overview, pass/fail table, links to Playwright report |
| **Playwright HTML** | `reports/html/index.html` | Detailed steps, screenshots, traces, videos |
| **JUnit XML** | `reports/junit/results.xml` | CI integration |
| **JSON summary** | `reports/summary.json` | Machine-readable results |

Open Playwright report:

```powershell
cd tests/Aganox.Rental.UITests
npm run report
```

## Adding tests

1. Add `data-testid` to new Angular UI elements.
2. Extend or create a page object in `pages/`.
3. Add a spec under the appropriate folder in `specs/`.
4. Use authenticated projects (`chromium`, `firefox`) for flows behind `authGuard`.
5. Use `chromium-unauthenticated` for login/redirect scenarios.

## CI notes

Set `CI=true` to enable retries and forbid `.only` tests. Publish `reports/junit/results.xml` and `reports/html/` as build artifacts.
