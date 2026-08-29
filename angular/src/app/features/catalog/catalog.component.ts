import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';

import { CatalogService } from '../../core/api/catalog.service';
import {
  CategoryDto,
  CategorySet,
  CreateUpdateCategoryDto,
  CreateUpdateLevelDto,
  CreateUpdateTopicDto,
  LevelDto,
  TopicDto,
} from '../../core/api/catalog.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * The catalogue everything else is filed against.
 *
 * This screen is the reason three finished features were dead. The tables
 * existed; nothing could put a row in one; so every exam and every question
 * carried a null domain and a null level — and the drawing rule behind the
 * shared item bank is "same domain, same level", which with nulls collapses to
 * "this exam's own questions". The bank, the blueprint and the topic breakdown
 * were all correct code nobody could reach.
 *
 * Two panes rather than three screens: a domain means nothing without its
 * ladder, and somebody setting up English wants A1 through C2 in front of them
 * while they think about it, not behind a second navigation.
 *
 * Nothing here asks for an identifier, a JSON blob or a slug the author has to
 * invent a convention for. A code is offered from the name and can be
 * overtyped — the code matters to a spreadsheet import, and nobody should have
 * to know that on the way in.
 */
@Component({
  selector: 'astro-catalog',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.scss',
})
export class CatalogComponent {
  private readonly catalog = inject(CatalogService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly categories = signal<CategoryDto[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly includeInactive = signal(false);

  readonly canManage = permissionSignal(P.Catalog.Manage);

  readonly selected = computed(() =>
    this.categories().find(c => c.id === this.selectedId()) ?? null,
  );

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.categories().length === 0);

  /**
   * Top-level topics with their children beneath them, one flat list carrying a
   * depth. The tree is two or three deep in practice, and rendering it as an
   * indented list keeps the whole thing on one screen where a real tree widget
   * would put half of it behind disclosure arrows.
   */
  readonly topicRows = computed<TopicRow[]>(() => {
    const topics = this.selected()?.topics ?? [];
    const byParent = new Map<string, TopicDto[]>();

    for (const topic of topics) {
      const key = topic.parentId ?? '';
      byParent.set(key, [...(byParent.get(key) ?? []), topic]);
    }

    const rows: TopicRow[] = [];

    const walk = (parent: string, depth: number): void => {
      for (const topic of byParent.get(parent) ?? []) {
        rows.push({ topic, depth });

        // Bounded because a cycle is refused server-side; the depth cap is here
        // so a bad row in old data cannot hang the screen.
        if (depth < 4) {
          walk(topic.id, depth + 1);
        }
      }
    };

    walk('', 0);

    return rows;
  });

  /** Candidate parents for a topic: everything except itself. */
  readonly parentOptions = computed(() =>
    (this.selected()?.topics ?? []).filter(t => t.id !== this.topicDraft().id),
  );

  // --- the drafts. One open at a time per kind, because two half-typed rows on
  //     one screen is how somebody saves the wrong one.
  readonly categoryDraft = signal<CategoryDraft>(emptyCategory());
  readonly levelDraft = signal<LevelDraft>(emptyLevel());
  readonly topicDraft = signal<TopicDraft>(emptyTopic());

  readonly vocabulary = signal<CategorySet | null>(null);
  readonly vocabularyOpen = signal(false);
  readonly vocabularySaving = signal(false);
  readonly vocabularySaved = signal(false);

  readonly saving = signal(false);
  readonly pendingDelete = signal<PendingDelete | null>(null);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.catalog.getCategories(this.includeInactive()).subscribe({
      next: categories => {
        this.categories.set(categories);

        // Keep the selection across a reload when it still exists, so saving a
        // level does not throw the author back to the first domain.
        const stillThere = categories.some(c => c.id === this.selectedId());
        this.selectedId.set(stillThere ? this.selectedId() : (categories[0]?.id ?? null));

        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  toggleInactive(): void {
    this.includeInactive.update(v => !v);
    this.load();
  }

  select(id: string): void {
    this.selectedId.set(id);
    this.levelDraft.set(emptyLevel());
    this.topicDraft.set(emptyTopic());
  }

  // ------------------------------------------------------------- categories

  newCategory(): void {
    this.categoryDraft.set({ ...emptyCategory(), open: true });
  }

  editCategory(category: CategoryDto): void {
    this.categoryDraft.set({
      open: true,
      id: category.id,
      name: category.name,
      code: category.code,
      description: category.description ?? '',
      displayOrder: category.displayOrder,
      isActive: category.isActive,
    });
  }

  cancelCategory(): void {
    this.categoryDraft.set(emptyCategory());
  }

  saveCategory(): void {
    const draft = this.categoryDraft();
    const body: CreateUpdateCategoryDto = {
      name: draft.name.trim(),
      code: (draft.code || suggestCode(draft.name)).trim(),
      description: draft.description.trim() || null,
      displayOrder: draft.displayOrder,
      isActive: draft.isActive,
    };

    if (!body.name || !body.code) {
      return;
    }

    this.run(
      draft.id ? this.catalog.updateCategory(draft.id, body) : this.catalog.createCategory(body),
      saved => {
        this.categoryDraft.set(emptyCategory());
        this.selectedId.set(saved.id);
      },
    );
  }

  // ----------------------------------------------------------------- levels

  newLevel(): void {
    const existing = this.selected()?.levels ?? [];

    this.levelDraft.set({
      ...emptyLevel(),
      open: true,
      // Appended to the ladder rather than dropped at the top. A new rung is
      // almost always the next one up.
      displayOrder: existing.length ? Math.max(...existing.map(l => l.displayOrder)) + 1 : 1,
    });
  }

  editLevel(level: LevelDto): void {
    this.levelDraft.set({
      open: true,
      id: level.id,
      name: level.name,
      code: level.code,
      displayOrder: level.displayOrder,
      isActive: level.isActive,
      appliesEverywhere: !level.categoryId,
    });
  }

  cancelLevel(): void {
    this.levelDraft.set(emptyLevel());
  }

  saveLevel(): void {
    const draft = this.levelDraft();
    const body: CreateUpdateLevelDto = {
      categoryId: draft.appliesEverywhere ? null : this.selectedId(),
      name: draft.name.trim(),
      code: (draft.code || suggestCode(draft.name)).trim(),
      displayOrder: draft.displayOrder,
      isActive: draft.isActive,
    };

    if (!body.name || !body.code) {
      return;
    }

    this.run(
      draft.id ? this.catalog.updateLevel(draft.id, body) : this.catalog.createLevel(body),
      () => this.levelDraft.set(emptyLevel()),
    );
  }

  // ----------------------------------------------------------------- topics

  newTopic(parentId: string | null = null): void {
    this.topicDraft.set({ ...emptyTopic(), open: true, parentId });
  }

  editTopic(topic: TopicDto): void {
    this.topicDraft.set({
      open: true,
      id: topic.id,
      name: topic.name,
      code: topic.code,
      parentId: topic.parentId ?? null,
      displayOrder: topic.displayOrder,
      isActive: topic.isActive,
    });
  }

  cancelTopic(): void {
    this.topicDraft.set(emptyTopic());
  }

  saveTopic(): void {
    const draft = this.topicDraft();
    const body: CreateUpdateTopicDto = {
      categoryId: this.selectedId(),
      name: draft.name.trim(),
      code: (draft.code || suggestCode(draft.name)).trim(),
      parentId: draft.parentId,
      displayOrder: draft.displayOrder,
      isActive: draft.isActive,
    };

    if (!body.name || !body.code) {
      return;
    }

    this.run(
      draft.id ? this.catalog.updateTopic(draft.id, body) : this.catalog.createTopic(body),
      () => this.topicDraft.set(emptyTopic()),
    );
  }

  // ---------------------------------------------------------------- deleting

  askDelete(kind: PendingDelete['kind'], id: string, name: string): void {
    this.pendingDelete.set({ kind, id, name });
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const pending = this.pendingDelete();

    if (!pending) {
      return;
    }

    const request =
      pending.kind === 'category'
        ? this.catalog.deleteCategory(pending.id)
        : pending.kind === 'level'
          ? this.catalog.deleteLevel(pending.id)
          : this.catalog.deleteTopic(pending.id);

    this.run(request, () => this.pendingDelete.set(null));
  }

  // -------------------------------------------------------------- vocabulary

  openVocabulary(): void {
    this.vocabularyOpen.set(true);
    this.vocabularySaved.set(false);

    if (!this.vocabulary()) {
      this.catalog.getVocabulary().subscribe({
        next: words => this.vocabulary.set(words),
        error: err => this.actionError.set(this.reason(err)),
      });
    }
  }

  closeVocabulary(): void {
    this.vocabularyOpen.set(false);
  }

  setWord(key: keyof CategorySet, value: string): void {
    const current = this.vocabulary();

    if (current) {
      this.vocabulary.set({ ...current, [key]: value });
    }
  }

  saveVocabulary(): void {
    const words = this.vocabulary();

    if (!words) {
      return;
    }

    this.vocabularySaving.set(true);
    this.actionError.set(null);

    this.catalog.updateVocabulary(words).subscribe({
      next: saved => {
        this.vocabulary.set(saved);
        this.vocabularySaving.set(false);
        this.vocabularySaved.set(true);
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.vocabularySaving.set(false);
      },
    });
  }

  /**
   * What is filed under this domain, as one phrase.
   *
   * Built here rather than in the template because the localisation call takes
   * strings, and a template that has to convert numbers on the way in is a
   * template nobody will keep tidy.
   */
  counts(category: CategoryDto): string {
    return this.t('::Catalog:Counts', String(category.examCount), String(category.questionCount));
  }

  /** Offered from the name, overtypable. Nobody should have to invent a slug. */
  suggest(name: string): string {
    return suggestCode(name);
  }

  // ------------------------------------------------------------------ plumbing

  private run<T>(request: Observable<T>, done: (value: T) => void): void {
    this.saving.set(true);
    this.actionError.set(null);

    request.subscribe({
      next: value => {
        done(value);
        this.saving.set(false);
        this.load();
      },
      error: (err: unknown) => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}

export interface TopicRow {
  readonly topic: TopicDto;
  readonly depth: number;
}

interface PendingDelete {
  readonly kind: 'category' | 'level' | 'topic';
  readonly id: string;
  readonly name: string;
}

interface CategoryDraft {
  open: boolean;
  id: string | null;
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
}

interface LevelDraft {
  open: boolean;
  id: string | null;
  name: string;
  code: string;
  displayOrder: number;
  isActive: boolean;

  /** A ladder shared across every domain, written once rather than once per subject. */
  appliesEverywhere: boolean;
}

interface TopicDraft {
  open: boolean;
  id: string | null;
  name: string;
  code: string;
  parentId: string | null;
  displayOrder: number;
  isActive: boolean;
}

const emptyCategory = (): CategoryDraft => ({
  open: false,
  id: null,
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  isActive: true,
});

const emptyLevel = (): LevelDraft => ({
  open: false,
  id: null,
  name: '',
  code: '',
  displayOrder: 0,
  isActive: true,
  appliesEverywhere: false,
});

const emptyTopic = (): TopicDraft => ({
  open: false,
  id: null,
  name: '',
  code: '',
  parentId: null,
  displayOrder: 0,
  isActive: true,
});

/**
 * A code from a name.
 *
 * Latin letters and digits survive, everything else becomes a hyphen. Arabic
 * names therefore produce nothing, and the field is left for the author to fill
 * — which is honest: a transliteration guessed by a regular expression is worse
 * than an empty box, because it looks deliberate.
 */
function suggestCode(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 32);
}
