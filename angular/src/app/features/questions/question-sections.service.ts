import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedResult, QuestionDto, QuestionListRequest } from '../../core/api/assessment.models';
import { ExamSectionDto } from '../../core/api/structure.models';

/**
 * Filing a question into a part of the paper.
 *
 * A question's section is written on the question but named by the exam's
 * structure, so both screens here need the two endpoints together: the picker on
 * the form and the column on the list are the same fact seen from either side.
 *
 * The list call takes the whole request object rather than naming its fields, so
 * a filter added to QuestionListRequest reaches the server without a second
 * parameter list to keep in step with the first.
 */
@Injectable({ providedIn: 'root' })
export class QuestionSectionsService {
  private readonly rest = inject(RestService);

  /** The exam's parts, in the order they are sat. */
  getSections(examId: string): Observable<ExamSectionDto[]> {
    return this.rest.request<void, ExamSectionDto[]>({
      method: 'GET',
      url: `/api/assessment/exam-structure/sections/${examId}`,
    });
  }

  getList(input: QuestionListRequest): Observable<PagedResult<QuestionDto>> {
    return this.rest.request<void, PagedResult<QuestionDto>>({
      method: 'GET',
      url: '/api/assessment/questions',

      // Undefined, null and empty-string values are dropped before the query is
      // built, so an unset filter is an absent parameter rather than "?type=".
      params: { ...input } as Record<string, unknown>,
    });
  }
}
