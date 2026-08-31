import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  CreateUpdateRoleDto,
  PermissionDto,
  PermissionGroupDto,
  RoleDto,
  RoleService,
} from '../../core/api/role.service';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { ModalDirective } from '../../shared/ui/modal.directive';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { failureReason } from '../../core/failure';

/**
 * Roles, and what each one may do.
 *
 * The product ships five roles with meanings the business decided, and until now
 * they were the only five there could ever be: the API to make another has
 * always been there, and nothing reached it. An organisation whose shape does
 * not match ours — an invigilator who may only monitor sittings, a moderator who
 * marks and nothing else — had no way to say so.
 *
 * The permission tree is the screen. Creating a role is one field; deciding what
 * it may do is the whole decision, and it is the part that was missing.
 */
@Component({
  selector: 'astro-role-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, PageHeaderComponent, DataStateComponent, ModalDirective],
  templateUrl: './role-list.component.html',
  styleUrl: './role-list.component.scss',
})
export class RoleListComponent {
  private readonly roles = inject(RoleService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly saving = signal(false);

  readonly items = signal<RoleDto[]>([]);

  readonly canCreate = permissionSignal('AbpIdentity.Roles.Create');
  readonly canEdit = permissionSignal('AbpIdentity.Roles.Update');
  readonly canDelete = permissionSignal('AbpIdentity.Roles.Delete');
  readonly canManagePermissions = permissionSignal('AbpIdentity.Roles.ManagePermissions');

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);

  // --- the role itself ---
  readonly draft = signal<Draft>(empty());
  readonly pendingDelete = signal<RoleDto | null>(null);

  // --- what it may do ---
  readonly editingFor = signal<RoleDto | null>(null);
  readonly groups = signal<PermissionGroupDto[]>([]);
  readonly granted = signal<Set<string>>(new Set());
  readonly permissionsLoading = signal(false);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.roles.getList().subscribe({
      next: page => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  // ------------------------------------------------------------------ the role

  newRole(): void {
    this.draft.set({ ...empty(), open: true });
  }

  edit(role: RoleDto): void {
    this.draft.set({
      open: true,
      id: role.id,
      name: role.name,
      isDefault: role.isDefault,
      isPublic: role.isPublic,
    });
  }

  cancel(): void {
    this.draft.set(empty());
  }

  patch<K extends keyof Draft>(key: K, value: Draft[K]): void {
    this.draft.update(d => ({ ...d, [key]: value }));
  }

  save(): void {
    const draft = this.draft();
    const name = draft.name.trim();

    if (!name) {
      return;
    }

    const body: CreateUpdateRoleDto = {
      name,
      isDefault: draft.isDefault,
      isPublic: draft.isPublic,
    };

    this.saving.set(true);
    this.actionError.set(null);

    const request = draft.id ? this.roles.update(draft.id, body) : this.roles.create(body);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.draft.set(empty());
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  askDelete(role: RoleDto): void {
    this.pendingDelete.set(role);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const role = this.pendingDelete();

    if (!role) {
      return;
    }

    this.saving.set(true);

    this.roles.delete(role.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.pendingDelete.set(null);
        this.load();
      },
      error: err => {
        // The server refuses to delete a role people still hold, and says so.
        // Shown as it comes: "four accounts still have this role" is something
        // an administrator can act on, and a generic failure is not.
        this.actionError.set(this.reason(err));
        this.saving.set(false);
        this.pendingDelete.set(null);
      },
    });
  }

  // ----------------------------------------------------------- the permissions

  openPermissions(role: RoleDto): void {
    this.editingFor.set(role);
    this.permissionsLoading.set(true);
    this.actionError.set(null);
    this.groups.set([]);

    this.roles.getPermissions(role.name).subscribe({
      next: tree => {
        this.groups.set(tree.groups);
        this.granted.set(
          new Set(
            tree.groups.flatMap(g => g.permissions.filter(p => p.isGranted).map(p => p.name)),
          ),
        );
        this.permissionsLoading.set(false);
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.permissionsLoading.set(false);
      },
    });
  }

  closePermissions(): void {
    this.editingFor.set(null);
    this.groups.set([]);
    this.granted.set(new Set());
  }

  isGranted(name: string): boolean {
    return this.granted().has(name);
  }

  /**
   * Ticking a permission that sits under another ticks its parent too, and
   * unticking a parent unticks everything under it.
   * <p>
   * Not a convenience — it is what the server does anyway. A child grant with no
   * parent is not honoured, so a screen that let somebody tick one and save
   * would report a permission the role does not actually have.
   * </p>
   */
  toggle(permission: PermissionDto): void {
    const next = new Set(this.granted());
    const children = this.descendantsOf(permission.name);

    if (next.has(permission.name)) {
      next.delete(permission.name);
      children.forEach(child => next.delete(child));
    } else {
      next.add(permission.name);

      for (let parent = permission.parentName; parent; parent = this.parentOf(parent)) {
        next.add(parent);
      }
    }

    this.granted.set(next);
  }

  /** Every permission in one group, on or off together. */
  toggleGroup(group: PermissionGroupDto): void {
    const next = new Set(this.granted());
    const all = group.permissions.every(p => next.has(p.name));

    group.permissions.forEach(p => (all ? next.delete(p.name) : next.add(p.name)));

    this.granted.set(next);
  }

  groupState(group: PermissionGroupDto): 'all' | 'some' | 'none' {
    const on = group.permissions.filter(p => this.granted().has(p.name)).length;

    if (on === 0) return 'none';

    return on === group.permissions.length ? 'all' : 'some';
  }

  /** How many of a group are on, for the summary beside its name. */
  grantedIn(group: PermissionGroupDto): number {
    return group.permissions.filter(p => this.granted().has(p.name)).length;
  }

  savePermissions(): void {
    const role = this.editingFor();

    if (!role) {
      return;
    }

    const all = this.groups().flatMap(g => g.permissions);

    this.saving.set(true);
    this.actionError.set(null);

    this.roles
      .setPermissions(role.name, {
        // The whole set every time, granted and revoked alike. Sending only the
        // ticked ones would leave a revocation looking like an omission, and the
        // server cannot tell those apart.
        permissions: all.map(p => ({ name: p.name, isGranted: this.granted().has(p.name) })),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.closePermissions();
        },
        error: err => {
          this.actionError.set(this.reason(err));
          this.saving.set(false);
        },
      });
  }

  private parentOf(name: string): string | null {
    for (const group of this.groups()) {
      const found = group.permissions.find(p => p.name === name);

      if (found) {
        return found.parentName;
      }
    }

    return null;
  }

  private descendantsOf(name: string): string[] {
    const direct = this.groups()
      .flatMap(g => g.permissions)
      .filter(p => p.parentName === name)
      .map(p => p.name);

    return direct.flatMap(child => [child, ...this.descendantsOf(child)]);
  }

  private reason(err: unknown): string {
    return failureReason(err, this.t);
  }
}

interface Draft {
  open: boolean;
  id: string | null;
  name: string;
  isDefault: boolean;
  isPublic: boolean;
}

const empty = (): Draft => ({
  open: false,
  id: null,
  name: '',
  isDefault: false,
  isPublic: true,
});
