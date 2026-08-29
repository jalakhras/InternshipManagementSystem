import { Page } from '@playwright/test';

/**
 * Stubs the question endpoints.
 *
 * The type catalogue mirrors what the server actually serves, including which
 * types are auto-graded — that flag drives what the picker tells the author, and
 * a stub that got it wrong would hide the very defect the integration tests just
 * found on the server side.
 */
const TYPES = [
  { type: 'single-choice', icon: 'bi-ui-radios', isAutoGraded: true, hasOptions: true, acceptsUpload: false },
  { type: 'multi-select', icon: 'bi-ui-checks', isAutoGraded: true, hasOptions: true, acceptsUpload: false },
  { type: 'true-false', icon: 'bi-toggle-on', isAutoGraded: true, hasOptions: true, acceptsUpload: false },
  { type: 'text', icon: 'bi-textarea-t', isAutoGraded: false, hasOptions: false, acceptsUpload: false },
  { type: 'numeric', icon: 'bi-123', isAutoGraded: true, hasOptions: false, acceptsUpload: false },
  { type: 'matching', icon: 'bi-arrow-left-right', isAutoGraded: true, hasOptions: true, acceptsUpload: false },
  { type: 'ordering', icon: 'bi-sort-numeric-down', isAutoGraded: true, hasOptions: true, acceptsUpload: false },
  { type: 'fill-in-the-blank', icon: 'bi-input-cursor-text', isAutoGraded: true, hasOptions: false, acceptsUpload: false },
  { type: 'hotspot', icon: 'bi-crosshair', isAutoGraded: true, hasOptions: false, acceptsUpload: false },
  { type: 'code', icon: 'bi-code-square', isAutoGraded: true, hasOptions: false, acceptsUpload: false },
  { type: 'file-upload', icon: 'bi-paperclip', isAutoGraded: false, hasOptions: false, acceptsUpload: true },
  { type: 'audio-response', icon: 'bi-mic', isAutoGraded: false, hasOptions: false, acceptsUpload: true },
  { type: 'scale', icon: 'bi-sliders2', isAutoGraded: false, hasOptions: false, acceptsUpload: false },
];

export async function stubQuestions(page: Page): Promise<void> {
  await page.route('**/api/assessment/questions/types', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        TYPES.map(t => ({
          ...t,
          nameKey: `::QuestionType:${t.type}`,
          descriptionKey: `::QuestionType:${t.type}:Description`,
        })),
      ),
    }),
  );

  await page.route('**/api/assessment/questions', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: '99999999-9999-9999-9999-999999999999' }),
    }),
  );
}
