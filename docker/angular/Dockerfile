# The Angular SPA, built once and served as static files by nginx.
#
# Build context is the repository root, to stay symmetrical with the API image:
#
#   docker build -f docker/angular/Dockerfile -t ims-web .

# ---------------------------------------------------------------------------
# build
# ---------------------------------------------------------------------------
FROM node:24-alpine AS build
WORKDIR /app

# Lockfile first: dependencies change far less often than application code, so an
# edit to a component reuses this layer instead of reinstalling Angular.
COPY angular/package.json angular/package-lock.json ./
RUN npm ci

COPY angular/ ./

# The production configuration, which is what applies the budgets and the output
# hashing. The URLs it compiles in are only fallbacks — the real ones are written
# into assets/config.json when this container starts.
RUN npx ng build --configuration production

# ---------------------------------------------------------------------------
# final
#
# nginx-unprivileged rather than nginx: it runs as a non-root user out of the box
# and listens on 8080, so nothing here needs a capability to bind a low port. The
# entrypoint has to rewrite assets/config.json at start, so that directory is owned
# by the runtime user rather than by root.
# ---------------------------------------------------------------------------
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

COPY docker/angular/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/angular/docker-entrypoint.d/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh

USER root
COPY --from=build /app/dist/astrolabe/browser /usr/share/nginx/html
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh \
 && chown -R nginx:nginx /usr/share/nginx/html
USER nginx

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=5 \
    CMD wget --quiet --spider http://localhost:8080/index.html || exit 1
