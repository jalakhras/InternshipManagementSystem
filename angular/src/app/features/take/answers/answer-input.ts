import { InputSignal, OutputEmitterRef, Type } from '@angular/core';
import { TakerQuestion } from '../take.models';

/**
 * The contract every answer input implements.
 *
 * The mirror image of the authoring registry: one slot in a stable frame, and a
 * component per type. Adding a question type stays one grader on the server, one
 * editor for the author, and one input here — and nothing else moves.
 *
 * An input receives the question the server chose to send and emits a response
 * string. It never sees a payload, a key, or a mark. That is not a convention to
 * be careful about: the projection the server builds has no field to leak.
 */
export interface AnswerInput {
  readonly question: InputSignal<TakerQuestion>;

  /** The stored response, so a resumed attempt comes back to what was typed. */
  readonly response: InputSignal<string | undefined>;

  /** Emits the response JSON. The frame owns saving, not the input. */
  readonly responseChange: OutputEmitterRef<string>;

  /**
   * Emitted instead by the two types whose answer is a file rather than text.
   *
   * Optional, because ten of the twelve inputs have no use for it. Kept separate
   * from `response` rather than encoded into it: `response` is what the grader
   * reads, and putting a blob name in there would make every grader's parse
   * ambiguous to save one field here.
   */
  readonly attachment?: OutputEmitterRef<AnswerAttachment>;
}

/** A stored file standing in for a written answer. */
export interface AnswerAttachment {
  blobName: string;
  fileName: string;
}

export type AnswerInputLoader = () => Promise<Type<AnswerInput>>;

/**
 * Maps a question type to the control a candidate answers it with.
 *
 * Lazy, because a paper of twenty single-choice questions should not download
 * the code for the other twelve types.
 *
 * A type absent from here falls back to a plain text box. The server accepts
 * types this build does not know and routes them to a person to mark, so
 * refusing to render one would strand a candidate on a question they can read
 * and cannot answer, with a clock running.
 */
export const ANSWER_INPUTS: Record<string, AnswerInputLoader> = {
  'single-choice': () => import('./choice-answer.component').then(m => m.ChoiceAnswerComponent),
  'multi-select': () => import('./choice-answer.component').then(m => m.ChoiceAnswerComponent),
  'true-false': () => import('./choice-answer.component').then(m => m.ChoiceAnswerComponent),
  numeric: () => import('./numeric-answer.component').then(m => m.NumericAnswerComponent),
  scale: () => import('./scale-answer.component').then(m => m.ScaleAnswerComponent),
  ordering: () => import('./ordering-answer.component').then(m => m.OrderingAnswerComponent),
  matching: () => import('./matching-answer.component').then(m => m.MatchingAnswerComponent),
  text: () => import('./text-answer.component').then(m => m.TextAnswerComponent),
  code: () => import('./code-answer.component').then(m => m.CodeAnswerComponent),
  'fill-in-the-blank': () => import('./blanks-answer.component').then(m => m.BlanksAnswerComponent),
  hotspot: () => import('./hotspot-answer.component').then(m => m.HotspotAnswerComponent),
  'file-upload': () => import('./upload-answer.component').then(m => m.UploadAnswerComponent),
  'audio-response': () => import('./audio-answer.component').then(m => m.AudioAnswerComponent),
};

/** What a type with no input of its own falls back to. */
export const FALLBACK_ANSWER_INPUT: AnswerInputLoader = () =>
  import('./text-answer.component').then(m => m.TextAnswerComponent);
