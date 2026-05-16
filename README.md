# EngiFlow

![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=nextdotjs)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-API%2FApplication%2FDomain%2FInfrastructure-5A2D82)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture%2FDDD-0B7285)

EngiFlow is a multi-tenant B2B SaaS platform for engineering teams that need a controlled, auditable process for Engineering Change Orders (ECOs). An ECO represents a formal request to change an engineering artifact, such as a material selection, CAD specification, manufacturing tolerance, or implementation procedure.

The platform is designed around strict tenant isolation, JWT-backed role-based access control, a domain-owned approval state machine, and an immutable audit trail. The current repository contains the foundational local orchestration, domain model, application use cases, EF Core PostgreSQL persistence layer, and secured ASP.NET Core API surface.

## Project Overview

Engineering changes frequently affect cost, quality, compliance, safety, and production continuity. EngiFlow treats each ECO as a governed workflow rather than a generic task record. The domain model enforces the core lifecycle:

- ECOs are created as drafts.
- Drafts can be edited before review.
- Review is required before approval or rejection.
- Approved ECOs can be implemented.
- Rejected and implemented ECOs are terminal.
- Every material business action produces an audit event.

Multi-tenancy is a first-class architectural constraint. Company identity is modeled as the tenant boundary, and tenant-scoped entities carry a `CompanyId` so infrastructure can enforce global query filters and data isolation from the tenant claim in the authenticated JWT.

## Architecture

EngiFlow uses a monorepo with a Next.js web application and an ASP.NET Core API organized with Clean Architecture and Domain-Driven Design.

```mermaid
flowchart LR
    subgraph Local["Local Docker Compose"]
        Browser["Browser"]
        Web["web: Next.js standalone server<br/>Port 3000"]
        Api["api: ASP.NET Core Web API<br/>Port 8080"]
        Db[("postgres: PostgreSQL<br/>Port 5432<br/>Named volume")]

        Browser --> Web
        Web --> Api
        Api --> Db
    end

    subgraph FutureAws["Future AWS Topology"]
        CloudFront["CloudFront or managed edge"]
        WebRunner["AWS App Runner<br/>Web service"]
        ApiRunner["AWS App Runner<br/>API service"]
        Rds[("Amazon RDS for PostgreSQL")]
        S3[("Amazon S3<br/>attachments and exports")]

        CloudFront --> WebRunner
        WebRunner --> ApiRunner
        ApiRunner --> Rds
        ApiRunner --> S3
    end

    Web -. deployment analogue .-> WebRunner
    Api -. deployment analogue .-> ApiRunner
    Db -. managed database analogue .-> Rds
```

### Backend Structure

The API is split into four projects:

- `EngiFlow.Domain`: entities, value objects, enums, domain exceptions, and domain contracts. This layer has no external dependencies.
- `EngiFlow.Application`: CQRS use cases, DTOs, validation, application exceptions, tenant/user context contracts, persistence contracts, and handler orchestration.
- `EngiFlow.Infrastructure`: EF Core persistence, tenant query filters, audit interceptors, migrations, password hashing, storage adapters, and integration implementations.
- `EngiFlow.Api`: ASP.NET Core composition root, JWT authentication, RBAC policies, HTTP tenant resolution, controllers, Swagger, and dependency injection.

The current domain foundation is intentionally rich. The `EngineeringChangeOrder` aggregate owns its state transitions and creates `EcoEvent` audit records during business operations so callers cannot bypass the approval workflow or forget audit history. Infrastructure persists those pending audit events through a SaveChanges interceptor so application code does not need a second manual audit insert.

The Application layer exposes EngiFlow-owned CQRS primitives instead of depending on an external mediator package. Commands and queries implement `ICommand<TResponse>` or `IQuery<TResponse>`, handlers implement the matching handler contracts, and `IApplicationMediator` dispatches requests through ordered pipeline behaviors. `ValidationBehavior<TRequest, TResponse>` runs FluentValidation validators before handlers execute and throws the custom application `ValidationException` with errors grouped by request property.

Implemented ECO use cases:

- `LoginQuery`: validates email/password credentials and returns a JWT bearer token.
- `CreateEcoCommand`: creates a draft ECO for the current tenant and current user.
- `SubmitEcoCommand`: transitions a draft ECO to under review.
- `ApproveEcoCommand`: transitions an ECO under review to approved.
- `RejectEcoCommand`: transitions an ECO under review to rejected with a required reason.
- `GetEcoByIdQuery`: retrieves one ECO with sorted audit history.
- `ListEcosQuery`: retrieves a tenant-scoped paginated list of ECO summaries.

