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
}

export interface TopicScore {
  topicId: string;
  topicName: string;
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

  score: number;
  maxScore: number;
  scorePercentage: number;
  isPassed: boolean;

  submittedAt: string;

  topicBreakdown: TopicScore[];

  /** Practice mode only, and only after submitting. */
  review: PracticeReviewItem[];
}
