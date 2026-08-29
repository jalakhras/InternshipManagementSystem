import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import { PagedResult } from './assessment.models';

/** A staff account. Candidates never have one — a link is their entire credential. */
export interface UserDto {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;

  /** What this account may do. A staff account with none can sign in and see nothing. */
  roles: string[];
}

export interface CreateUpdateUserDto {
  userName: string;
  email: string;

  /** Required on create, left blank on edit to keep the existing one. */
  password?: string;

  fullName: string;
  phoneNumber?: string | null;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/app/users';

  getList(skipCount = 0, maxResultCount = 50): Observable<PagedResult<UserDto>> {
    return this.rest.request<void, PagedResult<UserDto>>({
      method: 'GET',
      url: this.base,
      params: { skipCount, maxResultCount },
    });
  }

  /** Read from the identity module, so a role added by an administrator appears without a deployment. */
  getRoles(): Observable<string[]> {
    return this.rest.request<void, string[]>({ method: 'GET', url: `${this.base}/roles` });
  }

  create(body: CreateUpdateUserDto): Observable<UserDto> {
    return this.rest.request<CreateUpdateUserDto, UserDto>({
      method: 'POST',
      url: this.base,
      body,
    });
  }

  update(id: string, body: CreateUpdateUserDto): Observable<UserDto> {
    return this.rest.request<CreateUpdateUserDto, UserDto>({
      method: 'PUT',
      url: `${this.base}/${id}`,
      body,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/${id}` });
  }
}
