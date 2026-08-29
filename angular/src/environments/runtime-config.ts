import { Environment } from '@abp/ng.core';

/**
 * Where the API is, decided at container start rather than at build time.
 *
 * A production Angular build is a folder of static files with the API URL, the
 * OpenID issuer and the redirect URI compiled into a hashed bundle. That is fine
 * while there is one environment and it is the author's laptop; the moment there
 * are two, the same artifact can no longer be promoted from staging to production,
 * because the URLs it was built with are the wrong ones. Rebuilding per environment
 * means the thing that was tested is not the thing that ships.
 *
 * So the bundle carries local-development defaults and, before Angular boots,
 * overlays whatever `assets/config.json` says. The image writes that one small file
 * from environment variables on start. Nothing changes for `ng serve`: the file
 * shipped in `src/assets` holds exactly the values that were compiled in.
 */
export interface RuntimeConfig {
  /** Origin of the API host, no trailing slash. */
  apiUrl?: string;

  /** OpenID Connect issuer. Usually the API host, and it must match exactly what the token says. */
  issuer?: string;

  /**
   * Public origin of this SPA, used as the OAuth redirect target.
   *
   * Left unset it is read from the page's own base URI, which is right far more
   * often than a hand-written value: the browser knows where it loaded the app
   * from, and a mismatch here is an auth loop that only appears after deployment.
   */
  baseUrl?: string;

  clientId?: string;
  scope?: string;

  /**
   * Whether the OAuth client refuses a non-HTTPS issuer.
   *
   * True everywhere it can be. It has to be false to run the stack over plain HTTP
   * on a laptop — `docker compose up` does exactly that — and false in a real
   * deployment is a finding, not a setting.
   */
  requireHttps?: boolean;

  /** Display name in the shell. */
  appName?: string;
}

/**
 * Overlays the deployed configuration onto the compiled environment, in place.
 *
 * In place, because `environment` is imported by services that read it when they
 * are constructed and by the ABP providers that capture the object. Replacing the
 * reference would leave half the app looking at the old one; mutating the object
 * everybody already holds cannot.
 */
export async function applyRuntimeConfig(environment: Environment): Promise<void> {
  const config = await loadRuntimeConfig();

  const baseUrl = trimSlash(config.baseUrl) || currentOrigin();
  const apiUrl = trimSlash(config.apiUrl);
  const issuer = config.issuer?.trim();

  if (baseUrl) {
    environment.application!.baseUrl = baseUrl;
    environment.oAuthConfig!.redirectUri = baseUrl;
  }

  if (config.appName) {
    environment.application!.name = config.appName;
  }

  if (apiUrl) {
    environment.apis!['default'].url = apiUrl;
  }

  if (issuer) {
    // ABP's OAuth client compares this against the `iss` claim, and a missing
    // trailing slash is the most common way that comparison fails while every
    // other part of the setup looks correct.
    environment.oAuthConfig!.issuer = issuer.endsWith('/') ? issuer : issuer + '/';
  }

  if (config.clientId) {
    environment.oAuthConfig!.clientId = config.clientId;
  }

  if (config.scope) {
    environment.oAuthConfig!.scope = config.scope;
  }

  if (typeof config.requireHttps === 'boolean') {
    environment.oAuthConfig!.requireHttps = config.requireHttps;
  }
}

/**
 * Reads `assets/config.json`, and treats every way it can fail as "there is no
 * deployed configuration".
 *
 * A missing file is the normal case for a plain `ng build` that nobody has
 * containerised. A malformed one is a deployment mistake, and it is logged loudly
 * rather than thrown: an app that renders with the wrong API URL can be diagnosed
 * from the console, while one that shows a blank page cannot.
 */
async function loadRuntimeConfig(): Promise<RuntimeConfig> {
  const url = new URL('assets/config.json', document.baseURI).toString();

  try {
    const response = await fetch(url, { cache: 'no-store' });

    if (!response.ok) {
      return {};
    }

    const parsed: unknown = await response.json();

    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      console.error('[runtime-config] assets/config.json is not an object; ignoring it.');
      return {};
    }

    return parsed as RuntimeConfig;
  } catch (error) {
    console.error('[runtime-config] could not read assets/config.json; using compiled defaults.', error);
    return {};
  }
}

function currentOrigin(): string {
  return trimSlash(new URL(document.baseURI).href);
}

function trimSlash(value: string | undefined): string {
  return (value ?? '').trim().replace(/\/+$/, '');
}
