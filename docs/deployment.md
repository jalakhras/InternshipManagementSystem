# Deployment | النشر

How to run this outside the machine it was written on. Nothing here changes local
development: every value below has a local default already in the repository, and
a deployment overrides it through the environment.

كل ما يلي لا يغيّر بيئة التطوير المحلية. كل قيمة لها قيمة افتراضية محلية موجودة في
المستودع، والنشر يتجاوزها عبر متغيّرات البيئة.

---

## 1. Quick start | البدء السريع

```bash
cp .env.example .env       # then fill in the three secrets it names
docker compose up --build
```

| | |
|---|---|
| SPA | http://localhost:8080 |
| API | http://localhost:8081 |
| Swagger | http://localhost:8081/swagger |
| Health | http://localhost:8081/health |
| SQL Server | `localhost,1433`, user `sa` |

`docker compose up` refuses to start, naming the variable, if any required secret
is missing. That is deliberate: a stack that comes up with a blank exam-session
signing key is worse than one that does not come up.

The stack runs over plain HTTP on loopback. That is what lets it work with no
certificates — OpenIddict permits a loopback redirect URI over HTTP and refuses a
remote one. Anything reachable by a hostname needs TLS in front of it; see
section 6.

يعمل هذا التكوين عبر HTTP على العنوان المحلي فقط. أي نشر حقيقي يحتاج TLS — انظر
القسم السادس.

---

## 2. Environment variables | متغيّرات البيئة

ASP.NET Core maps a configuration key to an environment variable by replacing `:`
with `__`. So `App:ClientUrl` is `App__ClientUrl`. Every key in
`src/InternshipManagementSystem.HttpApi.Host/appsettings.json` can be overridden
this way, including ones not listed here.

### API host — required

| Variable | What it is |
|---|---|
| `ConnectionStrings__Default` | SQL Server connection string. **Keep `Max Pool Size=300`** — the default exhausted the pool at 150 concurrent candidates during a load test. |
| `ExamSession__SigningKey` | Signs the credential a candidate holds during an attempt. At least 32 characters, per environment, never shared with anything else. The host refuses to start without it. |
| `StringEncryption__DefaultPassPhrase` | ABP's pass phrase for settings stored encrypted in the database. Changing it after data exists makes what it protected unreadable. |

### API host — URLs

These are the addresses **a browser** uses, not container names. They end up in
the OpenID discovery document, in the CORS allow-list, and in the exam links that
are emailed to candidates — all read from outside the container network.

| Variable | Local default | Notes |
|---|---|---|
| `App__SelfUrl` | `https://localhost:44373` | Public URL of the API. |
| `App__ClientUrl` | `http://localhost:4200` | Public URL of the SPA. **This is what builds `{clientUrl}/exam/{token}`** in `AssignmentAppService`, so a wrong value emails candidates a dead link. |
| `App__CorsOrigins` | `https://*.InternshipManagementSystem.com,http://localhost:4200` | Comma-separated. Whitespace around entries is trimmed. |
| `App__RedirectAllowedUrls` | `http://localhost:4200,https://localhost:44383` | Comma-separated. |
| `AuthServer__Authority` | `https://localhost:44373` | The OIDC issuer. Must match what the SPA is told, exactly. |
| `AuthServer__RequireHttpsMetadata` | `false` | |

### API host — deployment concerns