```mermaid
stateDiagram-v2
    [*] --> Draft: Create ECO
    Draft --> UnderReview: SubmitForReview
    UnderReview --> Approved: Approve
    UnderReview --> Rejected: Reject
    Approved --> Implemented: Implement
    Rejected --> [*]
    Implemented --> [*]

    note right of Draft
        Draft ECOs can be edited.
        Review has not started.
    end note

    note right of UnderReview
        Approval or rejection must be explicit.
    end note

    note right of Rejected
        Terminal state.
    end note

    note right of Implemented
        Terminal state.
    end note
```

### Frontend Structure

The web application is a Next.js App Router project using TypeScript and Material UI. The Docker image uses Next.js standalone output so the runtime image contains only the traced production server, static assets, and public files needed to serve the app.

The current frontend foundation includes:

- Material UI App Router SSR wiring through `AppRouterCacheProvider` from `@mui/material-nextjs/v16-appRouter`.
- A baseline Material Design 2 theme with a restrained B2B SaaS palette and Roboto loaded globally through `@fontsource/roboto`.
- A persistent MUI application shell with an EngiFlow app bar, authenticated user role display, logout action, and route content container.
- A responsive Material UI login page at `/login` that posts credentials through the shared API client, stores the returned JWT through `AuthContext`, and redirects authenticated users to the workspace root.
- A typed native `fetch` API client that reads `NEXT_PUBLIC_API_URL`, falls back to `NEXT_PUBLIC_API_BASE_URL`, and then falls back to `http://localhost:8080`.
- A React authentication context that decodes backend JWT claims (`sub`, `tenant`, `role`, optional `exp`), stores the bearer token in local storage, mirrors it to a non-HttpOnly cookie, and clears auth state on `401 Unauthorized`.

## Tech Stack

| Area | Technology |
| --- | --- |
| Frontend | Next.js 16, React 19, TypeScript, Material UI |
| Backend | ASP.NET Core Web API, JWT bearer authentication, .NET 10, C# |
| Domain | Clean Architecture, Domain-Driven Design, rich aggregates |
| Application | Custom CQRS mediator, FluentValidation, DTO-based use cases |
| Persistence | EF Core 10, Npgsql, PostgreSQL 18 |
| Orchestration | Docker Compose with a dedicated bridge network |
| Testing | xUnit for API, application, domain, and infrastructure tests |
| Future Infrastructure | AWS App Runner, Amazon RDS for PostgreSQL, Amazon S3, Terraform |

## Getting Started

### Prerequisites

Install the following tools:

- Docker Desktop or Docker Engine with Compose support.
- .NET SDK 10 for local API builds and tests.
- Node.js 24 if running the web app outside Docker.

### Run the Full Local Stack

From the repository root:

```bash
docker compose up --build
```

This starts:

| Service | URL or Port | Purpose |
| --- | --- | --- |
| `web` | `http://localhost:3000` | Next.js frontend |
| `api` | `http://localhost:8080` | ASP.NET Core API |
| `api` Swagger UI | `http://localhost:8080/swagger` | Interactive API documentation in Development |
| `postgres` | `localhost:5432` | Local PostgreSQL database |

PostgreSQL uses the named Docker volume `postgres-data`, so local database state survives container restarts and rebuilds.

### Frontend Configuration

The web app calls the API through the typed client in `web/lib/api/client.ts`. Configure the browser-visible API URL with:

```bash
NEXT_PUBLIC_API_URL=http://localhost:8080
```

For compatibility with the existing Docker Compose configuration, `NEXT_PUBLIC_API_BASE_URL` is still accepted as a fallback. Authenticated requests automatically include:

```text
Authorization: Bearer <accessToken>
```

When the API returns `401 Unauthorized`, the frontend clears the stored token, emits an auth-state event, and redirects browser clients to `/login`. The login page submits credentials to `POST /api/auth/login`, stores the returned `accessToken` through the authentication context, and redirects successful sign-ins to `/`.

The API reads `ConnectionStrings:DefaultConnection`. Docker Compose supplies the container connection string, while `api/src/EngiFlow.Api/appsettings.Development.json` points local `dotnet run` usage at `localhost:5432`.

