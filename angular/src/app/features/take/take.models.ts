/**
 * What a candidate's browser is allowed to know.
 *
 * These mirror the server's taker DTOs and nothing else. There is no correctness
 * field anywhere in this file, and there must never be one: the server builds
 * what a taker receives through a projection that shares no type with the
 * authoring side, and this is that projection's shape.
 */

/** Matches the server's ExamMode. Practice reveals answers afterwards; Assessment does not. */
export enum TakeExamMode {
  Assessment = 0,
  Practice = 1,
}

export interface ExamPreview {
  /** False when the link is spent, expired, revoked, or the exam is closed. */
  isAccessible: boolean;

  /** Why, specifically. "Invalid link" tells a candidate nothing they can act on. */
  blockReason?: string;

  examTitle: string;
  description?: string;
  candidateName: string;

  timeLimitInMinutes: number;
  questionCount: number;

  attemptsAllowed: number;
  attemptsUsed: number;

  expiresAt: string;
  mode: TakeExamMode;

  /** Set when an attempt is already running, so the screen offers to resume rather than to start. */
  resumableAttemptId?: string;

  /**
   * The organisation running this exam, and its mark.
   *
   * A candidate has no relationship with this platform and no reason to trust a
   * name they have never heard of. What they recognise is the centre that told
   * them to expect the email.
   */
  organizationName?: string;
  organizationLogoUrl?: string;

  /** Painted onto the page, so the candidate is in the organisation's space. */
  organizationBrandColor?: string;

  /** Where to write if something goes wrong, when the centre has said. */
  organizationSupportEmail?: string;

  /** Minted on opening the link. Worth one attempt, and only in memory. */
  sessionToken?: string;
}

export interface TakerStimulus {
  id: string;
  instructions?: string;
  text?: string;
  mediaUrl?: string;
  mediaType?: string;
}

export interface TakerOption {
  id: string;
  text: string;
  mediaUrl?: string;
}

/**
 * Which part of the exam the candidate is in.
 *
 * Absent on the exams — most of them — that are one undivided paper. Nothing
 * about a section's clock, its floor or its qualifying flag is here, because
 * none of the three is enforced yet and a candidate told "twenty minutes for
 * this part" by a screen that will not stop them at twenty has been misled more
 * precisely than by being told nothing.
 */
export interface TakerSection {
  id: string;
  name: string;

  /** Sent only on the first question of the section, which is when they are true. */
  instructions?: string;

  /** Where in this section the candidate is, one-based. */
  position: number;

  /** How many questions this section holds on this candidate's paper. */
  questionCount: number;

  /** True on the question where the section begins. */
  isFirstQuestion: boolean;
}

export interface TakerQuestion {
  id: string;
  position: number;
  totalQuestions: number;

  text: string;
  type: string;
  score: number;

  timeLimitInSeconds?: number;

  mediaUrl?: string;
  mediaType?: string;

  /** A passage or a recording shared by several questions. */
  stimulus?: TakerStimulus;

  /** The part of the exam this question sits in, when the exam has parts. */
  section?: TakerSection;

  options: TakerOption[];

  /** Whatever else the type needs to be answered — never anything that reveals the answer. */
  display: Record<string, unknown>;

  savedResponse?: string;
  savedFileName?: string;
}

export interface AttemptState {
  attemptId: string;

  /** Recomputed by the server on every exchange. The browser's clock never gets a vote. */
  secondsRemaining: number;

  totalQuestions: number;
  answeredCount: number;

  /** One entry per position, so the map can show what is left without fetching the paper. */
  answered: boolean[];

  isSubmitted: boolean;
  allowBackNavigation: boolean;
  oneQuestionAtATime: boolean;

  /**
   * Where to write if something goes wrong, when the centre has said.
   *
   * On the state and not only on the entry screen, because the moment it is
   * needed is the moment that screen is long gone.
   */
  organizationSupportEmail?: string;

  /**
   * Returned by the start, and it replaces the one from the entry screen.
   *
   * That earlier token was minted before the attempt existed, so it names no
   * attempt at all. Everything after the start reads the attempt out of the
   * token, so keeping the old one asks the server about the empty id.
   */
  sessionToken?: string;
}

export interface SaveAnswer {
  questionId: string;
  response?: string;
  answerBlobName?: string;
  answerFileName?: string;

  // Observations about how the answer arrived, not judgements about it.
  timeSpentSeconds?: number;
  wasPasted?: boolean;
  keystrokeCount?: number;
  backspaceCount?: number;
}

export interface SaveAnswerResult {
  savedAt: string;
  secondsRemaining: number;

  /** True when the deadline has passed. The screen stops and submits. */
  isExpired: boolean;

  /** Whether anything was actually written. Not the opposite of `isExpired`. */
  saved: boolean;
}

export interface TopicScore {
  topicId: string;
  topicName: string;
  score: number;
  maxScore: number;
  percentage: number;
}

/** How the candidate did in one part of the paper. */
export interface SectionScore {
  sectionId: string;
  sectionName: string;
  questionCount: number;
  score: number;
  maxScore: number;
  percentage: number;
}

export interface PracticeReviewItem {
  questionId: string;
  text: string;
  wasCorrect?: boolean;
  awardedScore?: number;
  maxScore: number;
  correctAnswer?: string;
  explanation?: string;
}

export interface AttemptResult {
  attemptId: string;
  examTitle: string;

  /** False while a person still has answers to mark. The score is withheld until then. */
  isFinal: boolean;

  /** The organisation releases results itself; no score is shown here at all. */
  scoreWithheld: boolean;

  score: number;
  maxScore: number;
  scorePercentage: number;
  isPassed: boolean;

  submittedAt: string;

  topicBreakdown: TopicScore[];

  /**
   * The same marks read by the parts of the paper rather than by competency.
   *
   * Empty on an exam with no sections. In the exam's own order, so it reads back
   * against the paper the candidate remembers sitting.
   */
  sectionBreakdown: SectionScore[];

  /** What the marker wrote for this person to read. */
  feedback: string[];

  /** Practice mode only, and only after submitting. */
  review: PracticeReviewItem[];
}

/**
 * What the browser can observe, mirrored from the server's own enum.
 *
 * The numbers matter: this crosses the wire as a number and the server binds it
 * to `IntegritySignalType`. The client used to post `{ kind: 'window-blur' }` to
 * a server reading `Type`, so nothing bound and every observation was stored as
 * the default — Paste. A marker weighing whether an answer was somebody's own
 * work was told they pasted it when they had alt-tabbed. Observations are
 * supposed to inform a person's judgement; a wrong one misinforms it, about a
 * named candidate, in the record.
 */
export enum IntegritySignalType {
  Paste = 0,
  WindowBlur = 1,
  ImplausibleSpeed = 2,
  NoCorrections = 3,
  DevToolsOpened = 4,
  PageReloaded = 5,
}
