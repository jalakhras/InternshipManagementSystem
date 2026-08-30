import { expect, test } from '@playwright/test';
import { stubTake } from './support/take-stub';

/**
 * Sitting an exam.
 *
 * This screen is used once, under time pressure, by somebody who cannot come
 * back and try again. Every test here is a way a defect would cost a real person
 * a real mark.
 */
test.describe('Taking an exam', () => {
  const TOKEN = 'link-token';

  test('shows what the exam is before it starts, and costs nothing to look', async ({ page }) => {
    await stubTake(page);
    await page.goto(`/exam/${TOKEN}`);

    // Somebody who clicks a message on a bus to see how long the exam is has not
    // started it, and a product that treats that as a start has taken something
    // from them they cannot get back.
    await expect(page.getByRole('heading', { name: 'Spanish B1 Placement' })).toBeVisible();
    await expect(page.getByText('30 minutes')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Start the exam' })).toBeVisible();

    // No clock anywhere yet.
    await expect(page.getByRole('timer')).toHaveCount(0);
  });

  test('says specifically why a link does not work', async ({ page }) => {
    await stubTake(page, { accessible: false, blockReason: 'This link has already been used twice.' });
    await page.goto(`/exam/${TOKEN}`);

    // "Invalid link" leaves a candidate with nowhere to go. Expired, spent and
    // not yet open are three problems with three different answers.
    await expect(page.getByText('This link has already been used twice.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Start the exam' })).toHaveCount(0);
  });

  test('offers to continue when an attempt is running, and says the clock kept going', async ({ page }) => {
    await stubTake(page, { resumable: true });
    await page.goto(`/exam/${TOKEN}`);

    await expect(page.getByRole('button', { name: 'Continue the exam' })).toBeVisible();
    await expect(page.getByText('the clock has been running since')).toBeVisible();
  });

  test('starts, shows one question, and saves the answer', async ({ page }) => {
    const stub = await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    await expect(page.getByText('Question 1 of 3')).toBeVisible();
    await expect(page.getByRole('timer')).toBeVisible();

    await page.getByText('The level price failed to fall below').click();

    // Saved as it goes: somebody whose connection drops should lose the sentence
    // they were typing, not the hour behind it.
    await expect.poll(() => stub.saved.length).toBeGreaterThan(0);
    expect(stub.saved[0].questionId).toBe('q1');
    expect(stub.saved[0].response).toContain('a');

    await expect(page.getByText('Saved')).toBeVisible();
  });

  test('only ever has one question in the browser', async ({ page }) => {
    await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // The whole point of fetching one at a time. Developer tools show the
    // question in front of them and nothing else.
    const body = await page.locator('body').innerText();

    expect(body).toContain('Question 1');
    expect(body).not.toContain('Question 2:');
    expect(body).not.toContain('Question 3:');
  });

  test('saves the current answer before moving on', async ({ page }) => {
    const stub = await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    await page.getByText('The level price failed to rise above').click();

    // Clicked immediately, before the debounce would have fired. Moving on must
    // never be the thing that loses an answer.
    await page.getByRole('button', { name: 'Next' }).click();

    await expect(page.getByText('Question 2 of 3')).toBeVisible();
    await expect.poll(() => stub.saved.filter(s => s.questionId === 'q1').length).toBeGreaterThan(0);
  });

  test('asks before submitting and counts what is unanswered', async ({ page }) => {
    const stub = await stubTake(page, { totalQuestions: 2 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Question 2 of 2')).toBeVisible();

    await page.getByRole('button', { name: 'Finish' }).click();

    // The count, not a vague warning. Somebody who left two blank on purpose
    // should not be talked out of finishing.
    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toContainText('2 question');
    expect(stub.submitted()).toBe(false);

    await dialog.getByRole('button', { name: 'Submit' }).click();
    await expect.poll(() => stub.submitted()).toBe(true);
  });

  test('a submitted attempt goes to the result rather than back into the paper', async ({ page }) => {
    await stubTake(page, { totalQuestions: 1 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    await expect(page).toHaveURL(/result/);
    await expect(page.locator('.score__value')).toHaveText('80%');
    await expect(page.getByText('Passed', { exact: true })).toBeVisible();
  });

  test('withholds the score while a person still has answers to mark', async ({ page }) => {
    await stubTake(page, { totalQuestions: 1, isFinal: false });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    // A candidate who reads 45% and later receives 68% has been told something
    // untrue, and no explanation afterwards undoes it.
    await expect(page.getByText('Your answers are with a marker')).toBeVisible();
    await expect(page.locator('.score__value')).toHaveCount(0);
  });

  test('reports the result by skill, not only as one number', async ({ page }) => {
    await stubTake(page, { totalQuestions: 1 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    // One percentage tells nobody what to do next. This is what a coordinator
    // places a student on.
    await expect(page.getByText('Reading')).toBeVisible();
    await expect(page.getByText('Listening')).toBeVisible();
  });

  test('names the part of the exam the candidate is in', async ({ page }) => {
    await stubTake(page, {
      sections: [
        { name: 'Listening', questions: 2, instructions: 'Each recording plays once.' },
        { name: 'Grammar', questions: 2, instructions: 'Answer every question.' },
      ],
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    // Somebody who does not know they have moved from listening into grammar
    // does not know the rules moved with them, and cannot tell a coordinator
    // afterwards which part went badly.
    await expect(page.getByRole('heading', { name: 'Listening' })).toBeVisible();
    await expect(page.getByText('Question 1 of 2 in this part')).toBeVisible();

    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Question 2 of 2 in this part')).toBeVisible();

    // The heading changes with the part, not with the question.
    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByRole('heading', { name: 'Grammar' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Listening' })).toHaveCount(0);
  });

  test("shows a part's instructions where it begins, and not on the questions after", async ({ page }) => {
    await stubTake(page, {
      sections: [
        { name: 'Listening', questions: 2, instructions: 'Each recording plays once.' },
        { name: 'Grammar', questions: 1, instructions: 'Answer every question.' },
      ],
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    // Written to be read before the part starts: how many questions, whether the
    // audio plays once, whether they can go back.
    await expect(page.getByText('Before you begin this part')).toBeVisible();
    await expect(page.getByText('Each recording plays once.')).toBeVisible();

    // And gone on the second question. Repeating them is something a candidate
    // has to read past under time pressure.
    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Question 2 of 2 in this part')).toBeVisible();
    await expect(page.getByText('Each recording plays once.')).toHaveCount(0);

    // The next part announces its own.
    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Answer every question.')).toBeVisible();
  });

  test('an undivided exam shows no part heading at all', async ({ page }) => {
    await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // Most exams are one paper. A heading saying so on every one of them is
    // noise, and a candidate reading "in this part" on an exam with no parts is
    // being told about a structure that does not exist.
    await expect(page.getByText('in this part')).toHaveCount(0);
    await expect(page.getByText('Before you begin this part')).toHaveCount(0);
  });

  test('reports the result by part of the exam as well as by skill', async ({ page }) => {
    await stubTake(page, {
      totalQuestions: 1,
      sections: [
        { name: 'Listening', questions: 1 },
        { name: 'Grammar', questions: 1 },
      ],
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Next' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    await expect(page).toHaveURL(/result/);

    // Both breakdowns, not one instead of the other. A topic is what a question
    // measures; a part is what the candidate remembers sitting.
    await expect(page.getByRole('heading', { name: 'By part of the exam' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'By skill' })).toBeVisible();

    // The section figures, not the topic ones — 95 and 35 rather than 80.
    await expect(page.getByText('95%')).toBeVisible();
    await expect(page.getByText('35%')).toBeVisible();
  });

  test('the paper does not scroll sideways', async ({ page }) => {
    await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    const overflows = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );

    expect(overflows).toBe(false);
  });
});
