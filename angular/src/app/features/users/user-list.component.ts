import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CreateUpdateUserDto, UserDto, UserService } from '../../core/api/user.service';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { ModalDirective } from '../../shared/ui/modal.directive';

/**
 * Staff accounts.
 *
 * Candidates never appear here and never will: a link is their entire
 * credential, and giving them accounts would be asking somebody sitting one
 * exam to manage a password.
 *
 * Roles are on the form rather than behind a second screen. An account created
 * without one can sign in and see an empty application, and by the time that is
 * discovered whoever created it has already told them their password. What is
 * ticked when they press save is what the account holds — whole-list, because
 * the person is deciding what this account is for, not diffing it.
 */
@Component({
  selector: 'astro-user-list',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, DataStateComponent, ModalDirective],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent {
  private readonly users = inject(UserService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly saving = signal(false);

  readonly items = signal<UserDto[]>([]);
  readonly roles = signal<string[]>([]);

  readonly canCreate = permissionSignal(P.IdentityManagement.Users.Create);
  readonly canEdit = permissionSignal(P.IdentityManagement.Users.Edit);
  readonly canDelete = permissionSignal(P.IdentityManagement.Users.Delete);

  readonly draft = signal<Draft>(empty());
  readonly pendingDelete = signal<UserDto | null>(null);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);

  /** A new account needs a password; an existing one keeps the one it has. */
  readonly needsPassword = computed(() => !this.draft().id && !this.draft().password);

  constructor() {
    this.load();

    this.users.getRoles().subscribe({
      next: roles => this.roles.set(roles),
      error: () => undefined,
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.users.getList().subscribe({
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

  newUser(): void {
    this.draft.set({ ...empty(), open: true });
  }

  edit(user: UserDto): void {
    this.draft.set({
      open: true,
      id: user.id,
      userName: user.userName,
      email: user.email,
      fullName: user.fullName ?? '',
      phoneNumber: user.phoneNumber ?? '',

      // Deliberately blank. Showing a placeholder that looks like a password
      // invites somebody to save it as one.
      password: '',
      roles: [...(user.roles ?? [])],
    });
  }

  cancel(): void {
    this.draft.set(empty());
  }

  patch<K extends keyof Draft>(key: K, value: Draft[K]): void {
    this.draft.update(d => ({ ...d, [key]: value }));
  }

  hasRole(role: string): boolean {
    return this.draft().roles.includes(role);
  }

  toggleRole(role: string): void {
    this.draft.update(d => ({
      ...d,
      roles: d.roles.includes(role) ? d.roles.filter(r => r !== role) : [...d.roles, role],
    }));
  }

  save(): void {
    const draft = this.draft();

    if (!draft.userName.trim() || !draft.email.trim() || this.needsPassword()) {
      return;
    }

    const body: CreateUpdateUserDto = {
      userName: draft.userName.trim(),
      email: draft.email.trim(),
      fullName: draft.fullName.trim(),
      phoneNumber: draft.phoneNumber.trim() || null,
      password: draft.password || undefined,
      roles: draft.roles,
    };

    this.saving.set(true);
    this.actionError.set(null);

    const request = draft.id
      ? this.users.update(draft.id, body)
      : this.users.create(body);

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

  askDelete(user: UserDto): void {
    this.pendingDelete.set(user);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const user = this.pendingDelete();

    if (!user) {
      return;
    }

    this.saving.set(true);

    this.users.delete(user.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.pendingDelete.set(null);
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
        this.pendingDelete.set(null);
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}

interface Draft {
  open: boolean;
  id: string | null;
  userName: string;
  email: string;
  fullName: string;
  phoneNumber: string;
  password: string;
  roles: string[];
}

const empty = (): Draft => ({
  open: false,
  id: null,
  userName: '',
  email: '',
  fullName: '',
  phoneNumber: '',
  password: '',
  roles: [],
});
