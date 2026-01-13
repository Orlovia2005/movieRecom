# External Integrations

**Analysis Date:** 2026-01-13

## APIs & External Services

**Payment Processing:**
- Not integrated

**Email/SMS:**
- Gmail SMTP - Configured but not actively used
  - SDK/Client: MailKit 4.3.0
  - Auth: SMTP credentials in `plt/appsettings.json` (smtp.gmail.com:587)
  - Status: Configuration present but email sending not implemented

**External APIs:**
- None - No third-party API integrations detected

## Data Storage

**Databases:**
- PostgreSQL 15 - Primary data store for both services
  - Connection: via DATABASE_URL environment variable
  - C# Client: Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
  - Python Client: psycopg2-binary 2.9.9
  - Migrations: EF Core migrations in `plt/Migrations/`
  - Connection string: `plt/appsettings.json` (DefaultConnection)

**File Storage:**
- Local file system - Avatar images stored in `plt/wwwroot/Images/`
  - No cloud storage (S3, Azure Blob) used
  - Files managed via System.IO in C#

**Caching:**
- None - No Redis, Memcached, or in-memory caching layer

## Authentication & Identity

**Auth Provider:**
- Custom JWT implementation - `plt/Services/JwtService.cs`
  - Implementation: JWT token generation with claims
  - Token storage: RefreshTokens table in database
  - Session management: Cookie authentication (2-hour sliding expiration) + JWT for API
  - Access token expiry: 60 minutes
  - Refresh token expiry: 7 days

**OAuth Integrations:**
- None - No Google, Facebook, or other OAuth providers

## Monitoring & Observability

**Error Tracking:**
- Serilog - Structured logging to files and console
  - DSN: Log files in `plt/logs/app-{date}.log`
  - Configuration: `plt/Program.cs` (Serilog setup)

**Analytics:**
- None - No Mixpanel, Google Analytics, or similar

**Logs:**
- File-based logging - Serilog.Sinks.File 6.0.0
  - Retention: Not configured (manual cleanup)

## CI/CD & Deployment

**Hosting:**
- Docker Compose - Local and production deployment
  - Deployment: Manual via `docker-compose up`
  - Services: db (PostgreSQL), web (ASP.NET), ml (Flask)

**CI Pipeline:**
- None - No GitHub Actions, GitLab CI, or similar automated pipelines

## Environment Configuration

**Development:**
- Required env vars: DefaultConnection (database), JwtSettings (secret key)
- Secrets location: `plt/appsettings.json`, `ml_service/.env`
- Mock/stub services: All services run locally via Docker Compose

**Staging:**
- Not configured - No separate staging environment

**Production:**
- Secrets management: Environment variables in docker-compose.yml
- Database: PostgreSQL container with persistent volume

## Webhooks & Callbacks

**Incoming:**
- None - No webhook endpoints detected

**Outgoing:**
- None - No external webhook calls

## Inter-Service Communication

**ML Service Integration:**
- HTTP REST API - ASP.NET web app calls Python Flask service
  - Base URL: `http://localhost:5001` (dev) or `http://ml:5001` (Docker)
  - Client: HttpClientFactory in `plt/Services/MlRecommendationService.cs`
  - Endpoints:
    - `GET /recommendations/<user_id>?n=10` - Personalized recommendations
    - `GET /similar/<movie_id>?n=10` - Content-based similar movies
    - `POST /train` - Trigger model retraining
    - `GET /health` - Service health check
  - Timeout: 30 seconds
  - Error handling: Returns null on failure, falls back to local algorithm

**Database Sharing:**
- Both C# and Python services connect to same PostgreSQL database
  - C# via Npgsql + Entity Framework Core
  - Python via psycopg2 for raw SQL queries
  - No connection pooling configuration visible

---

*Integration audit: 2026-01-13*
*Update when adding/removing external services*
