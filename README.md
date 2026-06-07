# Replog Auth API

Authentication Lambda for the Replog workout tracker. Validates Google ID tokens and issues HS256 JWTs delivered as HttpOnly cookies.

> Work in progress

## Related Repositories

- **[replog-api](https://github.com/ArturDias98/replog-api)** — Sync Lambda (push/pull)
- **[replog](https://github.com/ArturDias98/replog)** — Web client

## Tech Stack

- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- AWS DynamoDB
- AWS Secrets Manager
- Google.Apis.Auth (Google ID token validation)
- FluentValidation

## Project Structure

```
replog-api-auth/       # Auth Lambda host (login/refresh/logout/health)
replog-api-auth-core/  # Shared JWT primitives (JwtSettings, AccessTokenValidator)
replog-api-authorizer/ # API Gateway REQUEST authorizer Lambda
replog-api-auth.tests/ # Integration + unit tests
replog-tests-shared/   # Testcontainers DynamoDB fixture
```

## Build & Run

```bash
# Build
dotnet build replog-api-auth

# Run the auth host (port 5140)
dotnet run --project replog-api-auth
```

In development, `Jwt:Secret` and `Google:ClientId` are read from `appsettings.Development.json` or user secrets. In production, they are loaded from AWS Secrets Manager via `JWT_SECRET_ARN` and `GOOGLE_CLIENT_ID_ARN` environment variables.

## API Endpoints

| Method | Route               | Description                      |
|--------|---------------------|----------------------------------|
| POST   | `/api/auth/login`   | Exchange Google ID token for JWT |
| POST   | `/api/auth/refresh` | Rotate access + refresh tokens   |
| POST   | `/api/auth/logout`  | Clear auth cookies               |
| GET    | `/api/auth/health`  | Health probe                     |

Tokens are set as `HttpOnly`, `Secure`, `SameSite=None` cookies — never in the response body. See [`docs/authentication.md`](docs/authentication.md) for the full auth flow, token details, and client integration guide.

## Testing

```bash
dotnet test replog-api-auth.tests
```

Integration tests use WebApplicationFactory + Testcontainers (local DynamoDB). Unit tests use NSubstitute.
