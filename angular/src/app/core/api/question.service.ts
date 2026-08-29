import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import {
  CreateUpdateQuestionDto,
  PagedResult,
  QuestionDto,
  QuestionListRequest,
  QuestionTypeDescriptor,
  QuestionGroupDto,
  CreateUpdateQuestionGroupDto,
} from './assessment.models';

/**
 * The question bank.
 *
 * Everything here is behind Questions.* on the server, including reads, because a
 * question carries its answer key. What a candidate receives comes from a
 * different endpoint entirely and shares no type with these.
 */
@Injectable({ providedIn: 'root' })
export class QuestionService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/questions';

  getList(input: QuestionListRequest): Observable<PagedResult<QuestionDto>> {
    return this.rest.request<void, PagedResult<QuestionDto>>({
      method: 'GET',
      url: this.base,
      params: {
        examId: input.examId,
        bankOnly: input.bankOnly,
        categoryId: input.categoryId,
        levelId: input.levelId,
        topicId: input.topicId,
        type: input.type,
        difficulty: input.difficulty,
        filter: input.filter,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
      },
    });
  }

  get(id: string): Observable<QuestionDto> {
    return this.rest.request<void, QuestionDto>({ method: 'GET', url: `${this.base}/${id}` });
  }

  create(body: CreateUpdateQuestionDto): Observable<QuestionDto> {
    return this.rest.request<CreateUpdateQuestionDto, QuestionDto>({
      method: 'POST',
      url: this.base,
      body,
    });
  }

  update(id: string, body: CreateUpdateQuestionDto): Observable<QuestionDto> {
    return this.rest.request<CreateUpdateQuestionDto, QuestionDto>({
      method: 'PUT',
      url: `${this.base}/${id}`,
      body,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/${id}` });
  }

  /**
   * The types this server supports.
   *
   * Fetched rather than hard-coded so the picker and the graders cannot disagree,
   * and so a type whose grader was never registered is shown as human-graded —
   * which is how it will actually behave.
   */
  getTypes(): Observable<QuestionTypeDescriptor[]> {
    return this.rest.request<void, QuestionTypeDescriptor[]>({
      method: 'GET',
      url: `${this.base}/types`,
    });
  }

  /**
   * Shared stimuli and the questions under each.
   *
   * A stimulus is a reading passage, a listening clip or a video that several
   * questions hang off. It is how an English exam actually works, and how a
   * trading exam asks four things about one chart.
   */
  getGroups(examId: string): Observable<QuestionGroupDto[]> {
    return this.rest.request<void, QuestionGroupDto[]>({
      method: 'GET',
      url: `${this.base}/groups/${examId}`,
    });
  }

  createGroup(body: CreateUpdateQuestionGroupDto): Observable<QuestionGroupDto> {
    return this.rest.request<CreateUpdateQuestionGroupDto, QuestionGroupDto>({
      method: 'POST',
      url: `${this.base}/groups`,
      body,
    });
  }

  updateGroup(id: string, body: CreateUpdateQuestionGroupDto): Observable<QuestionGroupDto> {
    return this.rest.request<CreateUpdateQuestionGroupDto, QuestionGroupDto>({
      method: 'PUT',
      url: `${this.base}/groups/${id}`,
      body,
    });
  }

  /** The questions under it survive as loose questions. */
  deleteGroup(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/groups/${id}` });
  }
}
