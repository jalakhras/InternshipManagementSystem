import { test, expect } from '@playwright/test';

test.use({ storageState: { cookies: [], origins: [] } });

test('recon', async ({ page }) => {
  page.on('console', m => { if (m.type() === 'error') console.log('CONSOLE ERR:', m.text().slice(0, 200)); });

  await page.goto('http://localhost:4200/exams');
  await page.waitForSelector('input[type="password"]', { timeout: 30_000 });
  await page.locator('input[type="text"], input[type="email"]').first().fill('admin@internship.com');
  await page.locator('input[type="password"]').fill('123456Aa@');
  await page.locator('button[type="submit"]').first().click();
  await page.waitForURL(/localhost:4200/, { timeout: 30_000 });
  await page.waitForTimeout(4000);

  console.log('URL:', page.url());
  console.log('DIR:', await page.evaluate(() => document.documentElement.dir), 'LANG:', await page.evaluate(() => document.documentElement.lang));
  console.log('LANG BUTTONS:', await page.locator('.lang__option').allTextContents());
  console.log('SIDEBAR LINKS:', await page.locator('.sidebar__link').allTextContents());

  // exam rows -> get an id
  const html = await page.content();
  const ids = [...html.matchAll(/\/exams\/([0-9a-f-]{36})/g)].map(m => m[1]);
  console.log('EXAM IDS:', [...new Set(ids)].slice(0, 5));
  console.log('ROW COUNT:', await page.locator('tbody tr').count());
  console.log('BODY SNIPPET:', (await page.locator('body').innerText()).slice(0, 1200));

  await page.context().storageState({ path: 'e2e/.audit-auth.json' });
});
