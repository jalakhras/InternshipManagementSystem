import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

/** One organisation on this deployment. */
export interface TenantDto {
  id: string;
  name: string;
}

export interface CreateTenantDto {
  name: string;

  /** The first account, created with the organisation. Without one nobody can get in. */
  adminEmailAddress: string;
  adminPassword: string;
}

export interface UpdateTenantDto {
  name: string;
}

export interface TenantListRequest {
  /** Matched as a contains, which is how somebody looks an organisation up. */
  filter?: string;
  skipCount?: number;
  maxResultCount?: number;
}

/**
 * The organisations sharing this deployment.
 *
 * Host-level only: a tenant cannot see that other tenants exist, which is most
 * of what multi-tenancy means here. ABP's tenant management module owns this,
 * and creating a tenant through it also seeds the organisation — its roles, its
 * permission grants, and the first administrator — so a tenant made from the
 * screen is usable immediately rather than an empty shell waiting for somebody
 * to run a console application.
 */
@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly rest = inject(RestService);

  /**
   * One page of organisations, searched on the server.
   *
   * It used to ask for a hundred and no more, with no term and no page — the
   * same wall the class roll had: a deployment with a hundred and one customers
   * had one nobody could rename, suspend or reach, and nothing said so. A host
   * administrator is the one person who cannot work around it, because there is
   * no other screen that lists organisations.
   */
  getList(request: TenantListRequest = {}): Observable<{ items: TenantDto[]; totalCount: number }> {
    return this.rest.request<void, { items: TenantDto[]; totalCount: number }>({
      method: 'GET',
      url: '/api/multi-tenancy/tenants',
      params: {
        filter: request.filter || undefined,
        skipCount: request.skipCount ?? 0,
        maxResultCount: request.maxResultCount ?? 25,
        sorting: 'name',
      },
    });
  }

  create(body: CreateTenantDto): Observable<TenantDto> {
    return this.rest.request<CreateTenantDto, TenantDto>({
      method: 'POST',
      url: '/api/multi-tenancy/tenants',
      body,
    });
  }

  update(id: string, body: UpdateTenantDto): Observable<TenantDto> {
    return this.rest.request<UpdateTenantDto, TenantDto>({
      method: 'PUT',
      url: `/api/multi-tenancy/tenants/${id}`,
      body,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `/api/multi-tenancy/tenants/${id}`,
    });
  }
}
