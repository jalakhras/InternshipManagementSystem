import { PagedRequest } from './assessment.models';

/**
 * Matches the server's CandidateStatus.
 *
 * A candidate is not a user and never becomes one: no account, no password,
 * nothing to log in to. Status is where they are in the organisation's own
 * process, not a state of an identity.
 */
export enum CandidateStatus {
  Pending = 0,
  Invited = 1,
  InProgress = 2,
  Completed = 3,
  Withdrawn = 4,
}

export interface CandidateDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  categoryId?: string;
  categoryName?: string;

  /** The tenant's own identifier — a student number, an applicant reference. */
  reference?: string;

  status: CandidateStatus;

  /** Why this person is on the list, in the words the coordinator gave them. */
  groupNames: string[];

  attemptCount: number;
  creationTime: string;
}

export interface CreateUpdateCandidateDto {
  fullName: string;
  email: string;
  phoneNumber?: string;
  categoryId?: string;
  reference?: string;
}

export interface CandidateListRequest extends PagedRequest {
  filter?: string;
  categoryId?: string;
  groupId?: string;
  status?: CandidateStatus;
}

export interface CandidateGroupDto {
  id: string;
  name: string;
  description?: string;
  categoryId?: string;
  categoryName?: string;
  memberCount: number;
  creationTime: string;
}

export interface CreateUpdateCandidateGroupDto {
  name: string;
  description?: string;
  categoryId?: string;
}

export interface ImportCandidatesDto {
  text: string;
  categoryId?: string;
  groupId?: string;

  /** Checks the list and reports what would happen without writing anything. */
  dryRun?: boolean;
}

export interface ImportProblem {
  /** One-based over the pasted text, so it matches what the reader is looking at. */
  line: number;
  content: string;

  /** A localisation key, so the reason reads in the reader's language. */
  reason: string;
}

export interface ImportCandidatesResult {
  created: number;

  /** Matched by address and left alone. Importing twice must not double the roll. */
  alreadyPresent: number;

  addedToGroup: number;
  problems: ImportProblem[];
}