| Variable | Default | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` in the image | |
| `ASPNETCORE_HTTP_PORTS` | `8080` in the image | |
| `BlobStoring__FileSystem__BasePath` | `App_Data/blobs` | Uploaded question media and candidate answer files. **Mount a volume.** These are not in the database; losing this directory loses the files. |
| `DataProtection__KeysPath` | unset | Mount a volume. Unset means the key ring lives in the container's writable layer and is regenerated on every restart. |
| `DataProtection__ApplicationName` | `InternshipManagementSystem` | Must match across replicas. |
| `OpenIddict__Certificate__Path` | unset | See section 6. Unset keeps ABP's development certificates. |
| `OpenIddict__Certificate__PassPhrase` | unset | |
| `ForwardedHeaders__Enabled` | `false` | Set `true` behind a reverse proxy or ingress. |
| `ForwardedHeaders__KnownNetworks` | unset | Comma-separated CIDR, e.g. `10.0.0.0/8`. Both lists empty means "trust the immediate caller", which is only safe when nothing but the proxy can reach the container. |
| `ForwardedHeaders__KnownProxies` | unset | Comma-separated IP addresses. |
| `Mailing__UseNullSender` | null | Null keeps the old behaviour: null sender in DEBUG builds, real SMTP otherwise. Set `true` to deploy before a mail relay exists. |
| `Settings__Abp.Mailing.Smtp.Host` etc. | `127.0.0.1:25` | The SMTP relay, once there is one. |
| `ExamSession__Issuer` / `__Audience` | `ims-exam-session` | Per-deployment labels. Distinct values stop a token minted against staging from validating against production if a signing key is ever copied between them. |
| `ExamSession__MediaIssuer` / `__MediaAudience` | `ims-exam-media` | The same, for the grants that let a candidate's browser fetch one attached file. |

### Migrator

| Variable | Notes |
|---|---|
| `ConnectionStrings__Default` | As above. |
| `StringEncryption__DefaultPassPhrase` | The same value as the API. It seeds encrypted rows. |
| `OpenIddict__Applications__InternshipManagementSystem_App__RootUrl` | The SPA's public URL. This is registered on the OAuth client as its redirect target; get it wrong and login fails at the last step with "invalid redirect_uri" after everything else appeared to work. |
| `OpenIddict__Applications__InternshipManagementSystem_Swagger__RootUrl` | The API's public URL. |

### SPA

The Angular bundle is static files. It reads `assets/config.json` before it boots
and overlays whatever that file names on top of the values compiled in, so **one
image is promoted between environments** rather than rebuilt per environment. The
container writes that file at start from these variables.

| Variable | Notes |
|---|---|
| `API_URL` | Origin of the API, as the browser reaches it. |
| `OAUTH_ISSUER` | Defaults to `API_URL`. Must match `AuthServer__Authority` exactly. |
| `OAUTH_CLIENT_ID` | `InternshipManagementSystem_App`. |
| `OAUTH_SCOPE` | `offline_access InternshipManagementSystem`. |
| `OAUTH_REQUIRE_HTTPS` | `true` everywhere it can be. False only for a plain-HTTP local stack. |
| `APP_NAME` | Name shown in the shell. |
| `SPA_BASE_URL` | Normally leave unset. The app then reads its own origin from the page, which stays correct however the container is published and is the only thing that works when it is reached over more than one hostname. |

Nothing else in the bundle is environment-specific. Anything not set here keeps
the value compiled into the build.

---

## 3. Migrations | ترحيل قاعدة البيانات

The migrator is a separate short-lived container running the same code as the API,
not a step inside the API's own startup. With more than one API replica,
migrate-on-start races itself; as its own container it either exits 0 or it does
not, and nothing else starts until it has.

مُرحِّل قاعدة البيانات حاوية منفصلة قصيرة العمر، وليس جزءًا من إقلاع الـ API.

**In compose** it runs automatically, before the API:

```bash
docker compose up migrator          # just the migration
docker compose run --rm migrator    # again, e.g. after adding a migration
```

**Standalone**, against any database:

```bash
docker build -f docker/api/Dockerfile --target migrator -t ims-migrator .

docker run --rm \
  -e ConnectionStrings__Default="Server=…;Max Pool Size=300" \
  -e StringEncryption__DefaultPassPhrase="…" \
  -e OpenIddict__Applications__InternshipManagementSystem_App__RootUrl="https://exams.example.org" \
  ims-migrator
