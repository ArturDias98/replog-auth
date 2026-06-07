# Replog Auth API

Authentication Lambda API built with .NET 10 — issues and rotates JWTs after validating Google ID tokens.

> **Standalone deployment.** This repository is the standalone auth service. The sync API lives in a separate repository (`replog-api`). Both Lambdas sit behind the same API Gateway HTTP API (v2): this repo owns `/api/auth/*` and the sync repo owns `/api/sync/*`.

## Project Documentation

- `docs/authentication.md` — auth flow, token details, error codes, client integration guide
- For the sync API spec and full deployment picture, see `docs/backend-api.md` in the `replog-api` repository

## Solution Structure

There is no `.sln` file. Each project has its own `.csproj`.

```text
replog-api-auth/                         # Auth Lambda host — login/refresh/logout/health
│   ├── Auth/                            # AuthService, IAuthService, AuthTokens
│   │                                    # TokenService, ITokenService
│   │                                    # GoogleTokenValidator, IGoogleTokenValidator
│   │                                    # SecretsLoader
│   ├── Common/                          # Result<T> pattern
│   ├── Endpoints/                       # AuthEndpoints (/api/auth/*)
│   ├── Entities/                        # UserEntity, RefreshTokenEntry
│   ├── Extensions/                      # CorsExtensions, HealthEndpointExtensions
│   ├── Interfaces/                      # IUserRepository
│   ├── Json/                            # JsonDefaults (System.Text.Json options)
│   ├── Middleware/                      # GlobalExceptionHandler
│   ├── Models/
│   │   ├── Requests/                    # LoginRequest
│   │   └── Responses/                   # AuthResponse, ErrorResponse
│   ├── Repositories/                    # UserRepository (DynamoDB)
│   ├── Settings/                        # GoogleAuthSettings, DynamoDbSettings
│   ├── Validators/                      # LoginRequestValidator (FluentValidation)
│   ├── DependencyInjection.cs
│   └── Program.cs
│
├── replog-api-auth-core/                # Shared auth primitives (no ASP.NET / no infra deps)
│   ├── JwtSettings.cs                   # JWT config POCO
│   └── AccessTokenValidator.cs          # HS256 validation — used by auth host and authorizer
│
├── replog-api-authorizer/               # API Gateway REQUEST authorizer Lambda
│   └── Function.cs                      # Validates access_token cookie → returns userId context
│
├── replog-api-auth.tests/               # Auth host integration tests + unit tests (xUnit)
│   ├── Endpoints/                       # AuthEndpointTests, HealthEndpointTests
│   ├── Fixtures/                        # AuthApiWebApplicationFactory, AuthApiCollection
│   └── Handlers/                        # LoginServiceTests, RefreshTokenServiceTests
│
└── replog-tests-shared/                 # Shared test utilities
    ├── Comparers/                       # DictionaryCompareHelper
    └── Fixtures/                        # DynamoDbFixture (Testcontainers setup)
```

> **Other projects** (`replog-api-host/`, `replog-infrastructure/`, `replog-domain/`, `replog-shared/`) exist in the repo but are **not referenced** by any active code. The auth host is fully self-contained inside `replog-api-auth/`.

### Project Responsibilities

- **replog-api-auth**: Auth Lambda host. Hosts `/api/auth/*` and `/api/auth/health`. Issues and rotates JWTs after validating Google ID tokens. Owns all auth logic: `AuthService`, `TokenService`, `GoogleTokenValidator`, `UserRepository`, `SecretsLoader` (loads JWT + Google secrets at cold start). References only `replog-api-auth-core`.
- **replog-api-auth-core**: Lean shared auth primitives — `JwtSettings` + `AccessTokenValidator` (HS256 validation). One package (`System.IdentityModel.Tokens.Jwt`), no ASP.NET/infra deps. Referenced by the auth host and the authorizer.
- **replog-api-authorizer**: API Gateway HTTP API REQUEST authorizer Lambda (plain class library, not ASP.NET). Validates the `access_token` cookie with the shared `AccessTokenValidator` and returns `userId` as authorizer context for `/api/sync/*`. References `replog-api-auth-core` only.
- **replog-api-auth.tests**: HTTP-level integration tests for the auth host (`WebApplicationFactory` + Testcontainers) plus unit tests for `AuthService`.
- **replog-tests-shared**: Shared test fixtures — `DynamoDbFixture` (Testcontainers local DynamoDB setup).

### Dependency Flow

```text
replog-api-auth-core  (no project deps; System.IdentityModel.Tokens.Jwt only)
replog-api-auth       → replog-api-auth-core
replog-api-authorizer → replog-api-auth-core
replog-api-auth.tests → replog-api-auth, replog-tests-shared
```

No reverse dependencies. Auth host must never reference infrastructure from other repos.

