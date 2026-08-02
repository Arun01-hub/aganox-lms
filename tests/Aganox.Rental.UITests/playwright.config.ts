import { defineConfig, devices } from '@playwright/test';
import { config } from './utils/config';

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : undefined,
  timeout: 60_000,
  expect: { timeout: 10_000 },

  reporter: [
    ['list'],
    [
      'html',
      {
        outputFolder: 'reports/html',
        open: 'never',
        attachmentsBaseURL: 'test-results/'
      }
    ],
    ['json', { outputFile: 'reports/results.json' }],
    ['junit', { outputFile: 'reports/junit/results.xml' }],
    ['./reporters/run-summary.reporter.ts']
  ],

  outputDir: 'test-results',

  use: {
    baseURL: config.webBaseUrl,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    ignoreHTTPSErrors: true,
    actionTimeout: 15_000,
    navigationTimeout: 30_000
  },

  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/
    },
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: config.authStatePath
      },
      dependencies: ['setup'],
      testIgnore: [/auth\.setup\.ts/, /auth\/.*\.spec\.ts/]
    },
    {
      name: 'firefox',
      use: {
        ...devices['Desktop Firefox'],
        storageState: config.authStatePath
      },
      dependencies: ['setup'],
      testIgnore: [/auth\.setup\.ts/, /auth\/.*\.spec\.ts/]
    },
    {
      name: 'chromium-unauthenticated',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /auth\/.*\.spec\.ts/,
      testIgnore: /auth\.setup\.ts/
    }
  ]
});
