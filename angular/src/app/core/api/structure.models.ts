import { QuestionDifficulty } from './assessment.models';

/**
 * A part of an exam: grammar, listening, reading, writing.
 *
 * Sections are what let one exam ask four different kinds of question and still
 * report a sensible result — and what let a section carry its own time limit or
 * its own pass mark, so a candidate who fails listening fails the exam however
 * well the rest went.
 */
export interface ExamSectionDto {
  id: string;
  examId: string;

  name: string;
  instructions?: string | null;

  topicId?: string | null;
  topicName?: string | null;

  timeLimitInMinutes?: number | null;

  /** A floor below which the whole exam fails, however well the rest went. */
  minimumPercentage?: number | null;

  questionsPerForm?: number | null;

  isQualifying: boolean;
  displayOrder: number;

  /** What this section can draw on, so an author can see whether it can fill itself. */
  questionCount: number;
}

export interface CreateUpdateExamSectionDto {
  examId: string;
  name: string;
  instructions?: string | null;
  topicId?: string | null;
  timeLimitInMinutes?: number | null;
  minimumPercentage?: number | null;
  questionsPerForm?: number | null;
  isQualifying: boolean;
  displayOrder: number;
}

export const ExamFormStatus = {
  Draft: 0,
  Published: 1,
  Retired: 2,
} as const;

export type ExamFormStatus = (typeof ExamFormStatus)[keyof typeof ExamFormStatus];

/**
 * A named paper: a fixed set of questions in a fixed order.
 *
 * The reason it exists is comparability. Two candidates who sat "Form 2"
 * answered the same questions, so their scores mean the same thing — where two
 * random draws from one bank do not. It is also how a retake is genuinely a
 * different paper rather than a redraw that repeats half the questions.
 */
export interface ExamFormDto {
  id: string;
  examId: string;

  name: string;
  code: string;

  status: ExamFormStatus;

  /** Filled from the blueprint rather than picked by hand. */
  wasGenerated: boolean;

  /** How many sittings have been served this paper. What a coordinator retires it on. */
  timesUsed: number;

  maxScore: number;
  questionCount: number;
}

export interface ExamFormDetailDto extends ExamFormDto {
  questions: ExamFormQuestionDto[];
}

export interface ExamFormQuestionDto {
  questionId: string;

  /** The prompt, so a reviewer reads the paper rather than a list of identifiers. */
  text: string;
  type: string;

  difficulty: QuestionDifficulty;
  displayOrder: number;

  /** Frozen when the form was published: the same question can be worth more here. */
  score: number;
}

export interface CreateUpdateExamFormDto {
  examId: string;
  name: string;
  code: string;
}
