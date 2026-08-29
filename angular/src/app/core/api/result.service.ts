import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

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
  private readonly http = inject(HttpClient);

  private readonly base = '/api/assessment/results';

  /** The API's origin. RestService knows it; a raw HttpClient call does not. */
  private readonly api = environment.apis.default.url.replace(/\/+$/, '');

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

  /**
   * The same rows as the list, as a file.
   *
   * Fetched rather than linked. A plain `<a href>` to this path resolved against
   * the application instead of the API — different origins — and carried no
   * token even when it did not, so the primary button on the results screen
   * navigated the coordinator to the dashboard and lost their filters.
   */
  exportCsv(input: ResultListRequest): Observable<Blob> {
    const query = new URLSearchParams();

    for (const [key, value] of Object.entries(this.params(input))) {
      if (value !== undefined && value !== null && value !== '') {
        query.set(key, String(value));
      }
    }

    return this.http.get(`${this.api}${this.base}/export?${query.toString()}`, {
      responseType: 'blob',
    });
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
