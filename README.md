# SerenAuth

> Calm authorization. Faster care.
> Prior authorization software built specifically for dialysis clinics.

SerenAuth is a HIPAA-conscious prior authorization workflow platform.
This repository contains the full stack: a .NET 8 / HotChocolate GraphQL API,
a Next.js 14 web app, a MongoDB persistence layer, and the supporting CI/CD,
Docker, and security tooling.

See [Architecture](#architecture), [Local development](#local-development),
[Testing](#testing), and [Security posture](#security-posture) below.

---

## Architecture

```text
                ┌──────────────────────────┐
                │   Next.js 14 (App Dir)   │
                │  Tailwind · Apollo Client│
                └────────────┬─────────────┘
                             │ GraphQL (HTTPS)
                             ▼
                ┌──────────────────────────┐
                │  ASP.NET Core 8 API      │
                │  HotChocolate · MediatR  │
                │  JWT · RBAC · Policies   │
                │  Serilog · Audit Log     │
                └────────────┬─────────────┘
                             │ MongoDB.Driver
                             ▼
                ┌──────────────────────────┐
                │       MongoDB 7          │
                │ org/users/providers/...  │
                │ append-only audit_events │
                └──────────────────────────┘
```

Clean Architecture layers:

* **Domain** – entities, value objects, status transitions. Pure C#.
* **Application** – MediatR commands/queries, FluentValidation, DTOs.
* **Infrastructure** – MongoDB, JWT, password hashing, audit sink.
* **Api** – ASP.NET host: HotChocolate, middleware, auth/policies.

## Local development

Prerequisites: Docker, .NET 8 SDK, Node 20.

```bash
cp .env.example .env
./infrastructure/scripts/dev-up.sh
```

This starts `mongo`, `api` (on `:8080`), and `web` (on `:3000`).
Open <http://localhost:3000>. GraphQL endpoint: <http://localhost:8080/graphql>.

Manual (without Docker):

```bash
# API
dotnet run --project src/SerenAuth.Api

# Web
npm --prefix apps/web ci
npm --prefix apps/web run dev
```

## Environment variables

All secrets are loaded from environment variables. See `.env.example`.

| Variable | Purpose |
| --- | --- |
| `Jwt__SigningKey` | HS256 signing key (≥ 64 random bytes). |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer + audience validation. |
| `Mongo__ConnectionString` | Mongo URI. |
| `Mongo__Database` | Database name (default `serenauth`). |
| `Seeding__Enabled` | Seed demo org/users/PAs at startup (dev only). |
| `Cors__AllowedOrigins` | Comma-separated CORS allowlist. |
| `RateLimit__PermitLimit` / `RateLimit__WindowSeconds` | Per-IP rate limit. |
| `NEXT_PUBLIC_GRAPHQL_ENDPOINT` | GraphQL endpoint the web app calls. |

## Testing

```bash
# Backend
dotnet test SerenAuth.sln --collect:"XPlat Code Coverage"

# Web
npm --prefix apps/web test -- --ci --coverage
```

Coverage threshold is enforced at **85%** in CI.

## Security posture

* JWT Bearer auth with HS256, issuer + audience validation.
* Role + policy authorization on every GraphQL resolver.
* Per-tenant `organizationId` filter is enforced server-side. The client
  cannot widen the scope of any query.
* PBKDF2-SHA256 password hashing (100k iterations, 16-byte salt, 32-byte
  output).
* AuditEvent collection is append-only. `CREATE_PA`, `UPDATE_PA`,
  `SUBMIT_PA`, `VIEW_PA`, and `LOGIN` events are recorded with
  timestamp, userId, organizationId, entity, entityId, action,
  ipAddress, and correlationId.
* Security headers: HSTS, X-Content-Type-Options, X-Frame-Options,
  Referrer-Policy, Permissions-Policy.
* CORS allowlisted; rate-limited (per-IP fixed window).
* GraphQL depth limit (8) and complexity limit (200). Introspection
  disabled outside Development. RFC 7807 ProblemDetails responses.
* All structured logs include a correlation ID. PHI fields are never
  logged.

## Compliance notes

SerenAuth's architecture targets technical safeguards under HIPAA
§164.312 and SOC 2 CC6/CC7 controls. The codebase does not, by itself,
constitute a HIPAA compliance program — production deployments
additionally require BAAs, key management (KMS), encryption-at-rest
(MongoDB Atlas), access reviews, and a written incident response plan.

## Project layout

```
apps/web                 Next.js 14 (App Router) + Tailwind + Apollo
src/SerenAuth.Domain     Pure domain model
src/SerenAuth.Application Commands, queries, validators, DTOs
src/SerenAuth.Infrastructure MongoDB, JWT, audit, password hashing
src/SerenAuth.Api        ASP.NET host + HotChocolate + middleware
tests/SerenAuth.UnitTests       xUnit + FluentAssertions
tests/SerenAuth.IntegrationTests Testcontainers Mongo + WebApplicationFactory
packages/shared-types    TS types shared by web
infrastructure/docker    Dockerfiles, docker-compose, mongo init
infrastructure/scripts   dev-up, seed, gen-jwt-secret
.github/workflows        CI, CodeQL, dependency review, secret scan
```
