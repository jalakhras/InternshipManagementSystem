import { expect, request, test } from '@playwright/test';
import { API, send, signIn } from './api';

/**
 * Photographs of the candidate's screen, for looking at.
 *
 * Not an assertion suite. Layout is the one thing that cannot be reviewed by
 * reading code, and the exam screen is used once, under time pressure, by
 * somebody who cannot come back — so it is worth being able to see it on demand
 * at the sizes people actually use.
 *
 *   npx playwright test --project=live screenshot
 *
 * Writes to angular/screens/.
 */
test.describe('The candidate\'s screen', () => {
  test.setTimeout(120_000);

  const SIZES = [
    { name: 'desktop', width: 1440, height: 900 },
    { name: 'laptop', width: 1280, height: 800 },
    { name: 'tablet', width: 834, height: 1112 },
    { name: 'phone', width: 390, height: 844 },
  ];

  test('entry, question and result at four sizes', async ({ page }) => {
    const admin = await signIn();
    const ctx = admin.ctx;

    const exams = await send<{ items: { id: string; title: string }[] }>(
      ctx,
      'get',
      '/api/assessment/exams?maxResultCount=10',
    );

    const exam = exams.items[0];

    expect(exam, 'no exam to photograph — run node tools/seed-tenants.js').toBeTruthy();

    const candidate = await send<{ id: string }>(ctx, 'post', '/api/assessment/candidates', {
      fullName: 'شاشة الاختبار',
      email: `screens-${Date.now().toString(36)}@example.test`,
    });

    const sent = await send<{ recipients: { url: string }[] }>(
      ctx,
      'post',
      '/api/assessment/assignments',
      {
        examId: exam.id,
        candidateId: candidate.id,
        expiresAt: new Date(Date.now() + 864e5).toISOString(),
        maxAttempts: 1,
        sendEmail: false,
      },
    );

    const linkToken = sent.recipients[0].url.split('/').pop()!;

    for (const size of SIZES) {
      await page.setViewportSize({ width: size.width, height: size.height });
      await page.goto(`/exam/${linkToken}`);

      await expect(page.getByRole('button', { name: /ابدأ|Start/ })).toBeVisible();
      await page.screenshot({ path: `screens/entry-${size.name}.png`, fullPage: true });
    }

    // Start once, then photograph the paper at each size. Starting per size would
    // burn the single attempt this link carries.
    await page.setViewportSize(SIZES[0]);
    await page.goto(`/exam/${linkToken}`);
    await page.getByRole('button', { name: /ابدأ|Start/ }).click();

    await expect(page.getByRole('timer')).toBeVisible({ timeout: 20_000 });

    for (const size of SIZES) {
      await page.setViewportSize({ width: size.width, height: size.height });
      await page.waitForTimeout(300);

      await page.screenshot({
        path: `screens/question-${size.name}.png`,
        fullPage: true,
      });
    }
  });
});
