import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedRequest, PagedResult } from './assessment.models';

export interface ReviewQueueItem {
  attemptId: string;
  candidateName: string;
  examTitle: string;
  submittedAt: string;

  /** How many answers on this attempt still need a person. */
  pendingCount: number;

  /** What the automatic marking has already awarded. Not the final score. */
  provisionalScore: number;
  maxScore: number;

  /** Behavioural observations to weigh, never a verdict. */
  integrityFlagCount: number;
}

export interface RubricCriterion {
  id: string;
  name: string;
  description?: string;
  maxScore: number;
}

export interface ReviewAnswer {
  answerId: string;
  questionId: string;
  questionText: string;
  questionType: string;
  maxScore: number;

  response?: string;
  answerFileUrl?: string;
  answerFileName?: string;

  /** What to look for, so two reviewers agree. Never shown to a candidate. */
  rubric: RubricCriterion[];
  reviewerGuidance?: string;

  correctAnswer?: string;
  explanation?: string;

  awardedScore?: number;
  reviewComment?: string;
  reviewedAt?: string;

  // How the answer arrived. Observations, not accusations.
  wasPasted: boolean;
  timeSpentSeconds?: number;
  keystrokeCount: number;
  backspaceCount: number;
}

/** Mirrors the server's IntegritySignalType. */
export enum IntegritySignalType {
  Paste = 0,
  WindowBlur = 1,
  ImplausibleSpeed = 2,
  NoCorrections = 3,
  DevToolsOpened = 4,
  PageReloaded = 5,
}

export interface GradeAnswerDto {
  answerId: string;
  awardedScore: number;
  rubricScores?: Record<string, number>;
  comment?: string;
}

/**
 * One thing the browser noticed, as the server actually sends it.
 *
 * This declared `kind: string` and `detail`, and the server has always sent
 * `type` and `magnitude` — so every field of it was wrong. Nothing rendered it,
 * which is the only reason it went unnoticed: the marker's screen shows the
 * server's own written observations rather than the raw list. A model that
 * describes a payload nobody reads is a trap for whoever reads it next.
 */
export interface IntegritySignal {
  type: IntegritySignalType;
  questionId?: string;
  occurredAt: string;

  /** Characters pasted, seconds away from the window, and so on. */
  magnitude?: number;
}

export interface IntegrityReport {
  attemptId: string;
  signals: IntegritySignal[];

  /** Descriptive rather than a score: the system reports, a person decides. */
  observations: string[];
}

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly rest = inject(RestService);
  private readonly base = '/api/assessment/review';

  getQueue(input: PagedRequest & { finished?: boolean }): Observable<PagedResult<ReviewQueueItem>> {
    return this.rest.request<void, PagedResult<ReviewQueueItem>>({
      method: 'GET',
      url: `${this.base}/queue`,
      params: { ...input },
    });
  }

  getAnswers(attemptId: string): Observable<ReviewAnswer[]> {
    return this.rest.request<void, ReviewAnswer[]>({
      method: 'GET',
      url: `${this.base}/attempts/${attemptId}`,
    });
  }

  grade(body: GradeAnswerDto): Observable<void> {
    return this.rest.request<GradeAnswerDto, void>({
      method: 'POST',
      url: `${this.base}/grade`,
      body,
    });
  }

  getIntegrity(attemptId: string): Observable<IntegrityReport> {
    return this.rest.request<void, IntegrityReport>({
      method: 'GET',
      url: `${this.base}/attempts/${attemptId}/integrity`,
    });
  }
}
