import { APIRequestContext, request } from '@playwright/test';

/**
 * A signed-in coordinator, talking to the real API.
 *
 * The live suite uses this for setup — creating a catalogue, an exam, questions,
 * a paper — and the browser for the parts that are actually about the browser:
 * what a candidate sees and what a coordinator reads afterwards.
 *
 * Driving setup through the API rather than the UI is deliberate. A journey test
 * that clicks through twelve authoring screens to reach the one behaviour it is
 * checking fails for twelve reasons, and eleven of them are somebody else's.
 */
export const API = 'https://localhost:44373';

export interface Coordinator {
  ctx: APIRequestContext;
  token: string;
}

export async function signIn(): Promise<Coordinator> {
  // The development certificate is self-signed.
  const anonymous = await request.newContext({ ignoreHTTPSErrors: true, baseURL: API });

  const auth = await anonymous.post('/connect/token', {
    form: {
      grant_type: 'password',
      username: process.env['SMOKE_USER'] ?? 'admin',
      password: process.env['SMOKE_PASSWORD'] ?? '1q2w3E*',
      client_id: 'InternshipManagementSystem_App',
      scope: 'InternshipManagementSystem offline_access openid profile',
    },
  });

  if (!auth.ok()) {
    throw new Error(
      `Could not sign in against ${API}. Is the host running and the database seeded? ` +
        `(${auth.status()})`,
    );
  }

  const token = (await auth.json()).access_token as string;

  const ctx = await request.newContext({
    ignoreHTTPSErrors: true,
    baseURL: API,
    extraHTTPHeaders: { Authorization: `Bearer ${token}` },
  });

  return { ctx, token };
}

/** POST/PUT that fails loudly. A silent 400 during setup produces a confusing failure later. */
export async function send<T>(
  ctx: APIRequestContext,
  method: 'post' | 'put' | 'get' | 'delete',
  url: string,
  body?: unknown,
): Promise<T> {
  const res = await ctx[method](url, body === undefined ? undefined : { data: body });

  if (!res.ok()) {
    throw new Error(`${method.toUpperCase()} ${url} → ${res.status()}: ${await res.text()}`);
  }

  const text = await res.text();

  return (text ? JSON.parse(text) : undefined) as T;
}

/** A suffix that keeps one run's rows apart from the last one's. */
export function unique(prefix: string): string {
  return `${prefix}-${Date.now().toString(36)}${Math.floor(Math.random() * 1e4).toString(36)}`;
}
