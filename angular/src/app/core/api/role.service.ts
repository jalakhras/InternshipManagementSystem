import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

/** A role as ABP's identity module keeps it. */
export interface RoleDto {
  id: string;
  name: string;

  /** Given to every new account automatically. */
  isDefault: boolean;

  /** Created by the seeder. Its name is a key other code depends on. */
  isStatic: boolean;

  isPublic: boolean;
}

export interface CreateUpdateRoleDto {
  name: string;
  isDefault: boolean;
  isPublic: boolean;
}

/** One permission, as the server defines and displays it. */
export interface PermissionDto {
  name: string;
  displayName: string;

  /** The permission this one sits under, or null at the top of a group. */
  parentName: string | null;

  isGranted: boolean;

  /**
   * Where the grant comes from. A permission granted through another provider —
   * a role the user also holds — is shown but cannot be taken away here.
   */
  grantedProviders: { providerName: string; providerKey: string }[];
}

export interface PermissionGroupDto {
  name: string;
  displayName: string;
  permissions: PermissionDto[];
}

export interface PermissionTreeDto {
  entityDisplayName: string;
  groups: PermissionGroupDto[];
}

export interface UpdatePermissionsDto {
  permissions: { name: string; isGranted: boolean }[];
}

/**
 * Roles, and what each one may do.
 *
 * Both live in ABP's own modules rather than this product's — identity owns the
 * roles, permission management owns the grants — so this service talks to those
 * endpoints directly instead of wrapping them in an application service that
 * would add nothing but a second name for the same thing.
 *
 * The permission tree is addressed by provider: "R" and a role name. The same
 * endpoint serves a user's permissions with "U", which is why the parameter is
 * not called `roleName`.
 */
@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly rest = inject(RestService);

  getList(): Observable<{ items: RoleDto[]; totalCount: number }> {
    return this.rest.request<void, { items: RoleDto[]; totalCount: number }>({
      method: 'GET',
      url: '/api/identity/roles',
      params: { maxResultCount: 100 },
    });
  }

  create(body: CreateUpdateRoleDto): Observable<RoleDto> {
    return this.rest.request<CreateUpdateRoleDto, RoleDto>({
      method: 'POST',
      url: '/api/identity/roles',
      body,
    });
  }

  update(id: string, body: CreateUpdateRoleDto): Observable<RoleDto> {
    return this.rest.request<CreateUpdateRoleDto, RoleDto>({
      method: 'PUT',
      url: `/api/identity/roles/${id}`,
      body,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `/api/identity/roles/${id}`,
    });
  }

  getPermissions(roleName: string): Observable<PermissionTreeDto> {
    return this.rest.request<void, PermissionTreeDto>({
      method: 'GET',
      url: '/api/permission-management/permissions',
      params: { providerName: 'R', providerKey: roleName },
    });
  }

  setPermissions(roleName: string, body: UpdatePermissionsDto): Observable<void> {
    return this.rest.request<UpdatePermissionsDto, void>({
      method: 'PUT',
      url: '/api/permission-management/permissions',
      params: { providerName: 'R', providerKey: roleName },
      body,
    });
  }
}
