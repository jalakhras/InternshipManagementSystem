/**
 * The organisation's own vocabulary.
 *
 * "Category" is our word, not the customer's. A language centre calls these
 * languages, a factory calls them competencies, a broker calls them desks —
 * which is what {@link CategorySet} is for.
 */
export interface CategoryDto {
  id: string;
  name: string;

  /** Short, stable, and the thing a spreadsheet import matches on. */
  code: string;

  description?: string | null;
  displayOrder: number;
  isActive: boolean;

  /** Levels under this domain, in ladder order. Beginner before advanced. */
  levels: LevelDto[];

  /** Topics under this domain, flat, each carrying its parent. */
  topics: TopicDto[];

  /** What deactivating this would affect, shown before anybody does it. */
  examCount: number;
  questionCount: number;
}

export interface LevelDto {
  id: string;
  categoryId?: string | null;
  name: string;
  code: string;
  displayOrder: number;
  isActive: boolean;
}

export interface TopicDto {
  id: string;
  categoryId?: string | null;
  name: string;
  code: string;

  /** Null at the top. "Grammar" holds "tenses" holds "past perfect". */
  parentId?: string | null;

  displayOrder: number;
  isActive: boolean;
}

export interface CreateUpdateCategoryDto {
  name: string;
  code: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateUpdateLevelDto {
  /** Null means it applies under every domain. */
  categoryId?: string | null;
  name: string;
  code: string;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateUpdateTopicDto {
  categoryId?: string | null;
  name: string;
  code: string;
  parentId?: string | null;
  displayOrder: number;
  isActive: boolean;
}

/**
 * What this organisation calls things.
 *
 * Not decoration. Staff who see their own words trust what the screen is
 * telling them; staff who see somebody else's spend the first week translating.
 */
export interface CategorySet {
  singularName: string;
  pluralName: string;
  subjectSingularName: string;
  subjectPluralName: string;
  groupSingularName: string;
  groupPluralName: string;
}
