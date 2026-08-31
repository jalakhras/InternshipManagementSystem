import { expect, test } from '@playwright/test';
import { send, signIn, unique } from './api';

/**
 * Does the invitation actually arrive?
 *
 * The one thing about this product that has never been measured. Every part of
 * the path is covered — the message is built and asserted by unit tests, the
 * link it carries is exercised by dozens more — and the last hop, the one where
 * a real relay either accepts the message or does not, has never run.
 *
 * It is also the hop that matters most. The link inside that email is the
 * candidate's entire credential: there is no account, no password, no other way
 * in. **An invitation that does not arrive is a candidate who cannot sit**, and
 * nothing anywhere in the product would say so — `EmailSentAt` records that we
 * handed the message to a sender, not that anybody received it.
 *
 * Skipped unless somebody deliberately names a recipient:
 *
 *     ASTRO_MAIL_TO=you@example.com npx playwright test e2e/live/mail.spec.ts --project=live
 *
 * That is not shyness about running it. A test that posts real mail on every
 * run is a test that will one day post real mail to a real candidate from a
 * branch nobody meant to run, and the address it would use is whatever row the
 * fixture happened to create.
 */
test.describe('The invitation, actually sent', () => {
  const to = process.env['ASTRO_MAIL_TO'];

  test.skip(!to, 'Set ASTRO_MAIL_TO to the address that should receive it.');

  test('a real relay accepts the invitation and the link inside it works', async () => {
    test.setTimeout(120_000);

    const { ctx } = await signIn();

    // A whole exam, because the message is built from one: its title, its
    // duration, and the organisation's name and colour are all in the body.
    const category = await send<{ id: string }>(ctx, 'post', '/api/assessment/catalog/categories', {
      code: unique('mail'),
      name: 'بريد',
    });

    const exam = await send<{ id: string }>(ctx, 'post', '/api/assessment/exams', {
      title: 'اختبار وصول البريد',
      timeLimitInMinutes: 30,
      passingPercentage: 50,
      categoryId: category.id,
    });

    await send(ctx, 'post', '/api/assessment/questions', {
      examId: exam.id,
      type: 'true-false',
      text: 'وصلت هذه الرسالة.',
      score: 1,
      payload: JSON.stringify({
        options: [
          { id: 'a', text: 'نعم', isCorrect: true },
          { id: 'b', text: 'لا', isCorrect: false },
        ],
      }),
    });

    await send(ctx, 'post', `/api/assessment/exams/${exam.id}/publish`);

    // The exact address, and the same person on every run.
    //
    // Plus-addressing would keep each run's row unique, and a relay sending on a
    // test key refuses it: until a domain is verified the only recipient allowed
    // is the account holder's address, character for character. So the person is
    // looked up first and only created once — which is also closer to life,
    // where a centre sends to somebody who is already on the roll.
    const inbox = to!;

    const existing = await send<{ items: { id: string }[] }>(
      ctx,
      'get',
      `/api/assessment/candidates?filter=${encodeURIComponent(inbox)}&maxResultCount=1`,
    );

    const candidate = existing.items.length > 0
      ? existing.items[0]
      : await send<{ id: string }>(ctx, 'post', '/api/assessment/candidates', {
          fullName: 'المستقبِل',
          email: inbox,
        });

    const assignment = await send<{ recipients: { url: string; emailSent: boolean }[] }>(
      ctx,
      'post',
      '/api/assessment/assignments',
      {
        examId: exam.id,
        candidateId: candidate.id,
        expiresAt: new Date(Date.now() + 7 * 86_400_000).toISOString(),
        maxAttempts: 1,
        sendEmail: true,
      },
    );

    const recipient = assignment.recipients[0];

    // The relay accepted it. This is the assertion that has never run: with no
    // relay configured the send is swallowed by a null sender and this flag is
    // false, and with a misconfigured one the request throws before it.
    expect(recipient.emailSent).toBe(true);

    // And the link inside it opens. A message that arrives carrying a link that
    // does not work is the same failure one step later — and the link is the
    // whole credential, so there is nothing else for the candidate to try.
    const token = recipient.url.split('/').pop()!;

    // Through the same context the rest of the setup used: it is the one that
    // tolerates the development certificate. The endpoint itself is anonymous —
    // the link is the whole credential — so the bearer token it also carries
    // changes nothing.
    const opened = await ctx.get(`/api/assessment/take/${token}`);

    expect(opened.ok()).toBe(true);
    expect((await opened.json()).isAccessible).toBe(true);

    console.log('Sent to ' + inbox + '. Open the inbox: the subject, the sender name and the link are what a candidate sees.');
  });
});
