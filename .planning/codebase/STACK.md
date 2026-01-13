# Technology Stack

**Analysis Date:** 2026-01-13

## Languages

**Primary:**
- C# / .NET 9.0 - All web application code (`plt/movieRecom.csproj`)
- Python 3.11 - ML recommendation service (`ml_service/Dockerfile`)

**Secondary:**
- HTML/Razor - Server-rendered views (`plt/Views/`)
- SQL - PostgreSQL database queries

## Runtime

**Environment:**
- .NET 9.0 Runtime - ASP.NET Core MVC web application
- Python 3.11-slim - ML microservice container
- PostgreSQL 15 - Database server

**Package Manager:**
- NuGet - .NET dependency management
- pip - Python package management (`ml_service/requirements.txt`)
- No Node.js/npm used

## Frameworks

**Core:**
- ASP.NET Core MVC 9.0 - Web framework with server-rendered Razor views
- Flask 3.0.0 - Python web framework for ML service REST API
- Entity Framework Core 9.0.9 - ORM for database access

**Testing:**
- xUnit 2.9.2 - Unit testing framework
- FluentAssertions 6.12.2 - Assertion library
- Moq 4.20.72 - Mocking framework

**Build/Dev:**
- dotnet CLI - Build and run commands
- Gunicorn 21.2.0 - Python WSGI server for production

## Key Dependencies

**Critical:**
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4 - PostgreSQL provider for EF Core
- scikit-surprise 1.1.3 - SVD collaborative filtering for recommendations
- scikit-learn 1.3.2 - TF-IDF vectorization and cosine similarity
- pandas 2.1.4 - Data manipulation in ML service
- Serilog 4.3.0 + Serilog.AspNetCore 8.0.3 - Structured logging

**Infrastructure:**
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0 - JWT token authentication
- Swashbuckle.AspNetCore 6.5.0 - Swagger/OpenAPI documentation
- AspNetCore.HealthChecks.NpgSql 8.0.2 - Database health monitoring
- psycopg2-binary 2.9.9 - Python PostgreSQL adapter
- Flask-CORS 4.0.0 - Cross-origin request handling

## Configuration

**Environment:**
- appsettings.json - Database connection strings, JWT secrets, ML service URL
- .env files - ML service configuration (DATABASE_URL, MODEL_PATH)
- Docker environment variables - Container configuration

**Build:**
- plt/movieRecom.csproj - NuGet package references
- ml_service/requirements.txt - Python dependencies with pinned versions
- docker-compose.yml - Multi-service orchestration

## Platform Requirements

**Development:**
- .NET 9.0 SDK required for building web application
- Python 3.11+ for ML service development
- PostgreSQL 15 client tools
- Docker and Docker Compose for full stack deployment

**Production:**
- Docker containers - Both services containerized
- PostgreSQL 15 - Shared database server
- Multi-stage Docker builds for optimized images
- Health checks configured for all services

---

*Stack analysis: 2026-01-13*
*Update after major dependency changes*
