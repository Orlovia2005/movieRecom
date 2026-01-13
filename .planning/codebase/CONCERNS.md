# Codebase Concerns

**Analysis Date:** 2026-01-13

## Security Concerns

**Hardcoded Database Credentials (CRITICAL):**
- Risk: PostgreSQL credentials exposed in source control
- Files:
  - `plt/Models/Model/EducationDbContext.cs` (line 44)
  - `plt/appsettings.json` (line 10)
  - `ml_service/.env` (line 1)
  - `docker-compose.yml` (lines 10, 32, 49)
- Credentials: Username `postgres`, Password `Ignat2005`
- Current mitigation: None
- Recommendations: Use environment variables, Azure Key Vault, or Docker Secrets

**Hardcoded JWT Secret Key:**
- Risk: All JWT tokens vulnerable to forgery
- Files:
  - `plt/Program.cs` (line 87)
  - `plt/appsettings.json` (line 13)
- Secret: `YourSuperSecretKeyForJWT_MinLength32Chars!` exposed in config
- Current mitigation: None
- Recommendations: Move to environment variables or secure vault

**No Input Validation on File Uploads:**
- Risk: MIME-type spoofing, path traversal, DoS via large files
- File: `plt/Controllers/AccountController.cs` (lines 109-121)
- Issue: Accepts any file type via `avatar.FileName`, no MIME validation, no size limit
- Current mitigation: Only file length check (> 0)
- Recommendations: Validate MIME type, limit file size, restrict extensions to image types

**Missing CSRF Protection:**
- Risk: POST actions vulnerable to CSRF attacks
- Files: MVC controllers (`plt/Controllers/AccountController.cs`)
- Issue: No visible `@Html.AntiForgeryToken()` in form submissions
- Current mitigation: None detected
- Recommendations: Add CSRF token validation to all state-changing operations

**Unvalidated Comment Text (XSS Risk):**
- Risk: XSS vulnerabilities if comments rendered without encoding
- File: `plt/Controllers/Api/MoviesApiController.cs` (line 266)
- Issue: Comment text stored as-is without sanitization
- Current mitigation: Razor auto-escaping (if used in views)
- Recommendations: Sanitize user input or ensure output is HTML-encoded

**Hardcoded Email Credentials:**
- Risk: Email account compromise
- File: `plt/appsettings.json` (lines 20-24)
- Issue: SMTP password in plaintext in config file
- Current mitigation: Service not actively used
- Recommendations: Move to environment variables or secure configuration provider

## Performance Bottlenecks

**N+1 Query Pattern in Recommendations:**
- Problem: Inefficient database queries loading related data in loops
- File: `plt/Controllers/RecommendationsController.cs` (lines 145-152)
- Code:
  ```csharp
  var preferredGenreIds = userRatings
      .Where(r => r.Score >= 4)
      .SelectMany(r => r.Movie.MovieGenres.Select(mg => mg.GenreId))
  ```
- Cause: Iterates through `r.Movie.MovieGenres` in-memory after loading all ratings
- Impact: Multiple round-trips to database for related data
- Improvement path: Use `.Include()` to eager-load MovieGenres in initial query

**Inefficient Movie Scoring Algorithm:**
- Problem: Loads all movies into memory then filters
- File: `plt/Controllers/RecommendationsController.cs` (lines 166-170)
- Issue: `.ToListAsync()` fetches all movies, then filters in memory
- Impact: Could load thousands of movie records unnecessarily
- Improvement path: Push filtering logic to database query

**Missing Database Indexes:**
- Problem: Common queries lack supporting indexes
- File: `plt/Models/Model/EducationDbContext.cs`
- Missing indexes:
  - Wishlist.UserId queries
  - Comments.UserId + Comments.MovieId composite
- Impact: Slow query performance as data grows
- Improvement path: Add indexes in migrations for frequently joined columns

**Multiple SaveChangesAsync Calls:**
- Problem: Separate database round-trips in single request
- File: `plt/Controllers/Api/AuthApiController.cs` (lines 59, 116, 223)
- Issue: Each method calls `SaveChangesAsync()` multiple times
- Impact: Increased latency and database load
- Improvement path: Batch operations and save once per request

## Tech Debt

**Large Controller Files Without Service Layer:**
- Issue: Business logic mixed with HTTP concerns
- Files:
  - `plt/Controllers/Api/AdminApiController.cs` (385 lines)
  - `plt/Controllers/Api/RecommendationsApiController.cs` (372 lines)
- Why: Rapid prototyping during initial development
- Impact: Hard to test, duplicate code patterns, difficult to reuse logic
- Fix approach: Extract service classes for admin operations and recommendations logic

**.NET Version Mismatch in Dockerfile (CRITICAL):**
- Issue: Project targets .NET 9.0 but Dockerfile uses .NET 8.0 SDK
- Files:
  - `plt/movieRecom.csproj` (TargetFramework: `net9.0`)
  - `plt/Dockerfile` (FROM `mcr.microsoft.com/dotnet/sdk:8.0`)
- Why: Dockerfile not updated after .NET upgrade
- Impact: Docker build will fail
- Fix approach: Update Dockerfile to use `dotnet/sdk:9.0` and `dotnet/aspnet:9.0`

**Python ML Service Debug Mode in Production:**
- Issue: Debug mode enabled in production code
- File: `ml_service/app.py` (line 172: `app.run(debug=True)`)
- Why: Development setting not removed
- Impact: Exposes sensitive information in error pages
- Fix approach: Set `debug=False` or use `debug=os.getenv('FLASK_DEBUG') == '1'`

**Hard-Coded ML Service Fallback:**
- Issue: No user indication of recommendation source
- File: `plt/Controllers/RecommendationsController.cs` (lines 74-116)
- Why: Quick implementation without UX consideration
- Impact: Users don't know if recommendations are ML-based or content-based
- Fix approach: Add explanation field in response or configuration setting

