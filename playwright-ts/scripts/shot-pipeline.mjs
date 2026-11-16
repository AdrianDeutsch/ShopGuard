// Takes the README screenshot of a pipeline run: title, status and the job graph.
// Usage: node scripts/shot-pipeline.mjs https://github.com/AdrianDeutsch/ShopGuard/actions/runs/<run id>
import { chromium } from '@playwright/test';
const runUrl = process.argv[2];
if (!runUrl) {
  console.error('Pass the URL of the workflow run to capture.');
  process.exit(1);
}
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 950 }, deviceScaleFactor: 2 });
await page.goto(runUrl, { waitUntil: 'networkidle' });
// Dismiss the cookie banner if present so it doesn't cover the content.
try { await page.getByRole('button', { name: /accept|alle akzeptieren/i }).first().click({ timeout: 4000 }); } catch {}
await page.waitForTimeout(2500);
// Leave out the site navigation above and the runner notices below the graph.
await page.screenshot({ path: '../docs/images/pipeline-green.png', clip: { x: 0, y: 172, width: 1400, height: 512 } });
await browser.close();
console.log('saved pipeline-green.png');
