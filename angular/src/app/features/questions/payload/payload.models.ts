/**
 * Payload shapes, mirroring the server's typed views in QuestionPayload.cs.
 *
 * Kept as plain interfaces plus a tolerant reader: the payload is free-form JSON
 * by design, so an editor must cope with a shape it did not write — a question
 * authored by an older build, or a type whose editor was added later.
 */

export interface ChoiceOption {
  id: string;
  text: string;
  isCorrect: boolean;
  blobName?: string;
}

export interface ChoicePayload {
  options: ChoiceOption[];

  /**
   * Multi-select only. Correct picks earn their share — but a single wrong pick
   * still scores zero, or selecting everything would be optimal.
   */
  allowPartialCredit: boolean;
}

export interface NumericPayload {
  correctValue: number;

  /** Absolute tolerance. 0.5 accepts anything within ±0.5. */
  tolerance: number;

  unit?: string;
}

export interface RubricCriterion {
  id: string;
  name: string;
  description?: string;
  maxScore: number;
}

export interface RubricPayload {
  criteria: RubricCriterion[];

  /** Written for the marker. Never shown to the candidate. */
  reviewerGuidance?: string;
}

/**
 * Parses a payload, returning the fallback when it cannot.
 *
 * Never throws. A malformed payload should open an editor showing a sensible
 * default, not a blank screen — the author is usually there to fix exactly that.
 */
export function readPayload<T>(json: string, fallback: T): T {
  if (!json?.trim()) {
    return fallback;
  }

  try {
    const parsed = JSON.parse(json);
    return parsed && typeof parsed === 'object' ? { ...fallback, ...parsed } : fallback;
  } catch {
    return fallback;
  }
}

export function writePayload(value: unknown): string {
  return JSON.stringify(value);
}

/**
 * A short, stable id for a new option or criterion.
 *
 * Ids travel into saved answers and into the shuffle order, so they have to
 * survive edits: renaming an option's text must not orphan the answers already
 * given against it.
 */
export function newId(prefix: string): string {
  return `${prefix}${Math.random().toString(36).slice(2, 8)}`;
}
