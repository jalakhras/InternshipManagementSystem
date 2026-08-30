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

  getList(): Observable<{ items: TenantDto[]; totalCount: number }> {
    return this.rest.request<void, { items: TenantDto[]; totalCount: number }>({
      method: 'GET',
      url: '/api/multi-tenancy/tenants',
      params: { maxResultCount: 100 },
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
