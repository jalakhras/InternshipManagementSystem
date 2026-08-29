import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedResult, PagedRequest } from './assessment.models';

/**
 * Matches the server's CreateAssignmentDto. One person or a whole cohort, never
 * both — the screen makes you choose, because "some of each" is a request nobody
 * can check before sending.
 */
export interface CreateAssignmentDto {
  /**
   * The named paper everybody in this sitting receives, or absent to draw one
   * each.
   *
   * Named on the sitting rather than on the exam or the class, because a sitting
   * is what a paper belongs to: the morning group and the afternoon group are
   * one class and two papers, and a resit is a second sitting.
   */
  examFormId?: string;

  /**
   * Move to the next published paper on each retake, rather than naming one.
   *
   * What makes a second sitting a genuinely different paper without anybody
   * having to remember which one this candidate already had.
   */
  rotateForms?: boolean;

  examId: string;
  candidateId?: string;
  candidateGroupId?: string;
  expiresAt: string;
  maxAttempts: number;
  sendEmail: boolean;
  note?: string;
}

export interface AssignmentRecipient {
  candidateId: string;
  candidateName: string;
  email: string;

  /** The whole link, returned once. It is never retrievable again. */
  url: string;

  emailSent: boolean;
  emailError?: string;
}

export interface AssignmentResult {
  assignmentId: string;
  linksCreated: number;
  emailsSent: number;
  emailsFailed: number;
  recipients: AssignmentRecipient[];
}

export interface ExamLinkDto {
  id: string;
  examId: string;
  candidateId: string;
  candidateName: string;

  /**
   * The first few characters only. The link itself is stored hashed and is
   * unrecoverable — which is the point, and why the send is the one chance to
   * copy it.
   */
  tokenPrefix: string;

  expiresAt: string;
  maxAttempts: number;
  attemptsUsed: number;

  isRevoked: boolean;
  firstOpenedAt?: string;
  emailSentAt?: string;
}

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private readonly rest = inject(RestService);
  private readonly base = '/api/assessment/assignments';

  create(body: CreateAssignmentDto): Observable<AssignmentResult> {
    return this.rest.request<CreateAssignmentDto, AssignmentResult>({
      method: 'POST',
      url: this.base,
      body,
    });
  }

  getLinks(examId: string, input: PagedRequest): Observable<PagedResult<ExamLinkDto>> {
    return this.rest.request<void, PagedResult<ExamLinkDto>>({
      method: 'GET',
      url: `${this.base}/links/${examId}`,
      params: { ...input },
    });
  }

  revoke(linkId: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'POST',
      url: `${this.base}/links/${linkId}/revoke`,
    });
  }
}
