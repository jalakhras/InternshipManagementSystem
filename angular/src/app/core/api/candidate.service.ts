import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedResult } from './assessment.models';
import {
  CandidateDto,
  CandidateGroupDto,
  CandidateListRequest,
  CreateUpdateCandidateDto,
  CreateUpdateCandidateGroupDto,
  ImportCandidatesDto,
  ImportCandidatesResult,
} from './candidate.models';

/**
 * Candidates and the cohorts they belong to.
 *
 * Hand-written against the server's contract, like the other services here. A
 * generated proxy would be one command away and would drift the moment the
 * server changed without anyone running it; writing it out means a change to the
 * contract fails the build rather than the screen.
 */
@Injectable({ providedIn: 'root' })
export class CandidateService {
  private readonly rest = inject(RestService);
  private readonly base = '/api/assessment/candidates';

  getList(input: CandidateListRequest): Observable<PagedResult<CandidateDto>> {
    return this.rest.request<void, PagedResult<CandidateDto>>({
      method: 'GET',
      url: this.base,
      params: { ...input },
    });
  }

  get(id: string): Observable<CandidateDto> {
    return this.rest.request<void, CandidateDto>({ method: 'GET', url: `${this.base}/${id}` });
  }

  create(body: CreateUpdateCandidateDto): Observable<CandidateDto> {
    return this.rest.request<CreateUpdateCandidateDto, CandidateDto>({
      method: 'POST',
      url: this.base,
      body,
    });
  }

  update(id: string, body: CreateUpdateCandidateDto): Observable<CandidateDto> {
    return this.rest.request<CreateUpdateCandidateDto, CandidateDto>({
      method: 'PUT',
      url: `${this.base}/${id}`,
      body,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/${id}` });
  }

  /**
   * Reads a pasted roll. With `dryRun` it reports what would happen and writes
   * nothing, which is what the screen does before asking anyone to commit.
   */
  import(body: ImportCandidatesDto): Observable<ImportCandidatesResult> {
    return this.rest.request<ImportCandidatesDto, ImportCandidatesResult>({
      method: 'POST',
      url: `${this.base}/import`,
      body,
    });
  }

  getGroups(): Observable<CandidateGroupDto[]> {
    return this.rest.request<void, CandidateGroupDto[]>({
      method: 'GET',
      url: `${this.base}/groups`,
    });
  }

  createGroup(body: CreateUpdateCandidateGroupDto): Observable<CandidateGroupDto> {
    return this.rest.request<CreateUpdateCandidateGroupDto, CandidateGroupDto>({
      method: 'POST',
      url: `${this.base}/groups`,
      body,
    });
  }

  updateGroup(id: string, body: CreateUpdateCandidateGroupDto): Observable<CandidateGroupDto> {
    return this.rest.request<CreateUpdateCandidateGroupDto, CandidateGroupDto>({
      method: 'PUT',
      url: `${this.base}/groups/${id}`,
      body,
    });
  }

  deleteGroup(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/groups/${id}` });
  }

  /**
   * Replaces a class's roll outright.
   *
   * Whole-list rather than add-one/remove-one, because the coordinator is working
   * from a register: they know who is in the class, not which two changed since
   * last week.
   */
  /**
   * The whole roll, replaced.
   *
   * `confirmEmptied` is required by the server when the list is empty and the
   * class is not, because an empty list arrives for two different reasons and
   * only one of them is a decision. The other is a screen that failed to read
   * the roll — and it deleted real classes before the server learned to ask.
   */
  setGroupMembers(
    id: string,
    candidateIds: string[],
    confirmEmptied = false,
  ): Observable<CandidateGroupDto> {
    return this.rest.request<
      { candidateIds: string[]; confirmEmptied: boolean },
      CandidateGroupDto
    >({
      method: 'PUT',
      url: `${this.base}/groups/${id}/members`,
      body: { candidateIds, confirmEmptied },
    });
  }

  /**
   * What changed about a class's roll: these people in, those people out.
   *
   * The whole-list save above is only a true sentence while the caller holds the
   * whole class, and a browser never can. The roll editor tried: it read 500
   * candidates and filtered them in the page, so a centre with more than 500
   * people had candidates who could not be found, could not be ticked, and so
   * could not be put into any class — and ABP refuses a page over 1000, so there
   * was no number that would have worked.
   *
   * Naming only the change settles two other things for free. A save can no
   * longer remove people the screen never read, and two coordinators editing one
   * class both keep their work, because adding one person and adding another
   * commute where two whole lists do not.
   */
  changeGroupMembers(id: string, add: string[], remove: string[]): Observable<CandidateGroupDto> {
    return this.rest.request<{ add: string[]; remove: string[] }, CandidateGroupDto>({
      method: 'POST',
      url: `${this.base}/groups/${id}/members`,
      body: { add, remove },
    });
  }
}
