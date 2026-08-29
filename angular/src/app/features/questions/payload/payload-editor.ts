import { InputSignal, OutputEmitterRef, Type } from '@angular/core';

/**
 * The contract every question-type editor implements.
 *
 * Thirteen types have to feel like one screen. They do that by sharing the whole
 * frame — prompt, marks, topic, difficulty, explanation, timer — and differing
 * only inside one slot. This interface is that slot.
 *
 * A payload editor knows nothing about the exam, the frame, or saving. It reads a
 * JSON string and emits a JSON string. That keeps a new type to one component and
 * one registry line, which is the same promise the server makes with
 * `IQuestionGrader`.
 */
export interface PayloadEditor {
  /** The question's stored JSON. Empty or malformed means "start from a default". */
  readonly payload: InputSignal<string>;

  /** Emits the new JSON on every change. The frame owns saving, not the editor. */
  readonly payloadChange: OutputEmitterRef<string>;
}

/**
 * Maps a question type to its editor.
 *
 * Lazy, so opening the builder does not download thirteen editors to use one.
 */
export type PayloadEditorLoader = () => Promise<Type<PayloadEditor>>;

/**
 * The registry.
 *
 * Every type this build ships has one. The raw JSON fallback in the frame stays,
 * but only for a type from a LATER build than this client: the server
 * deliberately accepts types it does not know, and the form must not be stricter
 * than the platform.
 *
 * It is no longer something an author of a shipped type can meet. The rule is the
 * owner's, and it is absolute: no input anywhere may require programming skill,
 * to write a question or to answer one. A JSON textarea in front of a language
 * teacher fails that rule however well it is documented.
 */
export const PAYLOAD_EDITORS: Record<string, PayloadEditorLoader> = {
  'single-choice': () => import('./choice-editor.component').then(m => m.ChoiceEditorComponent),
  'multi-select': () => import('./choice-editor.component').then(m => m.ChoiceEditorComponent),
  'true-false': () => import('./choice-editor.component').then(m => m.ChoiceEditorComponent),
  numeric: () => import('./numeric-editor.component').then(m => m.NumericEditorComponent),
  text: () => import('./rubric-editor.component').then(m => m.RubricEditorComponent),
  'file-upload': () => import('./rubric-editor.component').then(m => m.RubricEditorComponent),
  'audio-response': () => import('./rubric-editor.component').then(m => m.RubricEditorComponent),
  matching: () => import('./matching-editor.component').then(m => m.MatchingEditorComponent),
  ordering: () => import('./ordering-editor.component').then(m => m.OrderingEditorComponent),
  'fill-in-the-blank': () => import('./blanks-editor.component').then(m => m.BlanksEditorComponent),
  scale: () => import('./scale-editor.component').then(m => m.ScaleEditorComponent),
  code: () => import('./code-editor.component').then(m => m.CodeEditorComponent),
  hotspot: () => import('./hotspot-editor.component').then(m => m.HotspotEditorComponent),
};
