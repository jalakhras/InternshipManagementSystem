import { Signal, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { PermissionService } from '@abp/ng.core';

/**
 * Whether the current user holds a policy, as a signal that updates when the
 * answer does.
 *
 * `PermissionService.getGrantedPolicy()` answers once, from whatever the config
 * state happens to hold at that instant. Read in a field initialiser it is
 * evaluated during construction, and a component built before the remote
 * configuration lands captures `false` and keeps it — the screen then shows no
 * actions at all, to a user who has every permission.
 *
 * That race is not theoretical and it is not symmetric: Arabic loads its locale
 * data with an extra dynamic import, which is enough to move component
 * construction ahead of the configuration and produce a screen with no buttons
 * in one language and every button in the other.
 *
 * The observable form answers again whenever the configuration changes, so the
 * template simply re-renders when the truth arrives.
 */
export function permissionSignal(policy: string): Signal<boolean> {
  return toSignal(inject(PermissionService).getGrantedPolicy$(policy), {
    initialValue: false,
  });
}