```

**Locally**, without containers:

```bash
dotnet run --project src/InternshipManagementSystem.DbMigrator
```

The migrator applies EF Core migrations, seeds the permission and role data, and
registers the OpenIddict clients. It is safe to run repeatedly. Re-run it whenever
the SPA's public URL changes, because that URL is stored on the OAuth client.

To create a new migration (from the EntityFrameworkCore project):

```bash
dotnet ef migrations add <Name> --project src/InternshipManagementSystem.EntityFrameworkCore
```

---

## 4. Building the images | بناء الصور

Both build from the repository root, because central package management lives
there and every project imports it.

```bash
docker build -f docker/api/Dockerfile                    -t ims-api      .
docker build -f docker/api/Dockerfile --target migrator  -t ims-migrator .
docker build -f docker/angular/Dockerfile                -t ims-web      .
```

The API image is multi-stage — SDK to build, ASP.NET runtime to run — and the
process runs as the non-root `app` user that the .NET base image ships with. The
SPA image builds with Node and serves with `nginx-unprivileged`, which listens on
8080 as a non-root user.

---

## 5. The `/exam/:token` route

A candidate opens their link from an email, so `/exam/<token>` is always a cold
navigation and never a client-side route change. `docker/angular/nginx.conf`
handles it explicitly rather than leaving it to the general SPA fallback, because
it needs three things an ordinary deep link does not:

- **The SPA fallback**, like any deep link — without it every invitation 404s.
- **`Cache-Control: no-store`**, because the token in the path is the whole
  credential. A shared proxy on a school or corporate network caching that response
  would serve one candidate's exam to the next.
- **`Referrer-Policy: no-referrer`**, because the page loads webfonts from a third
  party and the token would otherwise leave in the `Referer` header.

---

## 6. What a real deployment still needs | ما يتبقّى لنشر حقيقي

The compose stack is a working demonstration, not a production topology. Before
real candidates:

1. **TLS**, terminated by a reverse proxy or ingress in front of both containers.
   Then set `ForwardedHeaders__Enabled=true` on the API — otherwise it sees plain
   HTTP and the proxy's address, and generated URLs carry the wrong scheme — and
   set `OAUTH_REQUIRE_HTTPS=true` on the SPA.

2. **An OpenIddict signing certificate.** Without `OpenIddict__Certificate__Path`,
   ABP generates development keys, and those are per-container: every token issued
   before a restart is rejected after it, and two replicas each reject the other's.

   ```bash
   dotnet dev-certs https -v -ep openiddict.pfx -p "<pass phrase>"
   ```

   Mount it read-only and pass the pass phrase as a secret. It is not a TLS
   certificate and never leaves the server.

3. **A managed database**, not the SQL Server container, which stores its data in
   a local volume and has no backups. Keep `Max Pool Size=300` in the connection
   string.

4. **Volumes with backups** for `BlobStoring__FileSystem__BasePath`. Uploaded
   question media and candidate answer files are on disk, not in the database, so
   a database backup alone does not restore an exam. For more than one API replica
   this has to become shared storage — S3 or Azure Blob — which is a change to the
   BLOB provider in `InternshipManagementSystemDomainModule`, not a rewrite.

5. **An SMTP relay**, and `Mailing__UseNullSender` unset. Until then invitations
   are generated but not delivered; the links are still returned by the API and can
   be distributed by hand.

6. **Secret management.** The compose file reads secrets from the environment; a
   real deployment should read them from whatever the platform provides rather
   than from a `.env` file on a host.

7. **Log shipping.** Serilog writes to `Logs/logs.txt` inside the container, which
   nothing collects and nothing rotates.

---

## 7. Continuous integration | التكامل المستمر

`.github/workflows/ci.yml` runs on every push and pull request:

- **backend** — restore, build, `dotnet test`. No database service: the
  integration tests run against SQLite in memory.
- **frontend** — `npm ci`, production build, then the Playwright `desktop` and
  `mobile` projects.

The `live` Playwright project is deliberately not in CI. It drives the real API
against a seeded database and writes rows; it cannot run without a host and a
database that the job does not have.

---

## 8. Local development is unchanged | التطوير المحلي كما هو

```bash
dotnet run --project src/InternshipManagementSystem.HttpApi.Host   # https://localhost:44373
cd angular && npm start                                            # http://localhost:4200
```

The values in `appsettings.json`, `angular/src/environments/environment.ts` and
`angular/src/assets/config.json` are the same local defaults as before. The only
new requirement is the one that already existed: `ExamSession:SigningKey`, set
through user-secrets or `appsettings.secrets.json`.
