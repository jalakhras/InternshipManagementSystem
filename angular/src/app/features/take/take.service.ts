import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AttemptResult,
  AttemptState,
  ExamPreview,
  IntegritySignalType,
  SaveAnswer,
  SaveAnswerResult,
  TakerQuestion,
} from './take.models';

/**
 * The candidate's side of an exam.
 *
 * Deliberately not built on ABP's RestService like every other service here.
 * That one attaches the staff OAuth token and expects a logged-in user; a
 * candidate has no account and never will. What they hold instead is a
 * short-lived session credential minted for one attempt, and it travels in its
 * own header so it cannot be confused with an identity.
 *
 * The token lives in memory rather than in storage. It is worth one attempt for
 * a few hours, and leaving it in localStorage would let the next person on a
 * shared computer resume somebody else's exam.
 */
@Injectable({ providedIn: 'root' })
export class TakeService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apis.default.url}/api/assessment/take`;

  private readonly session = signal<string | null>(null);

  /** True once a link has been opened and accepted. */
  readonly hasSession = () => this.session() !== null;

  setSession(token: string | null): void {
    this.session.set(token);
  }

  /**
   * Opens a link without consuming an attempt.
   * <p>
   * Separate from starting on purpose: a candidate should be able to look at
   * what they are about to sit — how long, how many questions, how many attempts
   * they have left — without that look costing them one.
   * </p>
   */
  open(token: string): Observable<ExamPreview> {
    return this.http.get<ExamPreview>(`${this.base}/${encodeURIComponent(token)}`);
  }

  /** Starts or resumes. This is where the clock begins. */
  start(): Observable<AttemptState> {
    return this.http.post<AttemptState>(`${this.base}/start`, {}, { headers: this.headers() });
  }

  getState(): Observable<AttemptState> {
    return this.http.get<AttemptState>(`${this.base}/state`, { headers: this.headers() });
  }

  /**
   * One question at a time.
   * <p>
   * The whole paper is never in the browser, so a candidate with developer tools
   * open still sees only the question in front of them. This is the reason the
   * screen fetches rather than caching the paper up front, and it is worth the
   * extra requests.
   * </p>
   */
  /**
   * One question, by the position a candidate sees.
   *
   * The screen counts from one — "question 3 of 20" — and the paper counts from
   * zero. The conversion lives here, at the boundary, because it went missing:
   * the sitting screen passed its own numbering straight through, so every
   * candidate was served the *second* question first, could never reach the
   * first, and hit "not on this paper" on the last one. The stubbed browser
   * tests answered whatever position they were asked for and saw nothing.
   */
  getQuestion(displayPosition: number): Observable<TakerQuestion> {
    return this.http.get<TakerQuestion>(`${this.base}/question/${displayPosition - 1}`, {
      headers: this.headers(),
    });
  }

  saveAnswer(input: SaveAnswer): Observable<SaveAnswerResult> {
    return this.http.put<SaveAnswerResult>(`${this.base}/answer`, input, {
      headers: this.headers(),
    });
  }

  /**
   * Reports something observed about how an answer arrived.
   * <p>
   * Fire and forget by design: a signal that fails to send must never interrupt
   * somebody sitting an exam. Integrity is the organisation's concern, and the
   * candidate's time is theirs.
   * </p>
   */
  /**
   * Records something the browser noticed. Never blocks, never retries: an
   * observation that fails to arrive is a gap in a report, not a lost answer.
   */
  reportSignal(type: IntegritySignalType, questionId?: string, magnitude?: number): void {
    this.http
      .post(`${this.base}/signal`, { type, questionId, magnitude }, { headers: this.headers() })
      .subscribe({ error: () => undefined });
  }

  submit(): Observable<AttemptResult> {
    return this.http.post<AttemptResult>(`${this.base}/submit`, {}, { headers: this.headers() });
  }

  getResult(): Observable<AttemptResult> {
    return this.http.get<AttemptResult>(`${this.base}/result`, { headers: this.headers() });
  }

  private headers(): Record<string, string> {
    return { 'X-Exam-Session': this.session() ?? '' };
  }
}
