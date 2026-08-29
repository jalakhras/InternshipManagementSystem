/**
 * Wire types, mirroring the server's contracts.
 *
 * Hand-written rather than generated. ABP's proxy schematic could not run against
 * Angular 22 — it looks for a `defaultProject` key the modern workspace format no
 * longer has — so these are maintained by hand against the C# DTOs.
 *
 * The cost of that is drift, and the guard against it is a CI step that regenerates
 * and fails on a difference (planned, not yet in place). Until then: a change to a
 * DTO on the server is a change here in the same commit.
 */

// ---------------------------------------------------------------- shared

export interface PagedResult<T> {
  totalCount: number;
  items: T[];
}

export interface PagedRequest {
  skipCount?: number;
  maxResultCount?: number;
  sorting?: string;
}

/** Matches the server's ExamStatus. */
export enum ExamStatus {
  Draft = 0,
  Published = 1,
  Archived = 2,
}

/** Matches the server's ExamMode. Practice reveals answers; Assessment does not. */
export enum ExamMode {
  Assessment = 0,
  Practice = 1,
}

export enum QuestionDifficulty {
  Easy = 0,
  Medium = 1,
  Hard = 2,
}

/**
 * Question type identifiers. Strings rather than an enum on the server too, so a
 * new type needs no schema change — see QuestionTypes.cs.
 */
export const QuestionType = {
  Text: 'text',
  SingleChoice: 'single-choice',
  MultiSelect: 'multi-select',
  TrueFalse: 'true-false',
  Matching: 'matching',
  Ordering: 'ordering',
  Numeric: 'numeric',
  Hotspot: 'hotspot',
  FillInTheBlank: 'fill-in-the-blank',
  Code: 'code',
  FileUpload: 'file-upload',
  AudioResponse: 'audio-response',
  Scale: 'scale',
} as const;

// ---------------------------------------------------------------- exams

export interface ExamDto {
  id: string;
  title: string;
  description?: string;

  categoryId?: string;
  categoryName?: string;
  levelId?: string;
  levelName?: string;

  status: ExamStatus;
  mode: ExamMode;

  timeLimitInMinutes: number;
  passingPercentage: number;

  /** Null means the whole bank; a number means a drawn form. */
  questionsPerForm?: number;

  /** How many questions the bank holds, so the author sees the ratio. */
  questionCount: number;

  shuffleQuestions: boolean;
  shuffleOptions: boolean;
  oneQuestionAtATime: boolean;
  allowBackNavigation: boolean;
  collectIntegritySignals: boolean;

  isScheduled: boolean;
  scheduledStartTime?: string;
  scheduledEndTime?: string;

  creationTime: string;
}

export interface CreateUpdateExamDto {
  title: string;
  description?: string;
  categoryId?: string;
  levelId?: string;
  mode: ExamMode;
  timeLimitInMinutes: number;
  passingPercentage: number;
  questionsPerForm?: number;
  shuffleQuestions: boolean;
  shuffleOptions: boolean;
  oneQuestionAtATime: boolean;
  allowBackNavigation: boolean;
  collectIntegritySignals: boolean;
  isScheduled: boolean;
  scheduledStartTime?: string;
  scheduledEndTime?: string;
}

export interface ExamListRequest extends PagedRequest {
  filter?: string;
  categoryId?: string;
  levelId?: string;
  status?: ExamStatus;
}

/**
 * What publishing would do, checked before it is attempted.
 *
 * Blockers and warnings are separate: a blocker refuses the publish, a warning
 * describes an exam that works but was probably not intended — no topics assigned,
 * so the result is a bare number; or no blueprint, so everyone sits the same paper
 * and one leak is everyone's paper.
 */
export interface PublishCheckDto {
  canPublish: boolean;
  blockers: string[];
  warnings: string[];
  questionCount: number;
  totalScore: number;
  formLength: number;
}

export interface BlueprintRuleDto {
  id: string;
  topicId?: string;
  topicName?: string;
  difficulty?: QuestionDifficulty;
  questionType?: string;
  questionCount: number;
  displayOrder: number;

  /** How many bank questions match this rule. Shown so "draw 8 from a pool of 5" is visible. */
  availableCount: number;
}

// ---------------------------------------------------------------- questions

export interface QuestionDto {
  id: string;
  examId: string;
  questionGroupId?: string;

  text: string;
  type: string;

  /** Carries the answer key. Authoring only — never sent to someone sitting the exam. */
  payload: string;

  topicId?: string;
  topicName?: string;

  difficulty: QuestionDifficulty;
  score: number;

  explanation?: string;
  timeLimitInSeconds?: number;

  mediaBlobName?: string;
  mediaType?: string;

  displayOrder: number;
  isActive: boolean;

  timesAnswered: number;

  /** Share answering correctly. Near 1 separates nobody; near 0 usually means it is broken. */
  difficultyIndex?: number;

  /** Whether strong candidates outperform weak ones. At or below zero, pull the question. */
  discriminationIndex?: number;
}

export interface CreateUpdateQuestionDto {
  examId: string;
  questionGroupId?: string;
  text: string;
  type: string;
  payload: string;
  topicId?: string;
  difficulty: QuestionDifficulty;
  score: number;
  explanation?: string;
  timeLimitInSeconds?: number;
  mediaBlobName?: string;
  mediaType?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface QuestionListRequest extends PagedRequest {
  examId?: string;
  topicId?: string;
  type?: string;
  difficulty?: QuestionDifficulty;
  filter?: string;
}

/** Describes a question type to the authoring UI, served so the two cannot disagree. */
export interface QuestionTypeDescriptor {
  type: string;
  nameKey: string;
  descriptionKey: string;

  /** False when a human must mark it — including a type whose grader was never registered. */
  isAutoGraded: boolean;

  hasOptions: boolean;
  acceptsUpload: boolean;
  icon: string;
}

export interface QuestionGroupDto {
  id: string;
  examId: string;
  instructions?: string;
  stimulusText?: string;
  stimulusBlobName?: string;
  stimulusMediaType?: string;
  displayOrder: number;
  questions: QuestionDto[];
}
