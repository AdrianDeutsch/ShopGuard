import { chromium } from '@playwright/test';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 950 } });
await page.goto('https://github.com/AdrianDeutsch/Qa_Automation/actions/runs/27617762662', {
  waitUntil: 'networkidle',
});
// Dismiss the cookie banner if present so it doesn't cover the content.
try { await page.getByRole('button', { name: /accept|alle akzeptieren/i }).first().click({ timeout: 4000 }); } catch {}
await page.waitForTimeout(2500);
await page.screenshot({ path: '../docs/images/pipeline-green.png' });
await browser.close();
console.log('saved pipeline-green.png');
