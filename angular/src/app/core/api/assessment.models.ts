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

/**
 * One line of a blueprint: how many questions of what kind a paper draws.
 *
 * A blueprint is what makes two drawn papers comparable. Without one every
 * candidate gets a random handful and the scores mean nothing side by side;
 * with one, "six grammar, four listening, two of them hard" holds for everybody
 * however the individual questions differ.
 */
export interface CreateUpdateBlueprintRuleDto {
  /** Null means "any topic", which is the right answer for a single-subject exam. */
  topicId?: string | null;

  /** Null means "any difficulty". */
  difficulty?: QuestionDifficulty | null;

  /** Null means "any type". */
  questionType?: string | null;

  questionCount: number;
  displayOrder: number;
}

// ---------------------------------------------------------------- questions

export interface QuestionDto {
  id: string;

  /** Absent for a bank question, which belongs to a domain rather than to a paper. */
  examId?: string | null;

  categoryId?: string | null;
  levelId?: string | null;

  /**
   * The part of the paper this question sits in, or absent for unfiled.
   *
   * Not a label. The paper is drawn section by section from the questions filed
   * into each one, so this field is what decides whether a question can appear
   * in the listening part at all.
   */
  examSectionId?: string | null;

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
  /**
   * The exam that owns this question, or absent for a bank question.
   *
   * Absent is the interesting case and the one that was unreachable: a bank
   * question belongs to a domain and a level rather than to one paper, and every
   * exam at that level can draw it.
   */
  examId?: string | null;

  /** Required when there is no exam: it is what makes the question findable. */
  categoryId?: string | null;

  /** Optional even in the bank — a question with no level suits every level in its domain. */
  levelId?: string | null;

  /**
   * Which part of the paper to file this question into. Absent means unfiled.
   *
   * The server assigns this unconditionally on every save, so a form that omits
   * it does not leave the section alone — it clears it. Anything editing a
   * question has to carry the value it read.
   */
  examSectionId?: string | null;

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

  /** Only questions owned by no exam. What the bank screen asks for. */
  bankOnly?: boolean;

  /** Only questions filed into one part of the paper. */
  examSectionId?: string;

  categoryId?: string;
  levelId?: string;
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

/**
 * A shared stimulus and the questions hanging off it.
 *
 * A reading passage with six questions under it, a listening clip with four, a
 * chart with three. It is how a language exam actually works, and the taker sees
 * the passage once beside every question rather than repeated in each prompt.
 */
export interface QuestionGroupDto {
  id: string;
  examId: string;

  /** What to do with it: "Read the passage and answer questions 1–6." */
  instructions?: string;

  /** The passage itself, when it is text. */
  stimulusText?: string;

  /** The clip, video or image, when it is not. */
  stimulusBlobName?: string;
  stimulusMediaType?: string;

  displayOrder: number;
  questions: QuestionDto[];
}

export interface CreateUpdateQuestionGroupDto {
  examId: string;
  instructions?: string | null;
  stimulusText?: string | null;
  stimulusBlobName?: string | null;
  stimulusMediaType?: string | null;
  displayOrder: number;
}

// ------------------------------------------------------- importing questions

/**
 * A spreadsheet of questions.
 *
 * The counterpart to the candidate import, for the other half of the setup
 * cost: an author's questions are already in a spreadsheet, and retyping eighty
 * of them with four options each is why authoring stops on the first evening.
 *
 * The file travels as bytes rather than as text so the byte-order mark Excel
 * writes reaches the server, which is the one place that should be deciding
 * what to do about it.
 */
export interface ImportQuestionsDto {
  /** The file, base64 encoded — which is what a `byte[]` is over JSON. */
  content: string;

  examId?: string;
  categoryId?: string;
  levelId?: string;

  /** Reads the file and reports what would happen without writing anything. */
  dryRun?: boolean;
}

/** One row that will become a question, in words the author will recognise. */
export interface ImportQuestionPreview {
  line: number;
  text: string;
  type: string;
  score: number;
  difficulty: QuestionDifficulty;

  options: string[];

  /**
   * What will be marked right, written out rather than numbered. The mistake
   * worth catching here is a key one row off, and a list of numbers looks
   * exactly as right when it is wrong.
   */
  correctAnswers: string[];
}

/**
 * One row that will not become a question, and why.
 *
 * Carries the column as well as the row, because "row 14 is wrong" sends
 * somebody to read nine cells and "the correct answer in row 14 names no
 * option" sends them to one.
 */
export interface ImportQuestionProblem {
  /** One-based over the file, so it is the row number the spreadsheet shows. */
  line: number;

  /** A localisation key naming the column at fault. */
  column: string;

  /** A localisation key, so the reason reads in the reader's language. */
  reason: string;

  content: string;
}

export interface ImportQuestionsResult {
  created: number;

  /** Matched by question text and left alone, so importing twice adds nothing twice. */
  alreadyPresent: number;

  preview: ImportQuestionPreview[];
  problems: ImportQuestionProblem[];
}
