import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

import {
  CategoryDto,
  CategorySet,
  CreateUpdateCategoryDto,
  CreateUpdateLevelDto,
  CreateUpdateTopicDto,
  LevelDto,
  TopicDto,
} from './catalog.models';

/**
 * The catalogue everything else is filed against.
 *
 * Thin like its siblings: one method per route, wire types out, no cache. The
 * catalogue is small enough that a screen refetching it costs nothing, and a
 * service that also caches is a second source of truth for the same data.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/catalog';

  /** Domains with their levels and topics. One call, because every caller needs all three. */
  getCategories(includeInactive = false): Observable<CategoryDto[]> {
    return this.rest.request<void, CategoryDto[]>({
      method: 'GET',
      url: `${this.base}/categories`,
      params: { includeInactive },
    });
  }

  createCategory(body: CreateUpdateCategoryDto): Observable<CategoryDto> {
    return this.rest.request<CreateUpdateCategoryDto, CategoryDto>({
      method: 'POST',
      url: `${this.base}/categories`,
      body,
    });
  }

  updateCategory(id: string, body: CreateUpdateCategoryDto): Observable<CategoryDto> {
    return this.rest.request<CreateUpdateCategoryDto, CategoryDto>({
      method: 'PUT',
      url: `${this.base}/categories/${id}`,
      body,
    });
  }

  deleteCategory(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/categories/${id}` });
  }

  createLevel(body: CreateUpdateLevelDto): Observable<LevelDto> {
    return this.rest.request<CreateUpdateLevelDto, LevelDto>({
      method: 'POST',
      url: `${this.base}/levels`,
      body,
    });
  }

  updateLevel(id: string, body: CreateUpdateLevelDto): Observable<LevelDto> {
    return this.rest.request<CreateUpdateLevelDto, LevelDto>({
      method: 'PUT',
      url: `${this.base}/levels/${id}`,
      body,
    });
  }

  deleteLevel(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/levels/${id}` });
  }

  createTopic(body: CreateUpdateTopicDto): Observable<TopicDto> {
    return this.rest.request<CreateUpdateTopicDto, TopicDto>({
      method: 'POST',
      url: `${this.base}/topics`,
      body,
    });
  }

  updateTopic(id: string, body: CreateUpdateTopicDto): Observable<TopicDto> {
    return this.rest.request<CreateUpdateTopicDto, TopicDto>({
      method: 'PUT',
      url: `${this.base}/topics/${id}`,
      body,
    });
  }

  deleteTopic(id: string): Observable<void> {
    return this.rest.request<void, void>({ method: 'DELETE', url: `${this.base}/topics/${id}` });
  }

  getVocabulary(): Observable<CategorySet> {
    return this.rest.request<void, CategorySet>({ method: 'GET', url: `${this.base}/vocabulary` });
  }

  updateVocabulary(body: CategorySet): Observable<CategorySet> {
    return this.rest.request<CategorySet, CategorySet>({
      method: 'PUT',
      url: `${this.base}/vocabulary`,
      body,
    });
  }
}
