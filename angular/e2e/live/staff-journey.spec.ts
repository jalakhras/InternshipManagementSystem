import { expect, test } from '@playwright/test';
import { signInThroughTheForm } from './api';

/**
 * The staff half of the product, done by clicking.
 *
 * <para>
 * On 2026-08-30 the product owner reported three journeys broken — exam links,
 * adding a person and sending them an exam, and changing the logo — while 350
 * backend tests, 258 browser tests and 26 live tests were green. None of them
 * was wrong. They were all answering a different question.
 * </para>
 * <para>
 * The stubbed browser suite drives real screens against a stubbed server, so it
 * proves a screen renders and calls something. The live suite drives a real
 * server through `/connect/token` and raw HTTP, so it proves the server works.
 * Neither is a person signing in and clicking, and a defect that lives in the
 * wiring between the two — a control that was never added, a request the screen
 * does not send, a URL built against the wrong origin — is invisible to both.
 * </para>
 * <para>
 * So this suite has one rule: **touch nothing but the screen**. No tokens, no
 * seeded ids, no `send()`. If a coordinator cannot do it by clicking, this fails
 * — which is the whole point, because that is the report we got.
 * </para>
 */
test.describe('What a coordinator can actually do by clicking', () => {
  test.setTimeout(180_000);

  const stamp = () => Date.now().toString().slice(-8);

  test('signing in reaches the product, not a login loop', async ({ page }) => {
    await signInThroughTheForm(page);

    // The plainest possible assertion, and it had none: no test in the repo has
    // ever signed in through this form.
    await expect(page).toHaveURL(/localhost:4200/);
    await expect(page.locator('astro-shell, .shell, nav').first()).toBeVisible({
      timeout: 30_000,
    });
  });

  test('a new person can be added by hand and is there afterwards', async ({ page }) => {
    await signInThroughTheForm(page);

    const name = `Live Clicked ${stamp()}`;
    const email = `ui-clicked-${stamp()}@example.test`;

    await page.goto('/candidates');
    await page.getByRole('button', { name: /Add|إضافة/ }).first().click();

    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 15_000 });

    await dialog.locator('input[name=candidateName]').fill(name);
    await dialog.locator('input[name=candidateEmail]').fill(email);
    await dialog.getByRole('button', { name: /^Save$|^حفظ$/ }).click();

    // Saved, and still there after a reload — a row that only exists in the
    // browser's memory is not a person who has been added. Found by searching,
    // because past one page of people the newest is not on the first one.
    const find = async () => {
      await page.locator('input[type=search]').fill(name);
      await page.locator('input[type=search]').press('Enter');
      await expect(page.getByText(name)).toBeVisible({ timeout: 20_000 });
    };

    await find();

    await page.reload();
    await find();
  });

  test('there is a way to put a person into a group', async ({ page }) => {
    await signInThroughTheForm(page);
    await page.goto('/candidates');

    // Deliberately not asserting a particular control: the question this test
    // asks is whether a coordinator has ANY route to it. If the only way to put
    // somebody in a group is to re-import a spreadsheet, then the journey the
    // owner described has no path through the screens, and that is the finding.
    const routes = page.locator(
      'button:has-text("Group"), button:has-text("مجموعة"), ' +
        'select[name*="roup"], select[name*="ategory"], a:has-text("Groups"), a:has-text("المجموعات")',
    );

    await expect(routes.first()).toBeVisible({ timeout: 20_000 });
  });

  test('a new person, into a class, sent an exam, and the link opens for them', async ({
    page,
    context,
  }) => {
    await signInThroughTheForm(page);

    const mark = stamp();
    const name = `Journey ${mark}`;
    const email = `ui-journey-${mark}@example.test`;

    // ------------------------------------------------------- add the person
    await page.goto('/candidates');
    await page.getByRole('button', { name: /Add|إضافة/ }).first().click();

    const dialog = page.getByRole('dialog');
    await dialog.locator('input[name=candidateName]').fill(name);
    await dialog.locator('input[name=candidateEmail]').fill(email);
    await dialog.getByRole('button', { name: /^Save$|^حفظ$/ }).click();
    await expect(page.getByText(name)).toBeVisible({ timeout: 20_000 });

    // -------------------------------------------------- put them in a class
    // An exam is sent to a class, never to a person, so this step is not
    // optional tidying — it is the only route from "added" to "can be sent
    // anything", and nothing on the candidates screen says so.
    await page.goto('/groups');
    await page.getByRole('button', { name: /^Roll$|^الكشف$/ }).first().click();

    const roll = page.getByRole('dialog');
    await expect(roll).toBeVisible({ timeout: 15_000 });

    await roll
      .locator('li, tr, .roll__item, label')
      .filter({ hasText: name })
      .locator('input[type=checkbox]')
      .first()
      .check();
    await roll.getByRole('button', { name: /^Save$|^حفظ$/ }).click();

    // --------------------------------------------------------- send the exam
    await page.goto('/assignments');
    await page.locator('button, li, [role=button]').filter({ hasText: /Live placement/ }).first().click();
    await page.getByRole('button', { name: /Send this exam|أرسل/ }).click();

    const send = page.getByRole('dialog').or(page.locator('.panel')).first();

    // The same class the person was just added to. Picking any class would
    // pass or fail on whoever happened to be in it.
    await send.locator('select').first().selectOption({ index: 1 });

    // The panel says plainly when a class is empty, and sending to an empty
    // class sends nothing — so assert we are not about to test that instead.
    await expect(send.getByText(/nobody in it yet|لا أحد/)).toHaveCount(0);

    const until = send.locator('input[type=datetime-local]');
    if (await until.count()) {
      await until.fill('2027-01-01T12:00');
    }

    // Off deliberately: a test must not put real mail on the wire.
    const emailToggle = send.locator('input[type=checkbox]');
    if ((await emailToggle.count()) && (await emailToggle.first().isChecked())) {
      await emailToggle.first().uncheck();
    }

    await send.getByRole('button', { name: /Create the links|أنشئ/ }).click();

    // ------------------------------------------- the link a candidate is given
    const shown = page.locator('code.recipient__url, .recipient__url').first();
    await expect(shown).toBeVisible({ timeout: 30_000 });

    const url = (await shown.textContent())?.trim();

    expect(url, 'the screen must show a link, not a fragment of one').toMatch(
      /^https?:\/\/[^ ]+\/exam\/[A-Za-z0-9_-]+$/,
    );

    // ------------------------------------------------ open it as a stranger
    const stranger = await context.browser()!.newContext({ ignoreHTTPSErrors: true });
    const theirs = await stranger.newPage();

    await theirs.goto(url!);

    // Not a 404 and not a redirect to a login page: a candidate who needs an
    // account is not a candidate, and the link is their whole credential.
    await expect(theirs.getByRole('button', { name: /Start|ابدأ/ })).toBeVisible({
      timeout: 30_000,
    });

    await stranger.close();
  });

  test('an exam link still opens in a browser that holds a stale staff session', async ({
    page,
    context,
  }) => {
    await signInThroughTheForm(page);

    const mark = stamp();
    const name = `Stale ${mark}`;

    await page.goto('/candidates');
    await page.getByRole('button', { name: /Add|إضافة/ }).first().click();

    const dialog = page.getByRole('dialog');
    await dialog.locator('input[name=candidateName]').fill(name);
    await dialog.locator('input[name=candidateEmail]').fill(`ui-stale-${mark}@example.test`);
    await dialog.getByRole('button', { name: /^Save$|^حفظ$/ }).click();
    await expect(dialog).toBeHidden({ timeout: 20_000 });

    // Searched rather than scanned: once a centre has more than a page of people
    // the newest one is not on the first page, and a test that only looks at page
    // one starts failing for a reason that has nothing to do with what it tests.
    await page.locator('input[type=search]').fill(name);
    await page.locator('input[type=search]').press('Enter');

    await expect(page.getByText(name)).toBeVisible({ timeout: 20_000 });

    await page.goto('/groups');
    await page.getByRole('button', { name: /^Roll$|^الكشف$/ }).first().click();

    const roll = page.getByRole('dialog');
    await roll
      .locator('li, tr, .roll__item, label')
      .filter({ hasText: name })
      .locator('input[type=checkbox]')
      .first()
      .check();
    await roll.getByRole('button', { name: /^Save$|^حفظ$/ }).click();

    await page.goto('/assignments');
    await page.locator('button, li, [role=button]').filter({ hasText: /Live placement/ }).first().click();
    await page.getByRole('button', { name: /Send this exam|أرسل/ }).click();

    const send = page.getByRole('dialog').or(page.locator('.panel')).first();
    await send.locator('select').first().selectOption({ index: 1 });

    const until = send.locator('input[type=datetime-local]');
    if (await until.count()) {
      await until.fill('2027-01-01T12:00');
    }

    const emailToggle = send.locator('input[type=checkbox]');
    if ((await emailToggle.count()) && (await emailToggle.first().isChecked())) {
      await emailToggle.first().uncheck();
    }

    await send.getByRole('button', { name: /Create the links|أنشئ/ }).click();

    const shown = page.locator('code.recipient__url, .recipient__url').first();
    await expect(shown).toBeVisible({ timeout: 30_000 });

    const url = (await shown.textContent())!.trim();

    // Now the situation that actually broke: the same browser, still carrying a
    // staff session, but an expired one. This is the coordinator opening the link
    // the morning after they created it — which is why it read as intermittent.
    const stale = await context.browser()!.newContext({ ignoreHTTPSErrors: true });
    const theirs = await stale.newPage();

    // A real session first, so the storage holds exactly what ABP's bootstrap
    // expects to find. Fabricated tokens do not reproduce this: the bootstrap
    // never attempts a refresh, so the redirect never happens and the test
    // passes for the wrong reason.
    await signInThroughTheForm(theirs);

    await theirs.evaluate(() => {
      // Expired, and a refresh token the server will refuse — which is what a
      // session looks like the next morning.
      localStorage.setItem('expires_at', String(Date.now() - 60_000));
      localStorage.setItem('refresh_token', 'no-longer-valid');
    });

    await theirs.goto(url);

    // ABP's OAuth bootstrap runs on every route, fails to refresh, and starts a
    // code flow whose redirect_uri is the app root — so the deep link is thrown
    // away and the still-valid sign-in cookie lands them on the dashboard. No
    // error is shown anywhere; the link simply does not open.
    await expect(theirs.getByRole('button', { name: /Start|ابدأ/ })).toBeVisible({
      timeout: 30_000,
    });

    expect(theirs.url(), 'the candidate must still be on their link').toContain('/exam/');

    await stale.close();
  });

  test('a refusal says what is wrong, in words, where the person is looking', async ({ page }) => {
    await signInThroughTheForm(page);

    const mark = stamp();
    const email = `ui-twice-${mark}@example.test`;

    const add = async (name: string) => {
      await page.goto('/candidates');
      await page.getByRole('button', { name: /Add|إضافة/ }).first().click();

      const dialog = page.getByRole('dialog');
      await dialog.locator('input[name=candidateName]').fill(name);
      await dialog.locator('input[name=candidateEmail]').fill(email);
      await dialog.getByRole('button', { name: /^Save$|^حفظ$/ }).click();

      return dialog;
    };

    const first = await add(`Twice A ${mark}`);
    await expect(first).toBeHidden({ timeout: 20_000 });

    const dialog = await add(`Twice B ${mark}`);

    // Two things had to be wrong at once for this to read as "it does not work",
    // and both were. The message ABP produced was "an internal error occurred",
    // because the code namespace registered for these errors did not match the
    // `IMS:` prefix the codes carry — 107 written, translated messages, none of
    // them reachable. And the alert rendered on the page behind the dialog's
    // scrim, so even the wrong message was invisible: you pressed Save and
    // nothing happened.
    const alert = dialog.locator('.alert-danger');

    await expect(alert).toBeVisible({ timeout: 20_000 });
    await expect(alert).not.toContainText(/internal error|خطأ داخلي/);
    await expect(alert).toContainText(/email|البريد/i);
  });
});
