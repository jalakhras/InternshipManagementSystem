import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedResult } from './assessment.models';
import { ResultRow } from './result.models';

export interface RunningAttemptRequest {
  examId?: string;
  filter?: string;
  includeExpired?: boolean;
  skipCount: number;
  maxResultCount: number;
}

/**
 * Sittings in progress, and what a coordinator can do about one.
 *
 * A running attempt is described by the same row shape as a finished one, on
 * purpose: two shapes for the same thing is how one of them ends up saying
 * something the other does not.
 */
@Injectable({ providedIn: 'root' })
export class AttemptAdminService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/attempts';

  getRunning(input: RunningAttemptRequest): Observable<PagedResult<ResultRow>> {
    return this.rest.request<void, PagedResult<ResultRow>>({
      method: 'GET',
      url: `${this.base}/running`,
      params: {
        examId: input.examId,
        filter: input.filter,
        includeExpired: input.includeExpired,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
      },
    });
  }

  /** Ends a sitting now. Everything answered so far counts and is marked. */
  forceSubmit(attemptId: string, reason: string): Observable<ResultRow> {
    return this.rest.request<{ reason: string }, ResultRow>({
      method: 'POST',
      url: `${this.base}/${attemptId}/end`,
      body: { reason },
    });
  }

  /** Removes an attempt that should never have counted. Refused once it is marked. */
  delete(attemptId: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/${attemptId}` });
  }
}