### Security and Default Login

In `Development`, the API applies EF Core migrations at startup and seeds a default company plus administrator when the database has no companies. The seeded credentials are for local development only:

| Field | Value |
| --- | --- |
| Company | `EngiFlow Demo Company` |
| Email | `admin@engiflow.local` |
| Password | `EngiFlow_Admin_123!` |
| Role | `Administrator` |

Authenticate with:

```text
http://localhost:3000/login
```

Or call the API directly:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@engiflow.local",
    "password": "EngiFlow_Admin_123!"
  }'
```

The response contains `accessToken`, `tokenType`, and `expiresAtUtc`. Send the token to secured endpoints as:

```text
Authorization: Bearer <accessToken>
```

JWT settings are read from `EngiFlow:Authentication:Jwt`:

```json
{
  "EngiFlow": {
    "Authentication": {
      "Jwt": {
        "Issuer": "EngiFlow.Api",
        "Audience": "EngiFlow.Clients",
        "SigningKey": "replace-with-at-least-32-characters",
        "AccessTokenMinutes": 60
      }
    }
  }
}
```

`appsettings.Development.json` includes a development signing key. Production deployments must override `SigningKey`, `Issuer`, and `Audience` through environment-specific configuration or secret management.

### API Documentation and ECO Workflow

The API exposes Swagger UI in the `Development` environment. With Docker Compose, open:

```text
http://localhost:8080/swagger
```

When running the API directly, use the launch profile URL:

```bash
dotnet run --project api/src/EngiFlow.Api/EngiFlow.Api.csproj
```

Then open:

```text
http://localhost:5128/swagger
```

The OpenAPI document is available at `/swagger/v1/swagger.json`. Controller XML comments are included in the generated endpoint summaries, remarks, parameters, and response descriptions. Public ECO enum values are serialized as strings, such as `Medium`, `UnderReview`, and `Approved`.

Swagger UI supports JWT bearer authentication. Call `POST /api/auth/login`, copy the `accessToken`, click `Authorize`, and enter:

```text
Bearer <accessToken>
```

The current secured REST surface is:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Authenticate and issue a JWT bearer token |
| `POST` | `/api/ecos` | Create a draft ECO |
| `GET` | `/api/ecos/{id}` | Retrieve one ECO with audit history |
| `GET` | `/api/ecos?pageNumber=1&pageSize=20` | List paged ECO summaries |
| `PUT` | `/api/ecos/{id}/submit` | Submit a draft ECO for review |
| `PUT` | `/api/ecos/{id}/approve` | Approve an ECO under review |
| `PUT` | `/api/ecos/{id}/reject` | Reject an ECO under review |

Example flow:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@engiflow.local",
    "password": "EngiFlow_Admin_123!"
  }'

TOKEN="<accessToken from the login response>"

curl -X POST http://localhost:8080/api/ecos \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Use aluminum bracket",
    "description": "Update load-bearing bracket material from steel to aluminum.",
    "priority": "Medium"
  }'

curl "http://localhost:8080/api/ecos?pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"

curl http://localhost:8080/api/ecos/<eco-id> \
  -H "Authorization: Bearer $TOKEN"

curl -X PUT http://localhost:8080/api/ecos/<eco-id>/submit \
  -H "Authorization: Bearer $TOKEN"

curl -X PUT http://localhost:8080/api/ecos/<eco-id>/approve \
  -H "Authorization: Bearer $TOKEN"

curl -X PUT http://localhost:8080/api/ecos/<eco-id>/reject \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "reason": "Specification is incomplete." }'
```

ECO routes require authentication. Create and submit require `Requester` or `Administrator`; approve and reject require `Approver` or `Administrator`; reads require any authenticated role.

### API Error Handling

The API uses a global ASP.NET Core exception handler that returns RFC 7807 `ProblemDetails` responses and does not expose stack traces to clients.

| Exception or failure | Status | Response shape |
| --- | --- | --- |
| Failed login or missing authenticated tenant context | `401 Unauthorized` | `ProblemDetails` with a generic authentication detail |
| Application `ValidationException` | `400 Bad Request` | `ValidationProblemDetails` with `errors` grouped by field |
| `EntityNotFoundException` | `404 Not Found` | `ProblemDetails` with the missing resource detail |
| Domain `DomainException` | `409 Conflict` | `ProblemDetails` with the violated business rule |
| Unhandled exception | `500 Internal Server Error` | Generic `ProblemDetails`; full details are logged server-side |

