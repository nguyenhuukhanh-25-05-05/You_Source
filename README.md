# Starter Kit - you_source

Full-stack starter kit: ASP.NET Core 8 API + Vue 3 + Tailwind CSS.

## Tech Stack

### Backend
- ASP.NET Core 8 Web API
- Entity Framework Core + SQL Server
- ASP.NET Identity (JWT Authentication)
- Serilog (Logging)
- FluentValidation
- Swagger

### Frontend
- Vue 3 (Composition API)
- Vue Router 4
- Pinia (State Management)
- Axios (HTTP Client + Refresh Token)
- Tailwind CSS v4
- Vite

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- SQL Server (local or Docker)

### One-command setup

```bash
setup.bat
```

This restores + builds backend, installs `dotnet-ef`, auto-configures dev User Secrets (JWT key + admin password), and installs frontend deps. Then run:

```bash
run-backend.bat    # API on http://localhost:5000/swagger
run-frontend.bat   # UI  on http://localhost:5173
```

### Dev admin login

- Username: `admin`
- Password: `Admin@12345` (auto-set in User Secrets by `setup-backend.bat`)

### Docker

```bash
copy .env.example .env   # then edit values
docker-compose up --build
```

## Dev vs Production

Security policy is **env-aware** so dev is easy to code against, prod is locked down:

| Setting | Development | Production |
|---|---|---|
| Lockout | 50 fails / 1 min | 5 fails / 15 min |
| CSP | `style-src 'unsafe-inline'` (Vue/Tailwind) | strict, no `unsafe-inline` |
| Exception messages | shown (debuggable) | hidden for 5xx |
| Swagger | enabled | disabled |
| HTTPS metadata | not required | required |
| HSTS | off | on |

Dev values live in `appsettings.Development.json`, prod in `appsettings.json` + env vars. Override any via `Identity:Lockout:*` / `Security:Csp` config keys.

## Default Admin Account

- Username: `admin`
- Password: set via `AdminSettings:Password` (User Secrets in dev, env var `ADMIN_PASSWORD` in Docker)
- Default fallback: `Admin@12345` (logged warning at startup — **change in prod**)

## Security

- **JWT secret**: must be >= 32 chars, validated at startup. Dev via User Secrets, prod via env.
- **Refresh tokens**: stored **hashed (SHA-256)** in a dedicated `RefreshTokens` table (1 record per session/device → multi-device support). Looked up by hash; rotated on every refresh. **Reuse detection**: if a revoked/used refresh token is presented again (suspected theft), all of that user's sessions are revoked. Per-session revoke on logout.
- **Token storage**: access + refresh tokens delivered via **HttpOnly, SameSite=Strict, Secure (prod)** cookies — not exposed to JS (XSS can't steal them). Frontend only stores the non-sensitive user profile (`username`/`fullName`/`roles`) in `localStorage` for UI state. CSRF mitigated by SameSite=Strict.
- **Passwords**: min 8 / max 100 chars, must include upper/lower/digit/non-alphanumeric. **Lockout** after 5 failed attempts (15 min) — actually enforced via `AccessFailedAsync`/`ResetAccessFailedCountAsync`/`IsLockedOutAsync`. Constant-time login (fake hash on unknown user).
- **Rate limiting**: 100 req/min/IP global; 5 req/min/IP on `login`/`register` (brute-force protection).
- **File upload**: extension whitelist, size cap (5MB), **magic-bytes signature check** (reject renamed `.exe`→`.pdf`), path-traversal protected (paths confined to `wwwroot`), `FileMode.CreateNew` (no overwrite).
- **Security headers**: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, COOP/CORP, **CSP** (configurable via `Security:Csp`), HSTS in production.
- **HTTPS**: `RequireHttpsMetadata` enforced in production.
- **Error responses**: production hides internal exception messages for 5xx; `X-Correlation-ID` header for tracing.
- **Audit log**: sensitive fields opt-out via `[AuditIgnore]` (refresh token, etc.).
- **Request limits**: body 10MB, header/line 8KB, max 1000 concurrent connections (Kestrel) — DoS mitigation.

## Configuration (Secrets)

The JWT secret is **NOT** committed. Set it per environment:

### Development (User Secrets)

```bash
cd backend\AppApi
dotnet user-secrets set "JwtSettings:Secret" "YourDevKey_AtLeast32CharactersLong!"
```

Already pre-set via `setup-backend.bat`. Verify with `dotnet user-secrets list`.

### Production (Environment Variable)

```
JwtSettings__Secret=<strong-random-key-min-32-chars>
ConnectionStrings__DefaultConnection=<prod-db>
Cors__AllowedOrigins__0=https://yourdomain.com
```

### Docker

Copy `.env.example` to `.env` and fill in `JWT_SECRET`, `SQL_SA_PASSWORD`, etc. `.env` is git-ignored.

> If the secret is missing or < 32 chars, the API throws on startup.

## Project Structure

```
you_source/
├── backend/
│   └── AppApi/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── Extensions/
│       ├── Helpers/
│       ├── Migrations/
│       ├── Middleware/
│       ├── Models/
│       ├── Services/
│       ├── wwwroot/
│       └── Program.cs
├── frontend/
│   └── starter-ui/
│       └── src/
│           ├── assets/
│           ├── components/
│           ├── layouts/
│           ├── router/
│           ├── services/
│           ├── stores/
│           └── views/
├── docs/
├── docker-compose.yml
└── setup.bat
```

## Features

- JWT Authentication + Refresh Token (HttpOnly cookies, multi-device sessions)
- Role-based Authorization (Admin/User)
- `/api/auth/me` endpoint (rehydrate session on reload)
- Global Exception Handling
- FluentValidation
- Pagination
- Standardized API Response
- Serilog Logging
- Swagger with JWT support
- Health Check + Rate Limiting
- Audit Log + Soft Delete (interceptor)
- File Upload Service
- Vue Router Auth Guard + Role Guard
- Axios Interceptors (auto refresh token)
- Admin Layout
- 404 Page
- Reusable Components (DataTable, Modal, Pagination)

## Production Hardening Checklist

Before going live, go through this:

- [ ] Generate a **strong random JWT secret** (>= 32 chars) — `openssl rand -base64 48` — set via env `JwtSettings__Secret`
- [ ] Change **admin password** via env `ADMIN_PASSWORD` (not the dev default)
- [ ] Set **SQL Server password** via `SQL_SA_PASSWORD` (not the compose default)
- [ ] Set `Cors:AllowedOrigins` to your real frontend domain (remove localhost)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Put behind **HTTPS** (nginx + Let's Encrypt) — `RequireHttpsMetadata` + HSTS then active
- [ ] Verify CSP doesn't break your app in prod (check browser console)
- [ ] Confirm Swagger is disabled (auto in non-dev)
- [ ] Run DB migrations / confirm `MigrateAsync` succeeds
- [ ] Set up **backups** (DB + `wwwroot/uploads/`)
- [ ] Rotate secrets if any leak suspected
