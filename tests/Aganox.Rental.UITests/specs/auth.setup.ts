import { test as setup, expect, type APIRequestContext } from '@playwright/test';
import fs from 'fs';
import path from 'path';
import { config } from '../utils/config';
import { loginThroughIdentity } from '../pages';

async function waitForWebApp(request: APIRequestContext): Promise<void> {
  const deadline = Date.now() + 90_000;
  while (Date.now() < deadline) {
    const response = await request.get(`${config.webBaseUrl}/login`).catch(() => null);
    if (response?.ok()) {
      return;
    }

    await new Promise(resolve => setTimeout(resolve, 2_000));
  }

  throw new Error(`Rental web app is not reachable at ${config.webBaseUrl}. Run .\\build\\run.ps1 first.`);
}

setup('authenticate test user', async ({ page, request }) => {
  fs.mkdirSync(path.dirname(config.authStatePath), { recursive: true });

  const health = await request.get(`${config.apiBaseUrl}/health`, {
    ignoreHTTPSErrors: true
  });
  expect(health.ok(), 'Rental API health check failed — run .\\build\\run.ps1 first').toBeTruthy();
  await waitForWebApp(request);

  await loginThroughIdentity(page);

  await page.waitForFunction(
    () => localStorage.getItem('access_token') !== null,
    undefined,
    { timeout: 15_000 }
  );

  await page.context().storageState({ path: config.authStatePath });
});
