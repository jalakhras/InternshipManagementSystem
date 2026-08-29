import { Environment } from '@abp/ng.core';

/**
 * Local development defaults.
 *
 * These are fallbacks, not the deployed configuration. `assets/config.json` is read
 * before the app boots and overlays anything it names — see `runtime-config.ts`.
 * What is here is what a developer gets with no config file present, and it matches
 * the ports the API host uses in `Properties/launchSettings.json`.
 */
const baseUrl = 'http://localhost:4200';

export const environment = {
  production: false,
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
