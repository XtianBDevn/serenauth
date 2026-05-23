# SerenAuth — End-to-End Process

> Companion to the README. This document describes how a prior authorization
> moves through the system from sign-in to a final decision, with a focus
> on which layer enforces which guarantee.

---

## 1. Request lifecycle (high level)

```mermaid
flowchart TD
    U[User<br/>Admin · Clinician · Intake · Viewer] -->|HTTPS| W[Next.js 14 Web<br/>apps/web]
    W -->|GraphQL + Bearer JWT| API[ASP.NET Core 8 API<br/>src/SerenAuth.Api]

    subgraph API[" "]
        direction TB
        MW[Correlation · SecurityHeaders<br/>GlobalException · CORS · RateLimit]
        AUTH[JWT Bearer auth<br/>+ named policies]
        GQL[HotChocolate<br/>Query / Mutation]
        MED[MediatR Pipeline<br/>+ ValidationBehavior]
        DOM[Domain methods<br/>PriorAuthorization.Submit/Approve/Deny]
        REPO[Mongo Repositories<br/>+ AuditPublisher]
        MW --> AUTH --> GQL --> MED --> DOM --> REPO
    end

    REPO -->|MongoDB.Driver| M[(MongoDB 7)]
    REPO -->|append-only| AE[(audit_events)]

    classDef store fill:#0f172a,color:#fff,stroke:#0f172a;
    class M,AE store;
```

A typical request crosses **five enforcement layers** before it can mutate
state:

| Layer | What it enforces | Where |
| --- | --- | --- |
| Middleware | Correlation id, security headers, problem-details errors | `src/SerenAuth.Api/Middleware/` |
| Rate limit | Per-IP fixed window | `Program.cs` (RateLimit__*) |
| AuthN | JWT HS256 with issuer + audience validation | `Program.cs` AddJwtBearer |
| AuthZ | Named policies — role + `org` claim | `Api/Authorization/Policies.cs` |
| Domain | State-machine transition rules + value-object validation | `Domain/Entities/PriorAuthorization.cs` |

The handler always reads `currentUser.OrganizationId` from the JWT — never
from the client payload — so a request cannot widen its tenant scope.

---

## 2. Prior authorization state machine

```mermaid
stateDiagram-v2
    [*] --> Draft: createPriorAuthorization<br/>(Intake/Clinician/Admin)
    Draft --> Draft: updatePriorAuthorization<br/>(Intake/Clinician/Admin)
    Draft --> Pending: submitPriorAuthorization<br/>(Clinician/Admin)
    Pending --> Approved: decidePriorAuthorization(APPROVE)<br/>(Admin)
    Pending --> Denied: decidePriorAuthorization(DENY)<br/>(Admin)
    Approved --> [*]
    Denied --> [*]
```

The `Draft → Draft` self-transition is intentional: edits are allowed
*only* while the PA is still a draft. Once `Submit()` flips it to
`Pending`, the entity refuses further `Update()` calls — the payer
always sees what was submitted, not a later rewrite.

Each transition is a domain method on `PriorAuthorization` — the entity is
the only thing that can change its own status, and illegal transitions
throw `InvalidOperationException` which the global exception filter
surfaces as an RFC 7807 problem.

---

## 3. End-to-end "happy path" — sign in → approve

```mermaid
sequenceDiagram
    autonumber
    actor Admin
    participant Web as Next.js (apps/web)
    participant API as API (HotChocolate)
    participant Med as MediatR + Validator
    participant Dom as PriorAuthorization
    participant Mongo as MongoDB
    participant Aud as audit_events

    Admin->>Web: open /login, submit email + password
    Web->>API: mutation login(input)
    API->>Med: LoginCommand
    Med->>Mongo: find user by email
    Med->>Med: PBKDF2 verify (constant-time)
    Med->>Aud: LOGIN
    Med-->>API: JWT (HS256, 60m, claims: sub/email/org/role)
    API-->>Web: { token, role, organizationId }
    Web->>Web: persist token in localStorage<br/>redirect /dashboard

    Web->>API: query priorAuthorizations (Bearer)
    API->>API: policy RequirePaRead
    API->>Med: GetPriorAuthorizationsQuery
    Med->>Mongo: list by orgId, filters
    Med->>Aud: VIEW_PA (one event per list call)
    API-->>Web: rows[]

    Admin->>Web: pick PENDING row → "Approve"
    Web->>API: mutation decidePriorAuthorization(APPROVE)
    API->>API: policy RequireAdmin
    API->>Med: DecidePriorAuthorizationCommand
    Med->>Mongo: get PA by (orgId, id)
    Med->>Dom: pa.Approve() — enforces Pending→Approved
    Med->>Mongo: update
    Med->>Aud: DECIDE_PA
    API-->>Web: { id, status: APPROVED }
```

---

### 3a. Edit-then-submit path

A draft is rarely correct on the first pass. Intake users frequently
need to fix a CPT code or swap a payer after the clinician reviews the
chart. The edit path mirrors the create path closely — same value-object
validation, same `RequirePaWrite` policy — but is gated by the entity's
Draft-only invariant.

