// Takes the README screenshot of a pipeline run.
// Usage: node scripts/shot-pipeline.mjs https://github.com/AdrianDeutsch/ShopGuard/actions/runs/<run id>
import { chromium } from '@playwright/test';
const runUrl = process.argv[2];
if (!runUrl) {
  console.error('Pass the URL of the workflow run to capture.');
  process.exit(1);
}
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 950 } });
await page.goto(runUrl, { waitUntil: 'networkidle' });
// Dismiss the cookie banner if present so it doesn't cover the content.
try { await page.getByRole('button', { name: /accept|alle akzeptieren/i }).first().click({ timeout: 4000 }); } catch {}
await page.waitForTimeout(2500);
await page.screenshot({ path: '../docs/images/pipeline-green.png' });
await browser.close();
console.log('saved pipeline-green.png');
