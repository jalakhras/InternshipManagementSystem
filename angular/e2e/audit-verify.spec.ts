import { test, Page } from '@playwright/test';

const EXAM = 'bbc19779-e6ba-98a3-c26c-3a235e21a576';
test.use({ storageState: 'e2e/.audit-auth.json' });
test.setTimeout(300_000);

async function theme(page: Page, t: string) {
  await page.addInitScript(v => localStorage.setItem('astro.theme', v), t);
}
async function lang(page: Page, l: 'ar' | 'en') {
  await page.goto('http://localhost:4200/');
  await page.waitForSelector('.lang__option', { timeout: 30_000 });
  const b = page.locator('.lang__option', { hasText: l === 'ar' ? 'العربية' : 'English' });
  if ((await b.getAttribute('aria-pressed')) !== 'true') { await b.click(); await page.waitForTimeout(3500); }
}

const probe = `(() => {
  const out = {};
  const g = (sel, props) => {
    const el = document.querySelector(sel);
    if (!el) return 'MISSING';
    const c = getComputedStyle(el); const r = el.getBoundingClientRect();
    const o = { box: [Math.round(r.width), Math.round(r.height)], top: Math.round(r.top), left: Math.round(r.left) };
    for (const p of props) o[p] = c.getPropertyValue(p);
    return o;
  };
  out.docScrollW = document.documentElement.scrollWidth;
  out.winW = window.innerWidth;
  out._g = g;
  return out;
})()`;

