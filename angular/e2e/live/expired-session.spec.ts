import { expect, request as playwrightRequest, test } from '@playwright/test';
import { API } from './api';

/**
 * What a candidate meets when their exam session has ended.
 *
 * The session is the whole of a candidate's credential — they have no account,
 * by design, and the link they were sent is the only thing that identifies
 * them. So the ordinary end of a sitting's session is not an edge case: it is
 * a stale tab, a phone left face-down through lunch, a link opened twice.
 *
 * The answer to it was **302 to `/Account/Login`**. A page asking them to sign
 * in to an account that does not exist for them, mid-exam. It happened because
 * the refusal was thrown as an authorization failure, and an authorization
 * failure on an unauthenticated request makes ASP.NET Core challenge the
 * default scheme — a cookie here — which redirects. Nothing in the code said
 * "send them to sign in"; the exception type said it on the code's behalf.
 *
 * Live on purpose, and this one cannot be asked of anything else. The whole
 * defect lives in the pipeline between the throw and the wire: the exception
 * type, the scheme that gets challenged, the redirect, the status. A unit test
 * calling the method sees the exception and learns nothing about any of it.
 */
test.describe('A candidate whose exam session has ended', () => {
  test('is answered, not sent to a sign-in page they cannot use', async () => {
    const ctx = await playwrightRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      // Not following it is the point: a redirect that the browser swallows is
      // exactly how this hid. The app asked for JSON and got a page of HTML.
      maxRedirects: 0,
    });

    const response = await ctx.post('/api/assessment/take/start', {
      headers: { 'X-Exam-Session': 'not-a-real-session', 'Accept-Language': 'ar' },
    });

    expect(response.status(), 'a redirect here is the defect itself').toBe(403);

    const body = await response.json();

    expect(body.error.code).toBe('IMS:Take:SessionExpired');

    // In their language, and it says what to do rather than what went wrong.
    // The link they already have is the answer, and nothing else is.
    expect(body.error.message).toContain('رابط الامتحان');

    await ctx.dispose();
  });
});
