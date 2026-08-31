import { TranslateService } from '../../core/translate.service';

/**
 * What went wrong, in words a candidate can act on.
 *
 * The server's own message when it sends one: it is written for the person
 * reading it, in their language, and says which of the specific things happened
 * — the link expired, it was revoked, the attempts are used up.
 *
 * And when it sends nothing readable, a sentence rather than Angular's. The
 * fallback used to be `HttpErrorResponse.message`, which reads:
 *
 *     Http failure response for https://localhost:44373/api/assessment/take/result: 401 Unauthorized
 *
 * That is an internal address and a status code on the screen of somebody who
 * has no account, no support desk and nobody to ask. It tells them nothing
 * about the only thing they want to know — whether their exam counted — and it
 * is the shape of message people are taught not to trust.
 *
 * The 401 is the one worth naming: it is what a candidate meets after they
 * submit, close the tab, and come back to the result page later. Their
 * credential lived in that tab. Saying so, and saying to open the link again,
 * turns a dead end into a next step.
 */
export function takerFailure(error: unknown, t: TranslateService['t']): string {
  const problem = error as {
    status?: number;
    error?: { error?: { message?: string } };
  };

  const fromServer = problem?.error?.error?.message;

  if (fromServer) {
    return fromServer;
  }

  if (problem?.status === 401 || problem?.status === 403) {
    return t('::Take:SessionEnded');
  }

  return t('::Take:CouldNotReachUs');
}
