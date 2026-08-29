import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import {
  CreateUpdateExamFormDto,
  CreateUpdateExamSectionDto,
  ExamFormDetailDto,
  ExamFormDto,
  ExamSectionDto,
} from './structure.models';

/**
 * An exam's parts and its papers.
 *
 * Both were finished on the server and had no client at all, which meant a
 * language exam could not be split into grammar and listening, and a named paper
 * could not be created — so the delivery path that serves one had nothing to
 * serve.
 */
@Injectable({ providedIn: 'root' })
export class StructureService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/exam-structure';

  getSections(examId: string): Observable<ExamSectionDto[]> {
    return this.rest.request<void, ExamSectionDto[]>({
      method: 'GET',
      url: `${this.base}/sections/${examId}`,
    });
  }

  createSection(body: CreateUpdateExamSectionDto): Observable<ExamSectionDto> {
    return this.rest.request<CreateUpdateExamSectionDto, ExamSectionDto>({
      method: 'POST',
      url: `${this.base}/sections`,
      body,
    });
  }

  updateSection(id: string, body: CreateUpdateExamSectionDto): Observable<ExamSectionDto> {
    return this.rest.request<CreateUpdateExamSectionDto, ExamSectionDto>({
      method: 'PUT',
      url: `${this.base}/sections/${id}`,
      body,
    });
  }

  deleteSection(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/sections/${id}` });
  }

  getForms(examId: string): Observable<ExamFormDto[]> {
    return this.rest.request<void, ExamFormDto[]>({
      method: 'GET',
      url: `${this.base}/forms/by-exam/${examId}`,
    });
  }

  getForm(id: string): Observable<ExamFormDetailDto> {
    return this.rest.request<void, ExamFormDetailDto>({
      method: 'GET',
      url: `${this.base}/forms/${id}`,
    });
  }

  createForm(body: CreateUpdateExamFormDto): Observable<ExamFormDto> {
    return this.rest.request<CreateUpdateExamFormDto, ExamFormDto>({
      method: 'POST',
      url: `${this.base}/forms`,
      body,
    });
  }

  /**
   * Fills a paper from the blueprint, so an author starts from something rather
   * than an empty list. The same seed produces the same paper twice.
   */
  generateForm(id: string, seed?: number): Observable<ExamFormDetailDto> {
    return this.rest.request<{ seed?: number }, ExamFormDetailDto>({
      method: 'POST',
      url: `${this.base}/forms/${id}/generate`,
      body: { seed },
    });
  }

  setFormQuestions(id: string, questionIds: string[]): Observable<ExamFormDetailDto> {
    return this.rest.request<{ questionIds: string[] }, ExamFormDetailDto>({
      method: 'PUT',
      url: `${this.base}/forms/${id}/questions`,
      body: { questionIds },
    });
  }

  publishForm(id: string): Observable<ExamFormDto> {
    return this.rest.request<void, ExamFormDto>({
      method: 'POST',
      url: `${this.base}/forms/${id}/publish`,
    });
  }

  retireForm(id: string): Observable<ExamFormDto> {
    return this.rest.request<void, ExamFormDto>({
      method: 'POST',
      url: `${this.base}/forms/${id}/retire`,
    });
  }

  deleteForm(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/forms/${id}` });
  }
}
