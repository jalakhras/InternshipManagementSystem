import { expect, test } from '@playwright/test';
import { ALL_POLICIES, gotoApp, stubAbp } from './support/abp-stub';
import { stubQuestions } from './support/question-stub';

/**
 * The four editors that used to be a JSON textarea.
 *
 * The rule these were built to satisfy is the product owner's, and it is
 * absolute: no input anywhere may require programming skill, to write a question
 * or to answer one. A JSON field in front of a language teacher fails that
 * however well it is documented — so the test that matters most here is the last
 * one, which asserts nobody meets JSON on a type this build ships.
 */
test.describe('Question type editors', () => {
  const open = async (page: import('@playwright/test').Page, type: RegExp) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');
    await page.getByRole('button', { name: type }).click();
  };

  test('matching pairs a term with its match on one row', async ({ page }) => {
    await open(page, /Matching/);

    // The pairing is expressed by being on the same row. No id to invent, no
    // arrow to draw.
    await page.getByLabel('Term 1').fill('Support');
    await page.getByLabel('Match 1').fill('A level price has repeatedly failed to fall below');

    await expect(page.getByText('A pair has one side empty')).toBeVisible();

    await page.getByLabel('Term 2').fill('Resistance');
    await page.getByLabel('Match 2').fill('A level price has repeatedly failed to rise above');

    await expect(page.getByText('A pair has one side empty')).toHaveCount(0);
  });

  test('ordering takes the list as the answer, with no positions to type', async ({ page }) => {
    await open(page, /Ordering/);

    await page.getByLabel('Step 1').fill('Identify the trend');
    await page.getByLabel('Step 2').fill('Mark the levels');
    await page.getByRole('button', { name: 'Add step' }).click();
    await page.getByLabel('Step 3').fill('Place the stop');

    // Moving a row is the whole interaction. Asking an author to keep a list and
    // a separate sequence in step is asking them to get it wrong eventually.
    await page.getByRole('button', { name: 'Move up' }).nth(2).click();

    await expect(page.getByLabel('Step 2')).toHaveValue('Place the stop');
    await expect(page.getByLabel('Step 3')).toHaveValue('Mark the levels');
  });

  test('a blank accepts every spelling the author lists', async ({ page }) => {
    await open(page, /Fill in the blanks/);

    await expect(page.getByText('A blank has no accepted answer')).toBeVisible();

    await page.getByLabel('Accepted answers 1').fill('colour | color');

    // Marking "color" wrong because the key said "colour" tests a spelling
    // convention rather than the thing the question asked.
    await expect(page.getByText('A blank has no accepted answer')).toHaveCount(0);
  });

  test('the scale shows the buttons a candidate will see', async ({ page }) => {
    await open(page, /Rating scale/);

    await expect(page.getByText('Neither end is labelled')).toBeVisible();

    await page.getByLabel('Label for the low end').fill('Not at all confident');
    await page.getByLabel('Label for the high end').fill('Completely confident');

    await expect(page.getByText('Neither end is labelled')).toHaveCount(0);

    // Five points for one to five, rendered rather than imagined. An author
    // should never save a question to find out what it looks like.
    await expect(page.locator('.preview__point')).toHaveCount(5);

    await page.getByRole('spinbutton', { name: 'To' }).fill('900');
    await expect(page.getByText('more than twenty points')).toBeVisible();
    await expect(page.locator('.preview__point')).toHaveCount(0);
  });

  test('no shipped question type asks anyone to write JSON', async ({ page }) => {
    await stubAbp(page, { culture: 'en', grantedPolicies: ALL_POLICIES });
    await stubQuestions(page);
    await gotoApp(page, '/exams/11111111-1111-1111-1111-111111111111/questions/new');

    const types = await page.locator('.type').count();
    expect(types).toBe(13);

    for (let i = 0; i < types; i++) {
      await page.locator('.type').nth(i).click();

      // The raw field stays in the frame for a type from a LATER build than this
      // client — the server accepts types it does not know, and the form must not
      // be stricter than the platform. It must not appear for anything we ship.
      const name = await page.locator('.chosen').innerText();
      expect(await page.locator('#rawPayload').count(), `raw JSON shown for ${name}`).toBe(0);

      await page.getByRole('button', { name: 'Change type' }).click();
    }
  });
});
