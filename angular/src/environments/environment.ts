import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'InternshipManagementSystem',
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
