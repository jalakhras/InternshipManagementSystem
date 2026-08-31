import { TranslateService } from './translate.service';

/**
 * What went wrong, in words somebody can act on.
 *
 * Nineteen screens each carried their own copy of this decision, and every copy
 * ended the same way: fall back to `HttpErrorResponse.message`. That string is
 * written for a developer reading a console —
 *
 *     Http failure response for https://localhost:44373/api/assessment/candidates: 401 Unauthorized
 *
 * — and it was being shown, on a page, to the person trying to get their work
 * done. It names an internal address, quotes a status code, and says nothing
 * about what to do next.
 *
 * The two cases worth naming are the two that actually happen:
 *
 * **401** is the ordinary end of a working session. A coordinator who has been
 * marking for an hour comes back from lunch and every screen refuses them; the
 * fix is to sign in again, and saying so turns a wall into a step.
 *
 * **status 0** is no network at all — the request never left the machine.
 * Reported as an unknown error it sends somebody hunting for a fault in the
 * product that is not there.
 *
 * The server's own message always wins when it sends one: it is written for the
 * reader, in their language, and names the specific thing that happened.
 */
export function failureReason(error: unknown, t: TranslateService['t']): string {
  const problem = error as {
    status?: number;
    error?: { error?: { message?: string } };
  };

  const fromServer = problem?.error?.error?.message;

  if (fromServer) {
    return fromServer;
  }

  if (problem?.status === 401) {
    return t('::Failure:SignedOut');
  }

  if (problem?.status === 0) {
    return t('::Failure:NoNetwork');
  }

  return t('::UnknownError');
}
