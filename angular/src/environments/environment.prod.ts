import { Environment } from '@abp/ng.core';

/**
 * Production build defaults.
 *
 * Deliberately still pointing at localhost. A production bundle is built once and
 * promoted between environments, so it cannot name any of them; the real URLs
 * arrive at container start through `assets/config.json` and are overlaid by
 * `runtime-config.ts` before the app boots.
 *
 * Keeping the local values here rather than a placeholder means a production build
 * run on a developer's machine still works against their own API, and a deployment
 * that forgot to supply a config file fails visibly against an unreachable
 * localhost rather than half-working against something unexpected.
 */
const baseUrl = 'http://localhost:4200';

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'Astrolabe',
    logoUrl: '',
  },
  oAuthConfig: {
    issuer: 'https://localhost:44373/',
    redirectUri: baseUrl,
    clientId: 'InternshipManagementSystem_App',
    responseType: 'code',
    scope: 'offline_access InternshipManagementSystem',
    requireHttps: true,
  },
  apis: {
    default: {
      url: 'https://localhost:44373',
      rootNamespace: 'InternshipManagementSystem',
    },
  },
} as Environment;
