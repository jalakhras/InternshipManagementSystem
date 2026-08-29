import { test, Page } from '@playwright/test';

const EXAM = 'bbc19779-e6ba-98a3-c26c-3a235e21a576';
test.use({ storageState: 'e2e/.audit-auth.json' });
test.setTimeout(300_000);

async function setTheme(page: Page, theme: string) {
  await page.addInitScript(t => localStorage.setItem('astro.theme', t), theme);
}

async function setLang(page: Page, lang: 'ar' | 'en') {
  await page.goto('http://localhost:4200/');
  await page.waitForSelector('.lang__option', { timeout: 30_000 });
  const want = lang === 'ar' ? 'العربية' : 'English';
  const btn = page.locator('.lang__option', { hasText: want });
  const active = await btn.getAttribute('aria-pressed');
  if (active !== 'true') {
    await btn.click();
    await page.waitForTimeout(3500);
  }
}

async function shot(page: Page, name: string, full = true) {
  await page.waitForTimeout(900);
  await page.screenshot({ path: `audit-${name}.png`, fullPage: full });
}

for (const theme of ['light', 'dark'] as const) {
  for (const lang of ['ar', 'en'] as const) {
    test(`shots ${lang}-${theme}`, async ({ page }, info) => {
      const vp = info.project.name === 'mobile' ? 'm' : 'd';
      const tag = `${vp}-${lang}-${theme}`;
      await setTheme(page, theme);
      await setLang(page, lang);

      // dashboard
      await page.goto('http://localhost:4200/');
      await shot(page, `dash-${tag}`);

      // exam list
      await page.goto('http://localhost:4200/exams');
      await page.waitForTimeout(1500);
      await shot(page, `list-${tag}`);

      // delete dialog
      const del = page.locator('button', { hasText: /حذف|Delete/ }).first();
      if (await del.count()) {
        await del.click({ force: true }).catch(() => {});
        await page.waitForTimeout(800);
        await shot(page, `dlg-${tag}`, false);
        await page.keyboard.press('Escape');
      }

      // exam new
      await page.goto('http://localhost:4200/exams/new');
      await page.waitForTimeout(1200);
      await shot(page, `new-${tag}`);

      // exam edit
      await page.goto(`http://localhost:4200/exams/${EXAM}`);
      await page.waitForTimeout(1500);
      await shot(page, `edit-${tag}`);

      // question type picker
      await page.goto(`http://localhost:4200/exams/${EXAM}/questions/new`);
      await page.waitForSelector('.type', { timeout: 20_000 });
      await shot(page, `picker-${tag}`);

      // each editor type by index
      const editors: Record<string, number> = { single: 0, multi: 1, written: 3, numeric: 4 };
      const count = await page.locator('.type').count();
      const names = await page.locator('.type__name').allTextContents();
      console.log(`TYPES(${count}):`, JSON.stringify(names));

      for (const [label, idx] of Object.entries(editors)) {
        await page.goto(`http://localhost:4200/exams/${EXAM}/questions/new`);
        await page.waitForSelector('.type', { timeout: 20_000 });
        await page.locator('.type').nth(idx).click();
        await page.waitForTimeout(1200);
        await shot(page, `q-${label}-${tag}`);
      }
    });
  }
}
