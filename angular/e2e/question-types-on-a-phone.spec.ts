import { expect, Page, test } from '@playwright/test';
import { stubTake } from './support/take-stub';

/**
 * Every question type, on a phone, in Arabic.
 *
 * Four of the thirteen types had ever been put in front of a browser by a test.
 * That is how a code question came to be answered in an essay box for as long
 * as the type had existed, and how the starter template its author wrote
 * reached the candidate's browser and was thrown away one step short of them.
 *
 * These do not check that a type is pretty. They check the four things that
 * decide whether somebody can answer it at all on the device most candidates
 * bring: that the control which *is* the answer exists, that the page does not
 * scroll sideways, that what they must touch is big enough to touch, and that
 * nothing is clipped off the edge of the screen.
 *
 * Arabic, because this product is Arabic first. A control only ever rendered
 * left-to-right in a test has never been checked in the direction nearly every
 * candidate reads.
 */
test.describe('Every question type, on a phone, in Arabic', () => {
  const TOKEN = 'link-token';

  const PHONE = { width: 390, height: 844 };

  /** Type, and the control that has to be there for it to be answerable at all. */
  const TYPES: { type: string; answeredWith: string }[] = [
    { type: 'single-choice', answeredWith: 'input[type=radio]' },
    { type: 'multi-select', answeredWith: 'input[type=checkbox]' },
    { type: 'true-false', answeredWith: 'input[type=radio]' },
    { type: 'text', answeredWith: 'textarea' },
    { type: 'numeric', answeredWith: 'input' },
    { type: 'scale', answeredWith: '[role=radio]' },
    { type: 'ordering', answeredWith: '.question button' },
    { type: 'matching', answeredWith: 'select' },
    { type: 'fill-in-the-blank', answeredWith: 'input' },
    { type: 'code', answeredWith: 'textarea' },
    { type: 'hotspot', answeredWith: '.hotspot__frame' },
    { type: 'file-upload', answeredWith: 'input[type=file]' },
    { type: 'audio-response', answeredWith: 'button' },
  ];

  async function sit(page: Page, type: string): Promise<void> {
    await page.setViewportSize(PHONE);
    await stubTake(page, { culture: 'ar', ofType: type });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'ابدأ الامتحان' }).click();
    await expect(page.locator('.question')).toBeVisible();
  }

  for (const { type, answeredWith } of TYPES) {
    test(`${type} can be answered at 390px`, async ({ page }) => {
      await sit(page, type);

      // The control that is the answer. A type whose input never rendered
      // leaves a candidate reading a question they cannot respond to, with a
      // clock running — and that is exactly what "code" did.
      await expect(page.locator(answeredWith).first()).toBeVisible();
    });

    test(`${type} does not push the page sideways`, async ({ page }) => {
      await sit(page, type);

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );

      // A phone that scrolls sideways hides the thing at the far edge, and the
      // candidate does not know it is there. Wide content is allowed to scroll
      // inside its own box; the page is not.
      expect(overflow).toBeLessThanOrEqual(1);
    });

    test(`${type} can be touched with a finger`, async ({ page }) => {
      await sit(page, type);

      // 44x44, which is this product's own rule and WCAG 2.2's minimum. A target
      // smaller than a fingertip is not a small annoyance in an exam: it is a
      // mis-tap under a clock, and the candidate cannot tell whether the answer
      // they meant was recorded.
      //
      // Measured on what a candidate actually presses. A checkbox or radio is
      // exempt where a label wraps it, because the label is the target — so the
      // label is measured instead.
      const targets = page.locator(
        '.question button, .question select, .question [role=radio], .question label:has(input)',
      );

      const count = Math.min(await targets.count(), 10);

      for (let i = 0; i < count; i++) {
        const box = await targets.nth(i).boundingBox();

        if (!box || box.width === 0) {
          continue;
        }

        expect(Math.round(box.height)).toBeGreaterThanOrEqual(44);
      }
    });

    test(`${type} keeps its controls inside the screen`, async ({ page }) => {
      await sit(page, type);

      const controls = page.locator('.question input, .question textarea, .question select, .question button');
      const count = Math.min(await controls.count(), 8);

      for (let i = 0; i < count; i++) {
        const box = await controls.nth(i).boundingBox();

        if (!box || box.width === 0) {
          continue;
        }

        // Nothing hanging off either edge. In Arabic the edge that fails is the
        // left one, which is the edge nobody checks by habit.
        expect(box.x).toBeGreaterThanOrEqual(-1);
        expect(box.x + box.width).toBeLessThanOrEqual(PHONE.width + 1);
      }
    });
  }

  test('a multi-select says how it will be marked, before it is answered', async ({ page }) => {
    await sit(page, 'multi-select');

    // The grader voids the whole question for one wrong tick. Nothing said so,
    // and the rule decides how a careful person answers: knowing it, they leave
    // the box they are unsure of alone; not knowing it, they tick it.
    //
    // Read from the sentence the candidate actually gets, in their language.
    await expect(page.locator('.choices__rule')).toHaveText(/اختر كلّ الإجابات الصحيحة/);
    await expect(page.locator('.choices__rule')).toContainText('لم تختر خاطئاً');
  });

  test('a single-choice question does not carry a marking rule', async ({ page }) => {
    await sit(page, 'single-choice');

    // Pick one. There is nothing to explain, and a sentence under every question
    // trains people to skip the sentence — which is how the one that matters
    // gets skipped too.
    await expect(page.locator('.choices__rule')).toHaveCount(0);
  });
});