**Audit Logging Logs Sensitive Data:**
- Issue: Logs all field changes including passwords and emails
- File: `plt/Models/Model/EducationDbContext.cs` (lines 336-341)
- Why: Generic audit logging without exclusions
- Impact: Plaintext passwords logged in application logs
- Fix approach: Exclude sensitive properties from audit logging

## Error Handling Gaps

**No Try-Catch in File Operations:**
- Risk: Unhandled disk errors crash requests
- File: `plt/Controllers/AccountController.cs` (lines 109-121)
- Issue: File I/O (`Directory.CreateDirectory`, `FileStream`, `CopyToAsync`) without error handling
- Impact: No user-friendly error message on disk failure
- Fix approach: Wrap in try-catch and return proper error response

**Silent Failure in ML Service Calls:**
- Risk: Users see no recommendations without explanation
- File: `plt/Services/MlRecommendationService.cs` (lines 38-45, 86-93)
- Issue: Returns `null` when ML service fails with no user notification
- Impact: Poor user experience when service is down
- Fix approach: Log warnings, return user-friendly error messages, implement circuit breaker

**Missing Null Checks in Database Queries:**
- Risk: NullReferenceException when referenced entities deleted
- File: `plt/Controllers/Api/MoviesApiController.cs` (line 157)
- Issue: `.User` in comment query could be null (cascade delete behavior)
- Impact: Potential crashes when building DTOs
- Fix approach: Add null checks or use left joins with null-coalescing

**No Global Exception Handling:**
- Risk: Unhandled exceptions result in generic error pages
- File: `plt/Program.cs` (lines 228-231)
- Issue: Try-finally block only, no middleware to catch runtime exceptions
- Impact: Poor error reporting and logging
- Fix approach: Add `app.UseExceptionHandler()` middleware

## Missing Critical Features

**No API Rate Limiting:**
- Problem: No protection against API abuse
- Files: All API controllers
- Current workaround: None
- Blocks: Can't prevent DoS attacks, users can spam endpoints
- Implementation complexity: Medium (add AspNetCoreRateLimit middleware)

**No Connection Pooling Configuration:**
- Problem: Database connections not optimized
- File: `ml_service/database.py` (lines 108-111)
- Current workaround: Manual connection close (prone to leaks)
- Blocks: Efficient resource usage, connection leaks on exceptions
- Implementation complexity: Low (use context manager: `with get_db_connection() as conn:`)

**No Soft Delete Implementation:**
- Problem: Deletion is permanent, no recovery option
- Files: User, Movie models
- Current workaround: None
- Blocks: Recovery of accidentally deleted accounts/data
- Implementation complexity: Medium (add `IsDeleted` field, filter all queries)

## Test Coverage Gaps

**No Controller Tests:**
- What's not tested: AccountController, MoviesController, RecommendationsController
- Risk: No validation that MVC endpoints work correctly
- Priority: High
- Difficulty to test: Low (mock DbContext and services)

**No Input Validation Tests:**
- What's not tested: File upload validation, rating score range, email format
- Risk: Invalid input could break application
- Priority: Medium
- Difficulty to test: Low (add [Theory] tests with invalid inputs)

**No Python ML Service Tests:**
- What's not tested: ML service endpoints, recommendation algorithms
- Risk: ML service could break silently
- Priority: High
- Difficulty to test: Medium (need test fixtures and mock database)

## Dependencies at Risk

**Outdated Python Dependencies:**
- Risk: Security vulnerabilities in old versions
- File: `ml_service/requirements.txt`
- Versions:
  - pandas 2.1.4 (from early 2024, likely has updates)
  - scikit-learn 1.3.2 (from early 2024, likely has updates)
  - numpy 1.26.2 (from late 2023, likely has security updates)
- Impact: Potential security vulnerabilities
- Migration plan: Run `pip install --upgrade`, test thoroughly, pin new versions

**Mismatched .env.example and .env:**
- Risk: Developers might commit real credentials
- Files: `ml_service/.env.example` and `ml_service/.env`
- Issue: Both contain hardcoded credentials instead of placeholders
- Impact: Accidental credential exposure
- Fix approach: Update .env.example with placeholders like `postgresql://postgres:PASSWORD@localhost:5432/movieRecom`

## Fragile Areas

**ML Service Health Check Loose:**
- File: `docker-compose.yml`, `ml_service/Dockerfile` (line 30)
- Why fragile: Health check only verifies `/health` endpoint responds, not that model is loaded
- Common failures: Service reports healthy but model not trained
- Safe modification: Add model readiness check to health endpoint
- Test coverage: No health check tests

**Magic Numbers in Business Logic:**
- Files: `plt/Controllers/RecommendationsController.cs` (lines 83, 102, 150, 157, 160, 162, 181, 186, 192)
- Why fragile: Constants like `15`, `5`, `20` hardcoded in algorithm
- Common failures: Difficult to adjust algorithm without code changes
- Safe modification: Define as configuration constants or database settings
- Impact: Inflexible recommendation tuning

## Scaling Limits

**No Caching Layer:**
- Current capacity: All queries hit database directly
- Limit: Database becomes bottleneck under high load
- Symptoms at limit: Slow response times, database connection exhaustion
- Scaling path: Add Redis caching for recommendations, movie details

**Single ML Service Instance:**
- Current capacity: One Flask worker handling all ML requests
- Limit: ~50 concurrent recommendation requests estimated
- Symptoms at limit: Timeouts, request queueing
- Scaling path: Scale horizontally with load balancer, increase Gunicorn workers

---

*Concerns audit: 2026-01-13*
*Update as issues are fixed or new ones discovered*
