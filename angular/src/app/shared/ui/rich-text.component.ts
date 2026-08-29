import {
  Component,
  ElementRef,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { TranslateService } from '../../core/translate.service';
import { sanitiseRichText } from './rich-text.sanitise';

/**
 * A small formatting editor for question text.
 *
 * Written rather than installed. The formatting an exam author needs is narrow —
 * emphasis, lists, code, and the superscripts a chemistry or physics question
 * cannot do without — and every editor that offers more offers a toolbar of
 * things that have no meaning in an exam, plus a paste path that carries a
 * candidate's browser whatever the author copied from.
 *
 * The Arabic case decided it. A connected right-to-left script wants the caret,
 * the list markers and the toolbar itself to follow the text direction, and the
 * usual libraries treat that as a locale to be configured rather than as the
 * default this product is built around.
 *
 * <h4>What reaches a candidate</h4>
 * The value produced here ends up in front of people sitting an exam, so it is
 * filtered on the way out of this component and again on the server. Angular
 * escapes it once more at render time. Three passes is not paranoia about the
 * author, who is staff; it is about what a stolen staff account could put in
 * front of a room full of candidates.
 */
@Component({
  selector: 'astro-rich-text',
  standalone: true,
  imports: [],
  template: `
    <div class="toolbar" role="toolbar" [attr.aria-label]="t('::Editor:Toolbar')">
      @for (tool of tools; track tool.command) {
        <button
          type="button"
          class="toolbar__button"
          [attr.title]="t(tool.labelKey)"
          [attr.aria-label]="t(tool.labelKey)"
          (mousedown)="$event.preventDefault()"
          (click)="apply(tool.command, tool.argument)">
          <i class="bi {{ tool.icon }}" aria-hidden="true"></i>
        </button>
      }
    </div>

    <!-- The editable surface. It is deliberately not bound to the incoming value
         after the first render: rewriting the DOM under a caret moves it to the
         start, which makes the field unusable from the second keystroke on. The
         component owns its content once it has it, and reports outwards. -->
    <div
      #surface
      class="surface form-control"
      contenteditable="true"
      role="textbox"
      aria-multiline="true"
      [attr.id]="inputId()"
      [attr.aria-label]="label()"
      (input)="onInput()"
      (blur)="publish()"
      (paste)="onPaste($event)"></div>
  `,
  styles: `
    :host {
      display: block;
    }

    .toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: var(--astro-space-1);
      padding: var(--astro-space-1);
      border: 1px solid var(--astro-line);
      border-end-start-radius: 0;
      border-end-end-radius: 0;
      border-start-start-radius: var(--astro-radius-md);
      border-start-end-radius: var(--astro-radius-md);
      background: var(--astro-surface-2);
    }

    .toolbar__button {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border: 0;
      border-radius: var(--astro-radius-sm);
      background: transparent;
      color: var(--astro-ink-2);
      cursor: pointer;

      &:hover {
        background: var(--astro-surface-3);
        color: var(--astro-ink-1);
      }
    }

    .surface {
      min-block-size: 8rem;
      border-start-start-radius: 0;
      border-start-end-radius: 0;
      border-block-start: 0;
      overflow-y: auto;

      /* Lists in a right-to-left question need their markers on the right, which
         padding-inline-start gives without a direction-specific rule. */
      :where(ul, ol) {
        margin-block: var(--astro-space-2);
        padding-inline-start: var(--astro-space-5);
      }

      :where(pre) {
        padding: var(--astro-space-2);
        border-radius: var(--astro-radius-sm);
        background: var(--astro-surface-2);
        overflow-x: auto;
        /* Code is read left to right whatever the surrounding page does. */
        direction: ltr;
        text-align: start;
      }

      :where(code) {
        font-family: var(--astro-font-mono);
        font-size: .9375em;
        direction: ltr;
        unicode-bidi: isolate;
      }
    }
  `,
})
export class RichTextComponent {
  readonly t = inject(TranslateService).t;

  /** Read once, when the surface first exists. See the note on the template. */
  readonly value = input('');
  readonly label = input('');
  readonly inputId = input<string>();

  readonly valueChange = output<string>();

  private readonly surface = viewChild.required<ElementRef<HTMLElement>>('surface');

  /**
   * Whether the author has typed here yet.
   * <p>
   * Not "have we seeded once". An editing form fetches its question after the
   * view exists, so the first value the component sees is empty and a one-shot
   * gate would leave the field blank over a question that has text. And seeding
   * on every value would rewrite the surface under the caret on each keystroke,
   * which reads as the screen flickering. Untouched means the value is still the
   * form's to set; touched means it is the author's.
   * </p>
   */
  private touched = false;

  readonly tools = [
    { command: 'bold', icon: 'bi-type-bold', labelKey: '::Editor:Bold', argument: undefined },
    { command: 'italic', icon: 'bi-type-italic', labelKey: '::Editor:Italic', argument: undefined },
    { command: 'insertUnorderedList', icon: 'bi-list-ul', labelKey: '::Editor:BulletList', argument: undefined },
    { command: 'insertOrderedList', icon: 'bi-list-ol', labelKey: '::Editor:NumberedList', argument: undefined },
    { command: 'formatBlock', icon: 'bi-code-square', labelKey: '::Editor:CodeBlock', argument: 'pre' },
    { command: 'superscript', icon: 'bi-superscript', labelKey: '::Editor:Superscript', argument: undefined },
    { command: 'subscript', icon: 'bi-subscript', labelKey: '::Editor:Subscript', argument: undefined },
    { command: 'removeFormat', icon: 'bi-eraser', labelKey: '::Editor:ClearFormatting', argument: undefined },
  ];

  constructor() {
    effect(() => {
      const incoming = this.value();

      if (this.touched) {
        return;
      }

      const host = this.surface().nativeElement;
      const next = sanitiseRichText(incoming);

      // Guarded because an effect that writes what is already there still counts
      // as a DOM write, and a DOM write on a contenteditable element collapses
      // the selection.
      if (host.innerHTML !== next) {
        host.innerHTML = next;
      }
    });
  }

  onInput(): void {
    this.touched = true;
    this.publish();
  }

  apply(command: string, argument?: string): void {
    this.touched = true;
    this.surface().nativeElement.focus();

    // execCommand is deprecated and has no replacement that browsers agree on.
    // The alternative is a document model and a selection layer of our own, which
    // is a larger and more fragile thing than the eight buttons above justify.
    document.execCommand(command, false, argument);
    this.publish();
  }

  /**
   * Pastes as plain text.
   * <p>
   * Whatever an author copies from arrives with the source's markup, its classes
   * and sometimes its scripts. Taking only the text loses their formatting and
   * keeps everyone else safe; the toolbar is right there to put it back.
   * </p>
   */
  onPaste(event: ClipboardEvent): void {
    event.preventDefault();

    const text = event.clipboardData?.getData('text/plain') ?? '';
    document.execCommand('insertText', false, text);
    this.publish();
  }

  publish(): void {
    this.valueChange.emit(sanitiseRichText(this.surface().nativeElement.innerHTML));
  }
}
