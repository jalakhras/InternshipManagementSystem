import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import {
  BlueprintRuleDto,
  CreateUpdateBlueprintRuleDto,
  CreateUpdateExamDto,
  ExamDto,
  ExamListRequest,
  PagedResult,
  PublishCheckDto,
} from './assessment.models';

/**
 * Exam authoring endpoints.
 *
 * Thin by design: it maps one method to one route and returns the wire type. No
 * caching, no state — screens own their own state through signals, and a service
 * that also caches is a second source of truth for the same data.
 */
@Injectable({ providedIn: 'root' })
export class ExamService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/exams';

  getList(input: ExamListRequest): Observable<PagedResult<ExamDto>> {
    return this.rest.request<void, PagedResult<ExamDto>>({
      method: 'GET',
      url: this.base,
      params: {
        filter: input.filter,
        categoryId: input.categoryId,
        levelId: input.levelId,
        status: input.status,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
        sorting: input.sorting,
      },
    });
  }

  get(id: string): Observable<ExamDto> {
    return this.rest.request<void, ExamDto>({ method: 'GET', url: `${this.base}/${id}` });
  }

  create(body: CreateUpdateExamDto): Observable<ExamDto> {
    return this.rest.request<CreateUpdateExamDto, ExamDto>({ method: 'POST', url: this.base, body });
  }

  update(id: string, body: CreateUpdateExamDto): Observable<ExamDto> {
    return this.rest.request<CreateUpdateExamDto, ExamDto>({
      method: 'PUT',
      url: `${this.base}/${id}`,
      body,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/${id}` });
  }

  /**
   * Everything that would block publishing, plus warnings that would not.
   *
   * Called before showing the publish dialog rather than on the publish attempt,
   * so the author sees the whole list at once instead of being walked through a
   * sequence of refusals.
   */
  checkPublish(id: string): Observable<PublishCheckDto> {
    return this.rest.request<void, PublishCheckDto>({
      method: 'GET',
      url: `${this.base}/${id}/publish-check`,
    });
  }

  publish(id: string): Observable<ExamDto> {
    return this.rest.request<void, ExamDto>({ method: 'POST', url: `${this.base}/${id}/publish` });
  }

  archive(id: string): Observable<ExamDto> {
    return this.rest.request<void, ExamDto>({ method: 'POST', url: `${this.base}/${id}/archive` });
  }

  getBlueprint(examId: string): Observable<BlueprintRuleDto[]> {
    return this.rest.request<void, BlueprintRuleDto[]>({
      method: 'GET',
      url: `${this.base}/${examId}/blueprint`,
    });
  }

  /**
   * Replaces the whole blueprint.
   *
   * Whole-list, because a blueprint is read as a shape — "six grammar, four
   * listening, two hard" — and somebody editing it is restating the shape, not
   * patching one line of it.
   *
   * This route existed on the server with no client method at all, while the
   * papers screen offered "fill from the blueprint" as the recommended way to
   * build a form. There was no blueprint to fill from, and no way to write one.
   */
  setBlueprint(examId: string, rules: CreateUpdateBlueprintRuleDto[]): Observable<BlueprintRuleDto[]> {
    return this.rest.request<CreateUpdateBlueprintRuleDto[], BlueprintRuleDto[]>({
      method: 'PUT',
      url: `${this.base}/${examId}/blueprint`,
      body: rules,
    });
  }
}
