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

/**
 * Signing in the way a person does: the login form.
 * <para>
 * Every other helper here takes a token from `/connect/token`, and the stubbed
 * browser suite replaces the server altogether. So the product has had two kinds
 * of coverage — real screens against a fake server, and a real server with no
 * screens — and neither of them is a person clicking a real screen against a
 * real server. Three journeys the owner reported broken on 2026-08-30 all lived
 * in exactly that gap, and every one of 350 backend, 258 browser and 26 live
 * tests passed while they were broken.
 * </para>
 */
export async function signInThroughTheForm(
  page: import('@playwright/test').Page,
  user = 'admin',
  password = '1q2w3E*',
): Promise<void> {
  await page.goto('/');

  // ABP serves the login page from the API origin and returns here afterwards.
  await page.waitForURL(/\/Account\/Login/, { timeout: 30_000 });

  await page.locator('#LoginInput_UserNameOrEmailAddress').fill(user);
  await page.locator('#LoginInput_Password').fill(password);
  await page.getByRole('button', { name: /Login|دخول/ }).click();

  await page.waitForURL(/localhost:4200/, { timeout: 30_000 });
}