```mermaid
sequenceDiagram
    autonumber
    actor Intake
    participant API as API (HotChocolate)
    participant Med as MediatR + Validator
    participant Dom as PriorAuthorization
    participant Mongo as MongoDB
    participant Aud as audit_events

    Intake->>API: mutation updatePriorAuthorization(input)
    API->>API: policy RequirePaWrite
    API->>Med: UpdatePriorAuthorizationCommand
    Med->>Med: FluentValidation<br/>(CPT/ICD10 allowlists, AI 0..1)
    Med->>Mongo: get PA by (orgId, id)
    Med->>Dom: pa.Update(cpt, icd10, payer, aiConfidence)
    Note over Dom: throws if Status != Draft<br/>(see User Story D)
    Med->>Mongo: update
    Med->>Aud: UPDATE_PA
    API-->>Intake: { id, procedureCpt, payer, status: DRAFT }
```

Notice the placement of the Draft-only check: it lives on the **entity**,
not on the policy. That's deliberate — the policy answers *who can call
the operation*, the entity answers *whether the operation is valid right
now*. Putting both checks in one layer would let either a route refactor
or a policy tweak silently re-open the edit window after submission.

## 4. Role / policy matrix

| Operation | Policy | Viewer | Intake | Clinician | Admin |
| --- | --- | :-: | :-: | :-: | :-: |
| `login` | (none, public) | ✓ | ✓ | ✓ | ✓ |
| `priorAuthorizations` query | `RequirePaRead` | ✓ | ✓ | ✓ | ✓ |
| `patients` / `providers` | `RequireOrgScope` | ✓ | ✓ | ✓ | ✓ |
| `createPriorAuthorization` | `RequirePaWrite` | — | ✓ | ✓ | ✓ |
| `updatePriorAuthorization` (Draft only) | `RequirePaWrite` | — | ✓ | ✓ | ✓ |
| `submitPriorAuthorization` | `RequirePaSubmit` | — | — | ✓ | ✓ |
| `decidePriorAuthorization` | `RequireAdmin` | — | — | — | ✓ |

All policies additionally require an `org` claim — a token without one is
treated as unauthenticated for org-scoped operations.

---

## 5. Audit trail

Every state-changing operation publishes an `AuditEvent` to an
append-only collection:

| Action | Emitted by |
| --- | --- |
| `LOGIN` | `LoginHandler` |
| `CREATE_PA` | `CreatePriorAuthorizationHandler` |
| `UPDATE_PA` | `UpdatePriorAuthorizationHandler` (Draft-only) |
| `SUBMIT_PA` | `SubmitPriorAuthorizationHandler` |
| `DECIDE_PA` | `DecidePriorAuthorizationHandler` |
| `VIEW_PA` | `GetPriorAuthorizationsHandler` (one per list call) |

`audit_events` is indexed on `(organizationId, timestamp desc)` so a
compliance review can pull a tenant's full activity in chronological
order without a collection scan.

PHI is never logged. Structured logs carry the correlation id and the
user/org id only; the body of the PA never leaves the database.

---

## 6. Where the user stories live in code

| User story | Source | Tests |
| --- | --- | --- |
| **A** — Admin approves a pending PA | `Application/PriorAuthorizations/Handlers.cs` `DecidePriorAuthorizationHandler` | `tests/SerenAuth.IntegrationTests/DecidePriorAuthorizationTests.cs::Admin_can_approve_a_pending_prior_authorization` |
| **B** — Admin denies a pending PA | same handler, `PaDecision.Deny` branch | `DecidePriorAuthorizationTests.cs::Admin_can_deny_a_pending_prior_authorization` |
| Guardrail — Clinician cannot decide | policy `RequireAdmin` on `Mutation.DecidePriorAuthorization` | `DecidePriorAuthorizationTests.cs::Clinician_cannot_decide_a_prior_authorization` |
| **C** — Intake edits a draft PA | `Application/PriorAuthorizations/Handlers.cs` `UpdatePriorAuthorizationHandler`; domain `PriorAuthorization.Update` | `tests/SerenAuth.IntegrationTests/UpdatePriorAuthorizationTests.cs::Intake_can_edit_a_draft_prior_authorization` |
| **D** — Cannot edit after submit | domain invariant `Update` throws unless `Status == Draft` | `UpdatePriorAuthorizationTests.cs::Editing_a_submitted_prior_authorization_is_rejected` |

The integration tests boot the full API host against a Testcontainers
Mongo instance, so each one exercises the same five enforcement layers
listed at the top of this document.

---

## 7. Running the demo

```bash
cp .env.example .env  # if not already present
./infrastructure/scripts/dev-up.sh    # mongo + api + web on 3000/8080
```

Sign in at <http://localhost:3000/login> with a seeded account
(`admin@riverbend.example` / `ChangeMe!123`) and walk a PA through
DRAFT → PENDING → APPROVED to exercise the full flow.
