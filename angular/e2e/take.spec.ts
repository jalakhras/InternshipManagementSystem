import { expect, test } from '@playwright/test';
import { contrastRatio } from './support/contrast';
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
    // The code the server actually sends, not a ready-made sentence. Feeding
    // this stub a sentence is what hid the defect: the screen printed whatever
    // it was given, so a test handing it English prose reported that a
    // candidate was told something useful — while the product was showing them
    // "IMS:ExamLink:AttemptsExhausted" and leaving them to make of it what they
    // could.
    await stubTake(page, { accessible: false, blockReason: 'IMS:ExamLink:AttemptsExhausted' });
    await page.goto(`/exam/${TOKEN}`);

    // "Invalid link" leaves a candidate with nowhere to go. Expired, spent and
    // not yet open are three problems with three different answers.
    await expect(page.getByText(/no attempts left|used/i)).toBeVisible();
    await expect(page.getByText('IMS:ExamLink')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Start the exam' })).toHaveCount(0);
  });

  test('a reason it cannot read is not printed at the candidate', async ({ page }) => {
    await stubTake(page, { accessible: false, blockReason: 'IMS:Something:NobodyTranslated' });
    await page.goto(`/exam/${TOKEN}`);

    // The half that decides how the fallback should behave. A candidate shown a
    // fragment of our internals is worse off than one told plainly that the
    // exam is not available — so an unreadable code becomes the general
    // sentence, never the code itself.
    await expect(page.getByText('IMS:Something')).toHaveCount(0);
    await expect(page.locator('.reason')).not.toBeEmpty();
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
    //
    // Matched on the whole sentence rather than on "2 question": the number no
    // longer inflects a noun. It read "2 question(s)" in English and
    // "٢ سؤالًا" in Arabic, and the Arabic was wrong — that accusative
    // singular goes with 11-99, where 3-10 takes the plural of paucity. Neither
    // language counts a noun here now, so the sentence is right at every number.
    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toContainText('Questions with no answer: 2');
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

  test('a prepared answer cannot be pasted into the paper', async ({ page }) => {
    await page.context().grantPermissions(['clipboard-read', 'clipboard-write']);
    await stubTake(page, { freeText: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    const box = page.locator('textarea').first();
    await box.click();

    // Put something on the clipboard the way a candidate would, then paste it.
    await page.evaluate(() => navigator.clipboard.writeText('An answer written earlier'));
    await page.keyboard.press('ControlOrMeta+V');

    // The assertion is the field, not the notice: a page that showed the message
    // and pasted anyway would pass a test that only looked for the message.
    await expect(box).toHaveValue('');

    // And it says why. A paste that silently does nothing reads as a broken text
    // box, and somebody under time pressure tries it three more times before
    // deciding the exam itself is broken.
    await expect(page.getByText('Pasting is not available')).toBeVisible();
  });

  test('the candidate can still type their own answer', async ({ page }) => {
    await stubTake(page, { freeText: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const box = page.locator('textarea').first();
    await box.click();
    await page.keyboard.type('Support is where buyers returned.');

    // The half that matters more than the block. Refusing paste is worth
    // nothing if it also makes the box hostile to somebody writing in it.
    await expect(box).toHaveValue('Support is where buyers returned.');
  });

  test('a hotspot question is answered by pointing, not by typing', async ({ page }) => {
    const stub = await stubTake(page, { hotspot: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // The author draws regions on an image; the candidate used to be handed a
    // textarea, which is not an answer to "point at the support level".
    await expect(page.locator('textarea')).toHaveCount(0);

    const frame = page.locator('.hotspot__frame');
    const box = (await frame.boundingBox())!;

    await page.mouse.click(box.x + box.width * 0.25, box.y + box.height * 0.75);

    await expect(page.getByRole('button', { name: 'Next' })).toBeEnabled();
    await expect.poll(() => stub.saved.length, { timeout: 10_000 }).toBeGreaterThan(0);

    // Percentages of the image, which is what the grader reads and what makes a
    // phone and a desktop produce the same answer for the same place.
    const sent = JSON.parse(stub.saved.at(-1)!.response!);

    expect(sent.x).toBeGreaterThan(15);
    expect(sent.x).toBeLessThan(35);
    expect(sent.y).toBeGreaterThan(65);
    expect(sent.y).toBeLessThan(85);
  });

  test('a hotspot can be answered without a mouse at all', async ({ page }) => {
    const stub = await stubTake(page, { hotspot: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // WCAG 2.2 wants a non-pointer path for a single-pointer interaction, and
    // here it is not a convenience: without it a candidate who cannot use a
    // mouse cannot answer the question at all.
    await page.locator('.hotspot__frame').focus();
    await page.keyboard.press('Enter');
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowDown');

    await expect.poll(() => stub.saved.length, { timeout: 10_000 }).toBeGreaterThan(0);

    // Enter starts in the middle; two rights and one down move it from there.
    const sent = JSON.parse(stub.saved.at(-1)!.response!);

    expect(sent.x).toBe(52);
    expect(sent.y).toBe(51);

    // And it is said in words, which is the only feedback somebody using the
    // keyboard gets.
    await expect(page.getByText('You chose 52% across')).toBeVisible();
  });

  test('a hotspot sent from an Arabic page carries the place that was pointed at', async ({ page }) => {
    // The warning this closes was raised and never checked: that a hotspot is
    // drawn mirrored in RTL. The product is Arabic first, so the untested
    // direction was the one nearly every candidate sits in.
    const stub = await stubTake(page, { hotspot: true, culture: 'ar' });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'ابدأ الامتحان' }).click();

    const frame = page.locator('.hotspot__frame');

    await expect(frame).toBeVisible();
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');

    const box = (await frame.boundingBox())!;

    // A quarter of the way in from the image's own left edge, three quarters
    // down — the same click as the English test, in the same screen position.
    await page.mouse.click(box.x + box.width * 0.25, box.y + box.height * 0.75);

    await expect.poll(() => stub.saved.length, { timeout: 10_000 }).toBeGreaterThan(0);

    // And the same answer. An image does not mirror when the text around it
    // does: the author drew the regions on this picture, and 25% across is the
    // same part of the picture in both languages. Read the other way it would
    // arrive as 75% — a different place entirely, and marked wrong for a
    // candidate who pointed at the right one.
    const sent = JSON.parse(stub.saved.at(-1)!.response!);

    expect(sent.x).toBeGreaterThan(15);
    expect(sent.x).toBeLessThan(35);
    expect(sent.y).toBeGreaterThan(65);
    expect(sent.y).toBeLessThan(85);

    // Where the ring is *drawn* in Arabic is checked separately, further down —
    // that half was the visible defect. This half is the one nobody would have
    // seen: the answer that reaches the grader.
  });

  test('a code question hands the candidate the template its author wrote', async ({ page }) => {
    const stub = await stubTake(page, {
      code: {
        language: 'Python',
        starterTemplate: 'def total(prices):\n    # your work here\n    return 0',
      },
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    const box = page.locator('textarea');

    // The author wrote a skeleton, the server put it in the projection and sent
    // it, and the client dropped it: the box came up empty. Work a person did,
    // delivered to the browser, and thrown away one step short of the person it
    // was for.
    await expect(box).toHaveValue(/def total\(prices\)/);
    await expect(page.getByText('started you off with the template')).toBeVisible();

    // And which language, which was sent and never shown. "Write it in Python"
    // is not decoration when the answer is marked as text.
    await expect(page.getByText('In Python')).toBeVisible();
  });

  test('a code box is a code box, and stays one in Arabic', async ({ page }) => {
    await stubTake(page, {
      culture: 'ar',
      code: { language: 'JavaScript', starterTemplate: 'if (x > 0) {\n}' },
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'ابدأ الامتحان' }).click();

    const box = page.locator('textarea');

    await expect(box).toBeVisible();

    const style = await box.evaluate(el => {
      const s = getComputedStyle(el);
      return { direction: s.direction, family: s.fontFamily, spellcheck: el.getAttribute('spellcheck') };
    });

    // Code is left to right whatever the page around it does. In an Arabic page
    // a plain box reorders it while the candidate types — `if (x > 0) {` comes
    // out with its brackets swapped — and the authoring form already knew this:
    // the author's two code boxes carry the same class. The candidate's was the
    // only box in the product that did not.
    expect(style.direction).toBe('ltr');

    // Monospace, because columns are meaning in code. And no spellchecker
    // underlining every identifier in a language it does not know.
    expect(style.family.toLowerCase()).toContain('mono');
    expect(style.spellcheck).toBe('false');
  });

  test('a code question says whether to write the program or what it prints', async ({ page }) => {
    await stubTake(page, { code: { expectsOutput: true, language: 'C#' } });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    // The one with a score attached. This question is marked by comparing the
    // candidate's text with what the author said the program should print — so
    // a candidate who writes the program scores nothing, for reading the box
    // the other way rather than for being wrong. The author is told which of
    // the two questions they wrote, on the form, while writing it.
    await expect(page.getByText('Write what the program prints')).toBeVisible();
    await expect(page.getByText('Write your code in the box below')).toHaveCount(0);
  });

  test('a code question that goes to a person asks for the code itself', async ({ page }) => {
    await stubTake(page, { code: { expectsOutput: false, language: 'C#' } });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    // The other half. A question asking for an approach has no single output
    // and goes to a marker, and telling that candidate to write what it prints
    // would be the same defect pointing the other way.
    await expect(page.getByText('Write your code in the box below')).toBeVisible();
    await expect(page.getByText('Write what the program prints')).toHaveCount(0);
  });

  test('a template shown is not an answer given', async ({ page }) => {
    const stub = await stubTake(page, {
      code: { starterTemplate: 'def total(prices):\n    return 0' },
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.locator('textarea')).toHaveValue(/def total/);

    // Filling the box for somebody is not the same as answering for them. If
    // the template were saved on sight, a question nobody had touched would be
    // ticked on the map and counted in the tally — and a candidate deciding
    // what to go back to would be reading a list that is not true.
    await page.waitForTimeout(1500);

    expect(stub.saved.length).toBe(0);

    // And once they do type, what is saved is the whole box: their work with
    // the author's skeleton around it, which is what the marker has to read.
    await page.locator('textarea').fill('def total(prices):\n    return sum(prices)');

    await expect.poll(() => stub.saved.length, { timeout: 10_000 }).toBeGreaterThan(0);
    expect(stub.saved.at(-1)!.response).toContain('sum(prices)');
  });

  test('a candidate is told what their written answer is marked on', async ({ page }) => {
    await stubTake(page, {
      freeText: true,
      rubric: [
        { name: 'Structure', maxScore: 4 },
        { name: 'Evidence', maxScore: 4 },
        { name: 'Language', maxScore: 2 },
      ],
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // The server has sent these for as long as the question type has existed,
    // and its own comment says the names and weights "help the taker aim". No
    // component read them — so on exactly the questions where aiming matters
    // most, the ones a person marks by hand, the candidate was guessing.
    await expect(page.getByText('Your answer is marked on')).toBeVisible();

    await expect(page.getByText('Structure')).toBeVisible();
    await expect(page.getByText('4 marks').first()).toBeVisible();
    await expect(page.getByText('Language')).toBeVisible();
    await expect(page.getByText('2 marks')).toBeVisible();
  });

  test('a question with no rubric shows no empty heading', async ({ page }) => {
    await stubTake(page, { freeText: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // Most questions are not marked against a rubric, and a heading with
    // nothing under it reads as a fault — the candidate looks for the list that
    // is missing rather than getting on with the answer.
    await expect(page.getByText('Your answer is marked on')).toHaveCount(0);
  });

  test('the rubric is above the box, where it can still be aimed at', async ({ page }) => {
    await stubTake(page, {
      freeText: true,
      rubric: [{ name: 'Structure', maxScore: 4 }],
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const rubric = (await page.locator('.rubric').boundingBox())!;
    const box = (await page.locator('textarea').boundingBox())!;

    // A candidate reads down to the place they write. A rubric under the box is
    // read after the answer is written, which is too late to have aimed at it.
    expect(rubric.y).toBeLessThan(box.y);
  });

  test('a file is the answer, and the answer carries it', async ({ page }) => {
    const stub = await stubTake(page, { fileUpload: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await expect(page.getByText('Question 1 of 3')).toBeVisible();

    // The type existed, the marker's screen already showed an attached file, and
    // the candidate was handed a textarea — so the one thing the question asked
    // for was the one thing they could not do.
    await expect(page.locator('textarea')).toHaveCount(0);

    await page.locator('input[type=file]').setInputFiles({
      name: 'my-work.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('a scanned worksheet'),
    });

    // Confirmed to the candidate. An upload with no confirmation cannot be told
    // apart from one that failed, and this is somebody who cannot try again
    // afterwards.
    await expect(page.getByText('We have: my-work.pdf')).toBeVisible();

    await expect.poll(() => stub.saved.length, { timeout: 10_000 }).toBeGreaterThan(0);

    // Storing the bytes is half of it: the answer has to carry the name, or the
    // marker sees nothing.
    expect(stub.saved.at(-1)!.answerBlobName).toBe('tenant/answers/a1/stored-file.pdf');
    expect(stub.saved.at(-1)!.answerFileName).toBe('my-work.pdf');
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

  test('in the dark, a candidate can read the answer they chose', async ({ page }) => {
    await stubTake(page);

    // The theme a person actually chose, not the one the machine defaults to.
    await page.addInitScript(() => localStorage.setItem('astro.theme', 'dark'));

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const choice = page.getByText('The level price failed to fall below');
    await choice.click();

    // 1.01:1 when this was written — the pale panel behind the chosen answer was
    // a fixed colour that never flipped, while the text on it was a token that
    // did. The candidate had to deselect their answer to read it back.
    //
    // "Visible" is not the question: a visibility assertion returns true for
    // white on white. The question is whether there is anything to see.
    const ratio = await contrastRatio(choice);

    expect(ratio, 'the chosen answer must be readable').toBeGreaterThanOrEqual(4.5);
  });

  test('in the dark, a candidate can read whether they passed', async ({ page }) => {
    await stubTake(page, { totalQuestions: 1 });
    await page.addInitScript(() => localStorage.setItem('astro.theme', 'dark'));

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    await expect(page).toHaveURL(/result/);

    // 1.06:1 when this was written. It is the one sentence on the page: the
    // whole reason the candidate sat the exam, and they could not read it.
    const ratio = await contrastRatio(page.locator('.score__verdict'));

    expect(ratio, 'the verdict must be readable').toBeGreaterThanOrEqual(4.5);
  });

  test('the hotspot picture is fetched from the API, not from the app', async ({ page }) => {
    // A server-relative path, which is what the server actually sends. Every
    // other test here uses the stub's inline `data:` URI, and that is why this
    // defect survived: a `data:` URI needs no resolving, so a binding that never
    // made the path absolute still drew a picture in every test.
    await stubTake(page, {
      hotspot: true,
      hotspotImageUrl: '/api/assessment/media/tenant/picture.png',
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const src = await page.locator('.hotspot__image').getAttribute('src');

    expect(src, 'the picture must be fetched from the API').toContain(
      '/api/assessment/media/tenant/picture.png',
    );

    // Left relative, it resolves against the app's own origin — where the file
    // is not — and a candidate opens a hotspot question to an empty frame with
    // nothing to point at.
    expect(src, 'a relative path resolves against the app, not the API').toMatch(/^https?:\/\//);
  });

  test('an inline picture is left exactly as it is', async ({ page }) => {
    // The other half of making the path absolute, and it is not hypothetical:
    // applying the helper to this binding broke every inline image on the first
    // attempt, because the API origin was prepended to a complete `data:` URI
    // and the result fetched nothing. An existing test caught it; this one names
    // the rule so the next person does not have to rediscover it.
    await stubTake(page, { hotspot: true });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const src = await page.locator('.hotspot__image').getAttribute('src');

    expect(src, 'an address that carries its own scheme is already an address')
      .toMatch(/^data:image\//);
  });

  test('the map shows what is left and reaches it', async ({ page }) => {
    await stubTake(page, { totalQuestions: 3 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const map = page.getByRole('navigation', { name: 'Question map' });
    await expect(map).toBeVisible();

    // Three questions, none answered yet, and the first is the one on screen.
    await expect(map.getByRole('button')).toHaveCount(3);
    await expect(map.getByRole('button', { name: 'Question 1 — no answer' })).toHaveAttribute(
      'aria-current',
      'step',
    );

    await page.getByText('The level price failed to fall below').click();
    await expect(page.getByText('Saved')).toBeVisible();

    // The state the server already sent on every exchange, and which nothing
    // drew: answered or not, per question, without revealing anything about them.
    await expect(map.getByRole('button', { name: 'Question 1 — answered' })).toBeVisible();

    // And it moves. The submit dialog could say two questions had no answer and
    // offer no way to reach either, with the clock running.
    await map.getByRole('button', { name: 'Question 3 — no answer' }).click();

    await expect(page.getByText('Question 3 of 3')).toBeVisible();
  });

  test('a paper that forbids going back shows the map but does not move you', async ({ page }) => {
    await stubTake(page, { totalQuestions: 3, allowBackNavigation: false });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const map = page.getByRole('navigation', { name: 'Question map' });

    // The half that keeps the other half safe. Jumping forward on a paper with
    // no way back would strand every question it skipped — a control that
    // quietly costs marks. So the map still shows progress and says why it does
    // not move you, because a row of numbers that ignores a press reads as
    // broken rather than deliberate.
    await expect(map.getByRole('button').first()).toBeDisabled();
    await expect(map.getByText(/does not allow going back/)).toBeVisible();
  });

  test('the map is readable in the dark too', async ({ page }) => {
    await stubTake(page, { totalQuestions: 3 });
    await page.addInitScript(() => localStorage.setItem('astro.theme', 'dark'));

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByText('The level price failed to fall below').click();

    const map = page.getByRole('navigation', { name: 'Question map' });

    // Written the day two other things on this same screen measured 1.01:1 and
    // 1.06:1 in the dark. A new control on the candidate's paper gets checked
    // before it ships, not after somebody cannot read it.
    const answered = await contrastRatio(map.getByRole('button', { name: /answered/ }));
    const blank = await contrastRatio(map.getByRole('button', { name: /no answer/ }).first());

    expect(answered, 'an answered question must be readable').toBeGreaterThanOrEqual(4.5);
    expect(blank, 'an unanswered question must be readable').toBeGreaterThanOrEqual(4.5);
  });


  test('the submit dialog behaves like a dialog', async ({ page }) => {
    await stubTake(page, { totalQuestions: 1 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();

    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toBeVisible();

    // It declared `aria-modal="true"` and honoured none of it: focus never
    // entered, Escape did nothing, Tab walked straight out into the paper
    // behind. A candidate under a running clock who reached this dialog by
    // keyboard could neither confirm nor cancel nor get out of it.
    const inside = await dialog.evaluate(box => box.contains(document.activeElement));
    expect(inside, 'focus must move into the dialog').toBe(true);

    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
  });

  test('the clock says the time, and does not shout it every second', async ({ page }) => {
    await stubTake(page, { secondsRemaining: 1800 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    const clock = page.getByRole('timer');

    // The name used to be an aria-label reading "Time left", which *replaced*
    // the digits rather than describing them — so the one thing this element
    // exists to say was the one thing a screen reader never said.
    await expect(clock).toHaveAccessibleName(/Time remaining.*\d/);

    // And `role="timer"` is given an implicit `aria-live="off"` by the spec for
    // a reason: a value that changes every second floods the buffer. Overriding
    // it to polite made this clock the only thing a candidate could hear, over
    // the question they were trying to read.
    await expect(clock).not.toHaveAttribute('aria-live', 'polite');
  });

  test('running low is said in words, not only in a colour', async ({ page }) => {
    await stubTake(page, { secondsRemaining: 200 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();

    // A candidate who cannot tell amber from grey was told nothing at all.
    await expect(
      page.getByRole('status').filter({ hasText: /minute/i }),
    ).toHaveCount(1);
    await expect(page.getByText('Time is short')).toBeVisible();
  });

  test('a candidate bounced to the root lands back on their own link', async ({ page }) => {
    await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await expect(page.getByRole('button', { name: 'Start the exam' })).toBeVisible();

    // Exactly the bounce this repairs: ABP's OAuth bootstrap meets a staff
    // session it cannot refresh, starts a code flow whose redirect_uri is the
    // app root, and the still-valid sign-in cookie completes the round trip in
    // silence. The candidate arrives at the dashboard having done nothing wrong,
    // with no error anywhere and no way back.
    await page.goto('/');

    await expect(page).toHaveURL(new RegExp(`/exam/${TOKEN}`), { timeout: 20_000 });
    await expect(page.getByRole('button', { name: 'Start the exam' })).toBeVisible();
  });

  test('somebody who leaves an exam link on purpose is left alone', async ({ page }) => {
    await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await expect(page.getByRole('button', { name: 'Start the exam' })).toBeVisible();

    // The half that keeps the repair from becoming a trap. It only ever mends a
    // redirect that has just happened; a coordinator who checks a link and then
    // deliberately goes to the dashboard must not be dragged back to it.
    await page.evaluate(() => {
      const kept = sessionStorage.getItem('astro.takerReturn');

      if (kept) {
        const parsed = JSON.parse(kept) as { at: number; url: string };
        sessionStorage.setItem(
          'astro.takerReturn',
          JSON.stringify({ ...parsed, at: parsed.at - 120_000 }),
        );
      }
    });

    await page.goto('/');

    await expect(page).not.toHaveURL(new RegExp(`/exam/${TOKEN}`));
  });

  test('the candidate reads what their marker wrote', async ({ page }) => {
    await stubTake(page, {
      totalQuestions: 1,
      feedback: ['Link the idea to the evidence in the third line.'],
    });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    // The marking screen has always said this box is shown to the candidate
    // with their result. It was stored and carried nowhere, so every marker who
    // took the trouble to write something wrote it to nobody.
    await expect(page.getByText('Link the idea to the evidence in the third line.')).toBeVisible();
  });

  test('a candidate nobody wrote to sees no empty heading', async ({ page }) => {
    await stubTake(page, { totalQuestions: 1 });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: 'Start the exam' }).click();
    await page.getByRole('button', { name: 'Finish' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Submit' }).click();

    await expect(page.locator('.score__value')).toBeVisible();

    // A block with nothing in it reads as a fault, and most sittings carry no
    // written feedback at all.
    await expect(page.locator('.feedback')).toHaveCount(0);
  });

  test('the candidate panels are laid out by this product, not by Bootstrap', async ({ page }) => {
    await stubTake(page);

    await page.goto(`/exam/${TOKEN}`);
    await expect(page.getByRole('button', { name: 'Start the exam' })).toBeVisible();

    // Bootstrap is loaded and owns `.card`. These panels used to carry that
    // name while declaring no display of their own, so Bootstrap's
    // `display: flex` won and the layout of the first screen a candidate ever
    // sees came from a stylesheet nobody here wrote. It read as correct wherever
    // flex happened to resemble block, which is why nobody caught it by looking.
    //
    // Measured rather than read: the class name can be renamed back by accident,
    // and only the computed value says who is in charge.
    await expect(page.locator('.card')).toHaveCount(0);

    const display = await page
      .locator('.sheet')
      .first()
      .evaluate(el => getComputedStyle(el).display);

    expect(display).not.toBe('flex');
  });

  test('in Arabic the mark lands where the candidate pointed', async ({ page }) => {
    await stubTake(page, { hotspot: true, culture: 'ar' });

    await page.goto(`/exam/${TOKEN}`);
    await page.getByRole('button', { name: /ابدأ|Start/ }).click();

    const frame = page.locator('.hotspot__frame');
    const box = (await frame.boundingBox())!;

    // A quarter of the way in from the left of the picture.
    const at = { x: box.x + box.width * 0.25, y: box.y + box.height * 0.5 };
    await page.mouse.click(at.x, at.y);

    const mark = (await page.locator('.hotspot__mark').boundingBox())!;
    const centre = mark.x + mark.width / 2;

    // Measured, not read. The point is a percentage from the left of the image,
    // and the marker was placed with a logical property — which measures from
    // the right in Arabic. So the ring appeared mirrored across the picture: a
    // candidate pointing at the correct place saw it land somewhere else, while
    // their answer was recorded where they had actually pointed.
    //
    // An image does not mirror when the page does. A diagram of a heart has the
    // aorta where it has it, in both languages.
    expect(Math.abs(centre - at.x)).toBeLessThan(12);
  });
});