Validation responses are designed for frontend form rendering:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed.",
  "status": 400,
  "detail": "One or more request values failed validation.",
  "instance": "/api/ecos",
  "errors": {
    "Title": ["Title is required."]
  },
  "traceId": "0H..."
}
```

### Database Migrations

Restore the repo-local EF tool and list migrations:

```bash
dotnet tool restore
dotnet tool run dotnet-ef -- migrations list \
  --project api/src/EngiFlow.Infrastructure/EngiFlow.Infrastructure.csproj \
  --startup-project api/src/EngiFlow.Api/EngiFlow.Api.csproj \
  --context EngiFlowDbContext
```

Generate future migrations from the repository root:

```bash
dotnet tool run dotnet-ef -- migrations add MigrationName \
  --project api/src/EngiFlow.Infrastructure/EngiFlow.Infrastructure.csproj \
  --startup-project api/src/EngiFlow.Api/EngiFlow.Api.csproj \
  --context EngiFlowDbContext \
  --output-dir Persistence/Migrations
```

### Verify the Containers

Render and validate the Compose configuration:

```bash
docker compose config
```

Build the images without starting the stack:

```bash
docker compose build
```

Check running containers after startup:

```bash
docker compose ps
```

Stop the stack:

```bash
docker compose down
```

To remove the local PostgreSQL volume as well:

```bash
docker compose down -v
```

## Testing

Run the domain test suite from the repository root:

```bash
dotnet test api/tests/EngiFlow.Domain.Tests/EngiFlow.Domain.Tests.csproj
```

The current tests cover:

- Company tenant preservation.
- User validation and active-user invariants.
- ECO creation in draft status.
- Valid approval flow from draft to implemented.
- Invalid transitions such as approving directly from draft.
- Rejected and implemented terminal states.
- Audit event creation for ECO creation, edits, and transitions.
- Application CQRS validation behavior.
- Login validation, credential verification, JWT claim issuance, and HTTP tenant claim resolution.
- ECO command handlers for create, submit, approve, and reject.
- ECO query handlers for detail retrieval and paginated lists.
- Infrastructure tenant query filters.
- Tenant-scoped write validation.
- Password hash persistence metadata and authentication lookup behavior.
- ECO audit-event persistence interception.
- Strongly typed identifier and enum conversion metadata.

Run the full API solution test suite:

```bash
dotnet test api/EngiFlow.slnx /m:1
```

Build the API solution:

```bash
dotnet build api/EngiFlow.slnx --no-restore /m:1
```

Verify the web app:

```bash
cd web
npm run lint
npm run build
```

The web build uses Next.js standalone output for the Docker runtime image. In restricted sandboxes, `next build` may need permission to run Turbopack's helper process.

## Repository Layout

```text
.
+-- api/
|   +-- Dockerfile
|   +-- EngiFlow.slnx
|   +-- src/
|   |   +-- EngiFlow.Api/
|   |   +-- EngiFlow.Application/
|   |   +-- EngiFlow.Domain/
|   |   +-- EngiFlow.Infrastructure/
|   +-- tests/
|       +-- EngiFlow.Application.Tests/
|       +-- EngiFlow.Domain.Tests/
|       +-- EngiFlow.Infrastructure.Tests/
+-- web/
|   +-- Dockerfile
|   +-- app/
|   +-- components/
|   +-- lib/
|   +-- next.config.ts
|   +-- package.json
+-- docker-compose.yml
```

## Current Scope

This foundation includes local orchestration, the core domain model, Application-layer CQRS use cases, validation, EF Core persistence, migrations, JWT authentication, role-based authorization policies, secured ECO/API controllers, Swagger bearer support, frontend MUI SSR/auth/API plumbing, and application/domain/infrastructure/API tests. It intentionally does not yet include frontend ECO workflows, file storage, production onboarding, refresh tokens, or cloud deployment automation.

Those concerns should build on the current boundaries rather than bypass them:

- Persistence enforces `ITenantScoped` filters and strongly typed identifier conversions.
- API endpoints should dispatch Application commands and queries through `IApplicationMediator`.
- Application command handlers should call aggregate methods instead of mutating status directly.
- Audit history should remain append-only.
- Tenant identity should be resolved centrally from authenticated JWT claims and applied consistently across queries and commands.
