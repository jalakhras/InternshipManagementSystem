import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedResult } from './assessment.models';
import {
  ItemAnalysisRow,
  ResultDetail,
  ResultListRequest,
  ResultRow,
  ResultSummary,
} from './result.models';

/**
 * What happened when people sat the exam.
 *
 * The export is deliberately not here. It is a file download rather than data
 * the app renders, and routing bytes through an XHR so the page can rebuild a
 * file the browser already knows how to save is work for no one's benefit — the
 * screen navigates to the URL instead, which is what {@link exportUrl} is for.
 */
@Injectable({ providedIn: 'root' })
export class ResultService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/results';

  getList(input: ResultListRequest): Observable<PagedResult<ResultRow>> {
    return this.rest.request<void, PagedResult<ResultRow>>({
      method: 'GET',
      url: this.base,
      params: this.params(input),
    });
  }

  getSummary(input: ResultListRequest): Observable<ResultSummary> {
    return this.rest.request<void, ResultSummary>({
      method: 'GET',
      url: `${this.base}/summary`,
      params: this.params(input),
    });
  }

  get(attemptId: string): Observable<ResultDetail> {
    return this.rest.request<void, ResultDetail>({
      method: 'GET',
      url: `${this.base}/${attemptId}`,
    });
  }

  getItemAnalysis(examId: string): Observable<ItemAnalysisRow[]> {
    return this.rest.request<void, ItemAnalysisRow[]>({
      method: 'GET',
      url: `${this.base}/item-analysis/${examId}`,
    });
  }

  /** The same filters as the list, as a URL the browser can download directly. */
  exportUrl(input: ResultListRequest): string {
    const query = new URLSearchParams();

    for (const [key, value] of Object.entries(this.params(input))) {
      if (value !== undefined && value !== null && value !== '') {
        query.set(key, String(value));
      }
    }

    return `${this.base}/export?${query.toString()}`;
  }

  private params(input: ResultListRequest): Record<string, unknown> {
    return {
      examId: input.examId,
      candidateGroupId: input.candidateGroupId,
      examFormId: input.examFormId,
      filter: input.filter,
      passedOnly: input.passedOnly,
      awaitingMarking: input.awaitingMarking,
      skipCount: input.skipCount,
      maxResultCount: input.maxResultCount,
    };
  }
}