## Tech Stack

- .NET 10 / C# 14
- ASP.NET Core Web API (minimal APIs)
- Amazon DynamoDB (via AWSSDK.DynamoDBv2) — user table (`replog-users`)
- Amazon Secrets Manager — JWT signing secret and Google client ID at cold start (production only)
- Google.Apis.Auth — Google ID token validation
- FluentValidation for input validation
- HS256 JWT (System.IdentityModel.Tokens.Jwt)
- xUnit + NSubstitute + Testcontainers for testing

## Build & Run

```bash
# Build the auth host
dotnet build replog-api-auth

# Run the auth API host (listens on http://localhost:5140 in Development)
dotnet run --project replog-api-auth

# Build the authorizer Lambda
dotnet build replog-api-authorizer
```

In development the auth host reads `Jwt:Secret` and `Google:ClientId` from `appsettings.Development.json` or user secrets. In production, `SecretsLoader` reads them from Secrets Manager via `JWT_SECRET_ARN` and `GOOGLE_CLIENT_ID_ARN` environment variables.

## Testing

**RULE: After every code modification, run `dotnet test replog-api-auth.tests` to verify nothing is broken.**

```bash
# Run all tests
dotnet test replog-api-auth.tests

# Verbose output
dotnet test replog-api-auth.tests --logger "console;verbosity=normal"
```

- Integration tests use **WebApplicationFactory** + **Testcontainers** (local DynamoDB).
- Unit tests use **NSubstitute** mocks — `LoginServiceTests`, `RefreshTokenServiceTests`.
- Tests follow the pattern: `MethodName_ShouldExpectedBehavior_WhenCondition`.

## Documentation

**RULE: After any change to endpoints, request/response models, error codes, auth behavior, or security config, update `docs/authentication.md` in this repository.**

## Security

- **Token issuance**: Custom JWT (HS256) issued after validating a Google ID token. Access tokens expire in 15 minutes (configurable via `Jwt:AccessTokenExpirationMinutes`). Refresh tokens are 30-day random Base64 strings stored as SHA-256 hashes in DynamoDB.
- **Token rotation**: Every `/api/auth/refresh` call issues a new refresh token and invalidates the previous one.
- **Cookie delivery**: Tokens are set as `HttpOnly`, `Secure`, `SameSite=None` cookies — never in the response body. JavaScript cannot access them.
- **Authorizer separation**: Token *validation* for `/api/sync/*` happens in the API Gateway Lambda authorizer (`replog-api-authorizer`), not here. This Lambda only issues and rotates tokens.
- **Secrets**: JWT signing secret is loaded from Secrets Manager at cold start in production. Never hardcoded.
- **CORS**: Restricted to `localhost:4200` (dev) and `replog.adrvcode.com` / `localhost` (prod). Only `GET` and `POST` methods allowed.

## Conventions

- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- Root namespace: `replog_api_auth` (auth host)
- `Result<T>` pattern in `Common/Result.cs` for explicit failure paths in `AuthService`
- Interface in `Interfaces/`, implementation in `Repositories/`
- All auth business logic in `Auth/` — no auth code in endpoints
- Entities in `Entities/`, request/response DTOs in `Models/`
- One FluentValidation validator per request type in `Validators/`

## Key Files

| Purpose | Path |
| --- | --- |
| Auth host entry point | `replog-api-auth/Program.cs` |
| Auth endpoints | `replog-api-auth/Endpoints/AuthEndpoints.cs` |
| Auth service (login/refresh) | `replog-api-auth/Auth/AuthService.cs` |
| Token generation/validation | `replog-api-auth/Auth/TokenService.cs` |
| Google ID token validation | `replog-api-auth/Auth/GoogleTokenValidator.cs` |
| Secrets Manager loader | `replog-api-auth/Auth/SecretsLoader.cs` |
| User DynamoDB repository | `replog-api-auth/Repositories/UserRepository.cs` |
| DI registration | `replog-api-auth/DependencyInjection.cs` |
| CORS setup | `replog-api-auth/Extensions/CorsExtensions.cs` |
| Exception middleware | `replog-api-auth/Middleware/GlobalExceptionHandler.cs` |
| Result pattern | `replog-api-auth/Common/Result.cs` |
| User entity | `replog-api-auth/Entities/UserEntity.cs` |
| JWT settings | `replog-api-auth-core/JwtSettings.cs` |
| Shared access-token validator | `replog-api-auth-core/AccessTokenValidator.cs` |
| Gateway authorizer Lambda | `replog-api-authorizer/Function.cs` |
| Auth API test factory | `replog-api-auth.tests/Fixtures/AuthApiWebApplicationFactory.cs` |
| Test DynamoDB fixture | `replog-tests-shared/Fixtures/DynamoDbFixture.cs` |
