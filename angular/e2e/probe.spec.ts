import { test } from '@playwright/test';
import { stubAbp, gotoApp } from './support/abp-stub';

test('overflow probe', async ({ page }) => {
  await stubAbp(page, { culture: 'ar' });
  await gotoApp(page);
  const info = await page.evaluate(() => {
    const doc = document.documentElement;
    const out: string[] = [];
    document.querySelectorAll('*').forEach(el => {
      const b = el.getBoundingClientRect();
      if (b.width > 0 && (b.right > doc.clientWidth + 1 || b.left < -1)) {
        const cs = getComputedStyle(el);
        out.push(`${el.tagName}.${(el as HTMLElement).className}|L=${Math.round(b.left)} R=${Math.round(b.right)} pos=${cs.position} tf=${cs.transform}`);
      }
    });
    return { cw: doc.clientWidth, sw: doc.scrollWidth, out: out.slice(0, 8) };
  });
  console.log('OVERFLOW:', JSON.stringify(info, null, 1));
});
