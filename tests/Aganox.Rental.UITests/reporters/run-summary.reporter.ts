import type {
  FullResult,
  Reporter,
  TestCase,
  TestResult
} from '@playwright/test/reporter';
import fs from 'fs';
import path from 'path';

interface TestRecord {
  title: string;
  file: string;
  project: string;
  status: string;
  durationMs: number;
  error?: string;
  retries: number;
}

interface RunSummary {
  startedAt: string;
  finishedAt: string;
  durationMs: number;
  status: 'passed' | 'failed' | 'timedout' | 'interrupted';
  totals: {
    tests: number;
    passed: number;
    failed: number;
    skipped: number;
    flaky: number;
  };
  environment: {
    webBaseUrl: string;
    apiBaseUrl: string;
    identityBaseUrl: string;
  };
  tests: TestRecord[];
  htmlReportPath: string;
}

class RunSummaryReporter implements Reporter {
  private startedAt = new Date();
  private records: TestRecord[] = [];

  onTestEnd(test: TestCase, result: TestResult): void {
    const project = test.parent.project()?.name ?? 'unknown';
    this.records.push({
      title: test.titlePath().join(' › '),
      file: path.relative(process.cwd(), test.location.file),
      project,
      status: result.status,
      durationMs: result.duration,
      retries: result.retry,
      error: result.error?.message
    });
  }

  async onEnd(result: FullResult): Promise<void> {
    const finishedAt = new Date();
    const summary: RunSummary = {
      startedAt: this.startedAt.toISOString(),
      finishedAt: finishedAt.toISOString(),
      durationMs: finishedAt.getTime() - this.startedAt.getTime(),
      status: result.status,
      totals: {
        tests: this.records.length,
        passed: this.records.filter(r => r.status === 'passed').length,
        failed: this.records.filter(r => r.status === 'failed').length,
        skipped: this.records.filter(r => r.status === 'skipped').length,
        flaky: this.records.filter(r => r.status === 'flaky').length
      },
      environment: {
        webBaseUrl: process.env.WEB_BASE_URL ?? 'http://localhost:4200',
        apiBaseUrl: process.env.API_BASE_URL ?? 'https://localhost:7241',
        identityBaseUrl: process.env.IDENTITY_BASE_URL ?? 'https://localhost:7036'
      },
      tests: this.records,
      htmlReportPath: 'reports/html/index.html'
    };

    const reportsDir = path.resolve(process.cwd(), 'reports');
    fs.mkdirSync(reportsDir, { recursive: true });
    fs.writeFileSync(
      path.join(reportsDir, 'summary.json'),
      JSON.stringify(summary, null, 2),
      'utf8'
    );

    this.writeSummaryHtml(summary, reportsDir);
  }

  private writeSummaryHtml(summary: RunSummary, reportsDir: string): void {
    const rows = summary.tests
      .map(test => {
        const statusClass = test.status === 'passed' ? 'pass' : test.status === 'skipped' ? 'skip' : 'fail';
        const error = test.error
          ? `<details><summary>Error</summary><pre>${escapeHtml(test.error)}</pre></details>`
          : '';
        return `<tr class="${statusClass}">
          <td>${escapeHtml(test.title)}</td>
          <td><span class="badge ${statusClass}">${test.status}</span></td>
          <td>${test.durationMs} ms</td>
          <td>${escapeHtml(test.project)}</td>
          <td>${escapeHtml(test.file)}</td>
          <td>${error}</td>
        </tr>`;
      })
      .join('\n');

    const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Aganox Rental UI Test Summary</title>
  <style>
    :root { color-scheme: light dark; font-family: Segoe UI, system-ui, sans-serif; }
    body { margin: 0; background: #0f172a; color: #e2e8f0; }
    .wrap { max-width: 1200px; margin: 0 auto; padding: 2rem 1.5rem 3rem; }
    h1 { margin: 0 0 0.5rem; font-size: 1.75rem; }
    .meta { color: #94a3b8; margin-bottom: 1.5rem; line-height: 1.6; }
    .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 0.75rem; margin-bottom: 1.5rem; }
    .card { background: #1e293b; border: 1px solid #334155; border-radius: 12px; padding: 1rem; }
    .card strong { display: block; font-size: 1.5rem; }
    .links { margin-bottom: 1.5rem; }
    .links a { color: #38bdf8; margin-right: 1rem; }
    table { width: 100%; border-collapse: collapse; background: #1e293b; border-radius: 12px; overflow: hidden; }
    th, td { padding: 0.75rem 0.875rem; border-bottom: 1px solid #334155; vertical-align: top; text-align: left; font-size: 0.9rem; }
    th { background: #111827; text-transform: uppercase; letter-spacing: 0.04em; font-size: 0.75rem; color: #94a3b8; }
    tr.pass td:first-child { border-left: 4px solid #22c55e; }
    tr.fail td:first-child { border-left: 4px solid #ef4444; }
    tr.skip td:first-child { border-left: 4px solid #eab308; }
    .badge { display: inline-block; padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; }
    .badge.pass { background: #14532d; color: #bbf7d0; }
    .badge.fail { background: #7f1d1d; color: #fecaca; }
    .badge.skip { background: #713f12; color: #fde68a; }
    pre { white-space: pre-wrap; word-break: break-word; background: #0b1220; padding: 0.75rem; border-radius: 8px; }
  </style>
</head>
<body>
  <div class="wrap">
    <h1>Aganox Rental UI Test Summary</h1>
    <p class="meta">
      Run status: <strong>${summary.status}</strong><br />
      Started: ${summary.startedAt}<br />
      Finished: ${summary.finishedAt}<br />
      Duration: ${summary.durationMs} ms<br />
      Web: ${summary.environment.webBaseUrl} · API: ${summary.environment.apiBaseUrl}
    </p>
    <div class="cards">
      <div class="card"><strong>${summary.totals.tests}</strong>Total</div>
      <div class="card"><strong>${summary.totals.passed}</strong>Passed</div>
      <div class="card"><strong>${summary.totals.failed}</strong>Failed</div>
      <div class="card"><strong>${summary.totals.skipped}</strong>Skipped</div>
      <div class="card"><strong>${summary.totals.flaky}</strong>Flaky</div>
    </div>
    <div class="links">
      <a href="html/index.html">Open Playwright HTML report</a>
    </div>
    <table>
      <thead>
        <tr>
          <th>Test</th>
          <th>Status</th>
          <th>Duration</th>
          <th>Browser</th>
          <th>Spec file</th>
          <th>Details</th>
        </tr>
      </thead>
      <tbody>${rows}</tbody>
    </table>
  </div>
</body>
</html>`;

    fs.writeFileSync(path.join(reportsDir, 'summary.html'), html, 'utf8');
  }
}

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}

export default RunSummaryReporter;