test.describe('verify', () => {
  for (const th of ['light', 'dark'] as const) {
    for (const lg of ['ar', 'en'] as const) {
      test(`verify ${lg}-${th}`, async ({ page }, info) => {
        const vp = info.project.name === 'mobile' ? 'm' : 'd';
        const tag = `${vp}-${lg}-${th}`;
        await theme(page, th);
        await lang(page, lg);

        // ---------- exam list probes ----------
        await page.goto('http://localhost:4200/exams');
        await page.waitForTimeout(2000);

        const listProbe = await page.evaluate(() => {
          const px = (el: Element | null, props: string[]) => {
            if (!el) return 'MISSING';
            const c = getComputedStyle(el); const r = el.getBoundingClientRect();
            const o: any = { w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top), left: Math.round(r.left) };
            for (const p of props) o[p] = c.getPropertyValue(p);
            return o;
          };
          const q = (s: string) => document.querySelector(s);
          return {
            docScrollW: document.documentElement.scrollWidth,
            winW: window.innerWidth,
            newExamBtn: px(q('.btn-primary'), ['background-color', 'color', 'border-radius', 'height', 'font-size', 'padding-inline-start']),
            rowAction: px(q('.row-action'), ['border-top-width', 'border-top-style', 'background-color', 'color']),
            rowActionDanger: px(q('.row-action--danger'), ['color', 'border-top-width']),
            segOption: px(q('.segmented__option'), ['height', 'min-height', 'font-size']),
            searchInput: px(q('.search input'), ['height', 'border-radius', 'font-size']),
            titleCell: px(q('.title'), ['direction', 'unicode-bidi', 'text-align']),
            titleText: q('.title')?.textContent?.trim(),
            numHeader: px(document.querySelectorAll('thead th')[3], ['text-align']),
            numCell: px(q('td.num'), ['text-align']),
            sendIcon: px(q('.row-action .bi-send'), ['transform']),
            navSendIcon: px(q('.sidebar__link .bi-send'), ['transform']),
            htmlDir: document.documentElement.dir,
          };
        });
        console.log(`\n### LIST ${tag}`, JSON.stringify(listProbe, null, 1));

        // delete dialog
        await page.locator('.row-action--danger').first().click();
        await page.waitForTimeout(600);
        await page.screenshot({ path: `audit-dlg-${tag}.png` });
        const dlg = await page.evaluate(() => {
          const px = (el: Element | null, props: string[]) => {
            if (!el) return 'MISSING';
            const c = getComputedStyle(el); const r = el.getBoundingClientRect();
            const o: any = { w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top), left: Math.round(r.left) };
            for (const p of props) o[p] = c.getPropertyValue(p);
            return o;
          };
          return {
            confirm: px(document.querySelector('.confirm'), ['background-color', 'border-top-width', 'border-top-style', 'box-shadow', 'color']),
            body: px(document.querySelector('.confirm__body'), ['color']),
            note: px(document.querySelector('.confirm__note'), ['color']),
            btns: [...document.querySelectorAll('.confirm__actions button')].map(b => {
              const c = getComputedStyle(b); const r = b.getBoundingClientRect();
              return { text: b.textContent?.trim(), bg: c.backgroundColor, color: c.color, w: Math.round(r.width), h: Math.round(r.height), left: Math.round(r.left) };
            }),
          };
        });
        console.log(`### DIALOG ${tag}`, JSON.stringify(dlg, null, 1));
        await page.keyboard.press('Escape').catch(() => {});
        await page.locator('.scrim').click({ force: true }).catch(() => {});

        // empty state
        await page.locator('.search input').fill('zzzzzznothing');
        await page.waitForTimeout(2000);
        await page.screenshot({ path: `audit-empty-${tag}.png` });
        const empty = await page.evaluate(() => {
          const el = document.querySelector('.state');
          if (!el) return 'NO .state';
          const c = getComputedStyle(el);
          return { color: c.color, text: (el as HTMLElement).innerText, titleColor: document.querySelector('.state__title') ? getComputedStyle(document.querySelector('.state__title')!).color : null };
        });
        console.log(`### EMPTY ${tag}`, JSON.stringify(empty));

        // error state
        await page.route('**/api/app/exam**', r => r.fulfill({ status: 500, contentType: 'application/json', body: '{"error":{"message":"boom"}}' }));
        await page.goto('http://localhost:4200/exams');
        await page.waitForTimeout(2500);
        await page.screenshot({ path: `audit-error-${tag}.png` });
        const err = await page.evaluate(() => {
          const el = document.querySelector('.state');
          const sp = document.querySelector('.spinner');
          return {
            state: el ? { cls: el.className, text: (el as HTMLElement).innerText, color: getComputedStyle(el).color, iconColor: el.querySelector('i') ? getComputedStyle(el.querySelector('i')!).color : null } : 'none',
            spinner: sp ? { borderW: getComputedStyle(sp).borderTopWidth, borderS: getComputedStyle(sp).borderTopStyle } : 'none',
          };
        });
        console.log(`### ERROR ${tag}`, JSON.stringify(err));
        await page.unroute('**/api/app/exam**');

        // loading state (slow the API)
        await page.route('**/api/app/exam**', async r => { await new Promise(res => setTimeout(res, 4000)); await r.continue(); });
        await page.goto('http://localhost:4200/exams');
        await page.waitForTimeout(1600);
        await page.screenshot({ path: `audit-loading-${tag}.png` });
        const load = await page.evaluate(() => {
          const sp = document.querySelector('.spinner');
          if (!sp) return 'no spinner';
          const c = getComputedStyle(sp); const r = sp.getBoundingClientRect();
          return { w: Math.round(r.width), h: Math.round(r.height), borderTop: c.borderTopWidth + ' ' + c.borderTopStyle + ' ' + c.borderTopColor, bg: c.backgroundColor };
        });
        console.log(`### LOADING ${tag}`, JSON.stringify(load));
        await page.unroute('**/api/app/exam**');

        // ---------- exam form probes ----------
        await page.goto('http://localhost:4200/exams/new');
        await page.waitForTimeout(1500);
        const formProbe = await page.evaluate(() => {
          const px = (el: Element | null, props: string[]) => {
            if (!el) return 'MISSING';
            const c = getComputedStyle(el); const r = el.getBoundingClientRect();
            const o: any = { w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top), left: Math.round(r.left) };
            for (const p of props) o[p] = c.getPropertyValue(p);
            return o;
          };
          const row = document.querySelector('.row-2');
          const cols = row ? [...row.children] : [];
          return {
            docScrollW: document.documentElement.scrollWidth, winW: window.innerWidth,
            save: px(document.querySelector('.head-actions .btn-primary'), ['background-color', 'color', 'height', 'border-radius', 'font-size']),
            cancel: px(document.querySelector('.head-actions a, .head-actions .btn:not(.btn-primary)'), ['background-color', 'color', 'border-top-color', 'height', 'border-radius']),
            headActionsHtml: document.querySelector('.head-actions')?.innerHTML.slice(0, 400),
            row2cols: cols.map(c => { const r = c.getBoundingClientRect(); const lab = c.querySelector('.form-label'); const inp = c.querySelector('input'); return { colTop: Math.round(r.top), labTop: lab ? Math.round(lab.getBoundingClientRect().top) : null, inpTop: inp ? Math.round(inp.getBoundingClientRect().top) : null, inpH: inp ? Math.round(inp.getBoundingClientRect().height) : null }; }),
            toggleInput: px(document.querySelector('.toggle input'), ['width', 'height', 'accent-color']),
            toggleBox: px(document.querySelector('.toggle'), ['height']),
            hintMax: px(document.querySelector('.hint'), ['max-inline-size', 'font-size', 'color']),
          };
        });
        console.log(`### EXAMFORM ${tag}`, JSON.stringify(formProbe, null, 1));

        // ---------- question builder ----------
        for (const [name, idx] of [['written', 3], ['numeric', 4]] as const) {
          await page.goto(`http://localhost:4200/exams/${EXAM}/questions/new`);
          await page.waitForSelector('.type', { timeout: 20_000 });
          await page.locator('.type').nth(idx as number).click();
          await page.waitForTimeout(1000);
          await page.screenshot({ path: `audit-q2-${name}-${tag}.png`, fullPage: true });
        }

        // single choice + weighted on
        await page.goto(`http://localhost:4200/exams/${EXAM}/questions/new`);
        await page.waitForSelector('.type', { timeout: 20_000 });
        await page.locator('.type').nth(0).click();
        await page.waitForTimeout(900);

        const qProbe = await page.evaluate(() => {
          const px = (el: Element | null, props: string[]) => {
            if (!el) return 'MISSING';
            const c = getComputedStyle(el); const r = el.getBoundingClientRect();
            const o: any = { w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top), left: Math.round(r.left) };
            for (const p of props) o[p] = c.getPropertyValue(p);
            return o;
          };
          const row = document.querySelector('.row-3');
          const cols = row ? [...row.children] : [];
          return {
            toolbar: px(document.querySelector('.toolbar'), ['border-top-width', 'border-top-style', 'background-color']),
            toolbarBtn: px(document.querySelector('.toolbar__button'), ['width', 'height', 'color']),
            surface: px(document.querySelector('.surface'), ['border-top-width', 'border-top-color', 'min-height']),
            optionText: px(document.querySelector('.option__text'), ['height', 'min-height', 'background-color', 'border-top-color']),
            optionRemove: px(document.querySelector('.option__remove'), ['width', 'height', 'color']),
            optionMarkInput: px(document.querySelector('.option__mark input'), ['width', 'height']),
            addOption: px(document.querySelector('.actions .btn'), ['height', 'background-color', 'color', 'border-top-color', 'font-size']),
            partialCheckbox: px(document.querySelector('.partial input'), ['width', 'height', 'accent-color', 'appearance']),
            partialLabel: px(document.querySelector('.partial'), []),
            actionsRow: px(document.querySelector('.actions'), ['flex-wrap']),
            saveBtn: px(document.querySelector('.form .actions .btn-primary, form .actions .btn'), ['background-color', 'color', 'height']),
            row3cols: cols.map(c => { const r = c.getBoundingClientRect(); const lab = c.querySelector('.form-label'); const ctl = c.querySelector('input, .segmented'); return { colTop: Math.round(r.top), labTop: lab ? Math.round(lab.getBoundingClientRect().top) : null, ctlTop: ctl ? Math.round(ctl.getBoundingClientRect().top) : null, ctlH: ctl ? Math.round(ctl.getBoundingClientRect().height) : null }; }),
            changeTypeLink: px(document.querySelector('.link'), ['height', 'min-height', 'font-size']),
          };
        });
        console.log(`### QFORM ${tag}`, JSON.stringify(qProbe, null, 1));

        // turn on weighted
        await page.locator('.partial input').check();
        await page.waitForTimeout(700);
        await page.screenshot({ path: `audit-weighted-${tag}.png`, fullPage: true });
        const w = await page.evaluate(() => {
          const px = (el: Element | null, props: string[]) => {
            if (!el) return 'MISSING';
            const c = getComputedStyle(el); const r = el.getBoundingClientRect();
            const o: any = { w: Math.round(r.width), h: Math.round(r.height), left: Math.round(r.left) };
            for (const p of props) o[p] = c.getPropertyValue(p);
            return o;
          };
          return {
            band: px(document.querySelector('.option__band'), ['color', 'font-size']),
            of: px(document.querySelector('.option__of'), ['color', 'font-size']),
            weight: px(document.querySelector('.option__weight'), ['height', 'width', 'text-align', 'direction']),
            bandText: document.querySelector('.option__band')?.textContent?.trim(),
            ofText: document.querySelector('.option__of')?.textContent?.trim(),
            optionHtml: document.querySelector('.option')?.innerHTML.replace(/\s+/g, ' ').slice(0, 600),
            bodyColor: getComputedStyle(document.body).color,
          };
        });
        console.log(`### WEIGHTED ${tag}`, JSON.stringify(w, null, 1));

        // try to save empty -> validation error styling
        await page.locator('form .btn-primary, form button[type=submit]').first().click().catch(() => {});
        await page.waitForTimeout(1500);
        await page.screenshot({ path: `audit-qerror-${tag}.png`, fullPage: true });
        const ve = await page.evaluate(() => {
          const a = document.querySelector('.alert');
          return a ? { cls: a.className, text: (a as HTMLElement).innerText.slice(0, 200), bg: getComputedStyle(a).backgroundColor, color: getComputedStyle(a).color, border: getComputedStyle(a).borderTopWidth + ' ' + getComputedStyle(a).borderTopColor, radius: getComputedStyle(a).borderRadius } : 'no alert';
        });
        console.log(`### QERROR ${tag}`, JSON.stringify(ve));
      });
    }
  }
});
