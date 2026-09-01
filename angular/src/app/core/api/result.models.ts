import { PagedRequest } from './assessment.models';

/** One person's sitting, as the coordinator sees it in a list. */
export interface ResultRow {
  attemptId: string;

  candidateId: string;
  candidateName: string;
  candidateEmail: string;

  examId: string;
  examTitle: string;

  /** The named paper, when the sitting used one. Blank when it was drawn. */
  formName?: string | null;

  startedAt: string;
  submittedAt?: string | null;

  isSubmitted: boolean;

  /**
   * False while something on the paper still needs a person to mark it.
   *
   * Shown rather than hidden: somebody hunting a missing result needs to know
   * it is waiting on the review queue and not on the candidate.
   */
  isGraded: boolean;
  needsManualReview: boolean;

  score: number;
  maxScore: number;
  scorePercentage: number;
  isPassed: boolean;

  endReason: string;

  /** What the person who ended the sitting wrote. Staff-facing, and often absent. */
  endedByReason?: string;
  integrityFlagCount: number;
  durationInMinutes: number;
}

export interface ResultListRequest extends PagedRequest {
  examId?: string;
  candidateGroupId?: string;
  examFormId?: string;
  filter?: string;
  passedOnly?: boolean;
  awaitingMarking?: boolean;
}

/** The figures above the roster, over the whole filtered set rather than the page. */
export interface ResultSummary {
  sat: number;

  /** Sent a link and never started. The number a coordinator chases. */
  notStarted: number;

  passed: number;
  failed: number;
  awaitingMarking: number;

  averageScorePercentage: number;
  highestScorePercentage: number;
  lowestScorePercentage: number;
  medianScorePercentage: number;
}

export interface ResultDetail {
  summary: ResultRow;
  answers: ResultAnswer[];

  /** Empty when the questions carry no topic. */
  byTopic: TopicScore[];

  /**
   * The same marks by the parts the paper was laid out in.
   *
   * Alongside the topics rather than instead of them: a topic is what a question
   * measures, a section is where it sat on the paper. Empty on an exam with no
   * sections.
   */
  bySection: SectionScore[];
}

/** How one sitting went in one part of the paper. */
export interface SectionScore {
  sectionId: string;
  sectionName: string;
  questionCount: number;
  score: number;
  maxScore: number;
  scorePercentage: number;
}

export interface ResultAnswer {
  questionId: string;
  position: number;

  questionText: string;
  type: string;
  topicName?: string | null;

  response?: string | null;
  answerFileName?: string | null;

  isCorrect?: boolean | null;

  awardedScore: number;
  maxScore: number;

  needsManualReview: boolean;
  reviewComment?: string | null;

  timeSpentSeconds?: number | null;
}

export interface TopicScore {
  topicId?: string | null;
  topicName: string;
  questionCount: number;
  score: number;
  maxScore: number;
  scorePercentage: number;
}

/** How one question has behaved across every sitting of its exam. */
export interface ItemAnalysisRow {
  questionId: string;
  text: string;
  type: string;
  topicName?: string | null;

  timesAnswered: number;

  /** Proportion who got it right. Runs backwards from "difficulty": 0.95 is easy. */
  facility: number;

  /**
   * Top quarter minus bottom quarter. Negative nearly always means a wrong key.
   *
   * Null when it cannot be measured — one of the groups never answered this
   * question, which happens as a matter of course when a cohort is split across
   * named papers, or the totals sat too close together for the split to mean
   * anything. Shown as unknown rather than as zero: reporting zero told authors
   * that correctly keyed questions were mis-keyed.
   */
  discrimination: number | null;

  /** A localisation key, set when the numbers say something worth acting on. */
  flagKey?: string | null;
}
