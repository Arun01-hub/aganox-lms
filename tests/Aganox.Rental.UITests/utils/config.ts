import dotenv from 'dotenv';
import path from 'path';

dotenv.config({ path: path.resolve(__dirname, '..', '.env') });

export const config = {
  webBaseUrl: process.env.WEB_BASE_URL ?? 'http://localhost:4200',
  identityBaseUrl: process.env.IDENTITY_BASE_URL ?? 'https://localhost:7036',
  apiBaseUrl: process.env.API_BASE_URL ?? 'https://localhost:7241',
  testUser: {
    email: process.env.TEST_USER_EMAIL ?? 'admin@aganox.local',
    password: process.env.TEST_USER_PASSWORD ?? 'ChangeMe123!'
  },
  authStatePath: path.resolve(__dirname, '..', '.auth', 'user.json'),
  reportsDir: path.resolve(__dirname, '..', 'reports')
} as const;
