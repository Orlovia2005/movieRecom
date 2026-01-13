# Architecture

**Analysis Date:** 2026-01-13

## Pattern Overview

**Overall:** Two-Service Microservices with Hybrid Architecture

**Key Characteristics:**
- Monolithic MVC application (primary service) - ASP.NET Core 9.0
- Specialized ML microservice (supporting service) - Python Flask
- Service-Oriented Architecture (SOA) pattern with distinct separation of concerns
- Shared PostgreSQL database between services

## Layers

**Presentation Layer** (`plt/Controllers/`, `plt/Views/`):
- Purpose: HTTP request handling and response rendering
- Contains: MVC Controllers, API Controllers, Razor views
- MVC Controllers: AccountController, MoviesController, RecommendationsController, WishlistController, AdminController, HistoryController
- API Controllers: `plt/Controllers/Api/` (AuthApiController, MoviesApiController, RecommendationsApiController, AdminApiController, WishlistApiController, HistoryApiController)
- Depends on: Services layer, Models, EducationDbContext
- Used by: Browser clients (MVC), API clients (REST JSON)

**Service Layer** (`plt/Services/`):
- Purpose: Business logic and external integrations
- Contains: MlRecommendationService, JwtService, CurrentUserService
- Key services:
  - `MlRecommendationService` - Orchestrates calls to Python ML microservice
  - `JwtService` - Token generation and validation
  - `CurrentUserService` - Context-aware user resolution
- Depends on: HttpClient, EducationDbContext, Logging
- Used by: Controllers

**Data Access Layer** (`plt/Models/Model/`):
- Purpose: Database entity definitions and EF Core configuration
- Contains: Entity models, DbContext with audit logging
- Key file: `EducationDbContext.cs` - 8 DbSet entities (Users, Movies, Ratings, Wishlists, Comments, Genres, RefreshTokens, HiddenRecommendations)
- Depends on: Entity Framework Core, Npgsql
- Features: Audit logging via SaveChangesAsync override, fluent API relationships

**Transfer/ViewModel Layer** (`plt/Models/DTO/`, `plt/Models/ViewModel/`):
- Purpose: Data serialization contracts
- Contains: DTOs for APIs, ViewModels for server-rendered views
- DTOs: AuthDto, MovieDto, RatingDto, RecommendationDto, MlServiceDto, ApiResponse
- ViewModels: BaseViewModel, RecommendationsViewModel, MovieDetailsViewModel, MovieCatalogViewModel
- Depends on: Domain models
- Used by: Controllers for data mapping

**Infrastructure Layer** (`plt/Program.cs`, `plt/appsettings.json`):
- Purpose: Application startup, DI configuration, middleware
- Contains: Service registration, authentication setup, health checks, logging
- Key file: `plt/Program.cs` - Configures DbContext, authentication (Cookie + JWT), HttpClient factory, Serilog

**External ML Microservice** (`ml_service/`):
- Purpose: Hybrid recommendation engine
- Contains: Flask API, RecommenderModel (SVD + TF-IDF)
- Endpoints:
  - `/health` - Service health status
  - `GET /recommendations/<user_id>?n=10` - Collaborative filtering
  - `GET /similar/<movie_id>?n=10` - Content-based similarity
  - `POST /train` - Model training trigger
- Key files: `app.py` (Flask entry), `recommender.py` (ML algorithms), `database.py` (PostgreSQL queries)

## Data Flow

**User Views Personalized Recommendations:**

1. User navigates to `/Recommendations/Index` (MVC) or `GET /api/recommendations` (API)
2. RecommendationsController.GetRecommendations() authenticates user via Claims
3. Calls IMlRecommendationService.GetRecommendationsAsync(userId)
4. MlRecommendationService makes HTTP GET to Flask ML service: `http://localhost:5001/recommendations/{userId}?n=10`
5. ML Service (Flask `app.py`):
   - RecommenderModel.get_recommendations(user_id)
   - Uses trained SVD model to predict ratings
   - Filters movies user already rated
   - Returns top-N [MovieId, PredictedRating, Explanation]
6. Back in MVC Controller:
   - If ML returns results → Use them
   - If ML fails (timeout/error) → Fallback to local algorithm
7. Local Fallback Algorithm (in RecommendationsController):
   - Query user's rated movies (score >= 4)
   - Analyze preferred genres
   - Score unrated movies by genre match + IMDB rating + release year
8. Fetch Movie details from database (include genres, posters, IMDB data)
9. Filter out hidden recommendations (HiddenRecommendations table)
10. Return to View:
    - MVC: Render Razor template with RecommendationsViewModel
    - API: JSON response with RecommendationsResponseDto

**User Rates a Movie:**

1. User submits rating via form or API POST
2. RecommendationsController.Rate(movieId, score) validates score (1-5)
3. Create/Update Rating entity in database (unique constraint: UserId, MovieId)
4. Save to Ratings table via EducationDbContext (audit logging triggered)
5. (Optional) POST /train to ML service to retrain model

**Authentication Flow (JWT + Cookie Hybrid):**

1. User Registration: POST /api/auth/register with RegisterDto
2. AuthApiController validates and creates User (BCrypt password hash)
3. JWT Token Generation (JwtService):
   - Create signed JWT with claims (Id, Email, Role)
   - Sign with HMAC-SHA256 using secret key
4. Return tokens: AccessToken (60 min expiry), RefreshToken (7 days, stored in table)
5. Client stores tokens:
   - API calls: Authorization: Bearer {AccessToken}
   - MVC calls: Cookie (CookieAuthenticationDefaults)
6. On each request, middleware validates signature, expiration, issuer/audience

**State Management:**
- Database-backed - All state in PostgreSQL (users, movies, ratings, wishlists, comments)
- Stateless request handling - No in-memory session state
- ML model state - Pickled model persisted to `ml_service/models/recommender.pkl`

## Key Abstractions

**Service Abstraction:**
- Purpose: Interface-based dependency injection
- Examples: IMlRecommendationService, IJwtService, ICurrentUserService
- Pattern: Constructor injection with scoped lifetime

**Base Classes:**
- Purpose: Common controller functionality
- Example: BaseController provides GetCurrentUserAsync(), CurrentUserId property, Notif_* methods
- Pattern: Inheritance-based code reuse

**API Response Envelope:**
- Purpose: Standardized API responses
- Implementation: ApiResponse<T> with Success(data) / Fail(error) factory methods
- Pattern: Result type pattern

**Repository Pattern (Implicit):**
- Purpose: Data access abstraction
- Implementation: EducationDbContext acts as data gateway, controllers query DbSet<T> directly
- Pattern: Simplified repository (no explicit repository interfaces)

**Audit Logging:**
- Purpose: Track all data modifications
- Implementation: Custom audit trail in DbContext.SaveChanges override
- Logs: TableName, Action (Add/Modify/Delete), UserId, IP, NewValues

## Entry Points

**Web Application:**
- Location: `plt/Program.cs` - WebApplication.Run()
- Triggers: HTTP requests on port 5226 (dev) or 8080 (Docker)
- Responsibilities: Initialize DbContext, DI container, middleware pipeline, authentication, logging, Swagger

**ML Service:**
- Location: `ml_service/app.py` - Flask app.run()
- Triggers: HTTP requests on port 5001
- Responsibilities: Load model from pickle, handle recommendation requests

**Docker Orchestration:**
- Location: `docker-compose.yml`
- Triggers: `docker-compose up` command
- Responsibilities: Bring up PostgreSQL, ASP.NET web app, Flask ML service with networking

**Tests:**
- Location: `tests/movieRecom.Tests/movieRecom.Tests.csproj`
- Triggers: `dotnet test` command
- Responsibilities: Unit tests in `/Unit/Services/`, Integration tests in `/Integration/`

## Error Handling

**Strategy:** Exception middleware at top level, try-catch at controller boundaries

**Patterns:**
- Services throw exceptions with descriptive messages
- Controllers catch and return ApiResponse.Fail() or redirect to error page
- ML service failures handled gracefully with local fallback algorithm
- Unhandled exceptions redirect to `/Home/Error` (production)
- Missing authentication redirects to `/Account/Login`

## Cross-Cutting Concerns

**Logging:**
- Serilog structured logging
- Output: Console + rolling file (`logs/app-{date}.log`)
- Request logging middleware enriches with UserId, IP, UserAgent
- Audit logging in SaveChanges for all data modifications

**Authentication & Authorization:**
- Dual scheme: CookieAuthentication (MVC) + JwtBearer (API)
- Default challenge route: `/Account/Login`
- Sliding expiration: 2 hours (cookie)
- Claims-based authorization via `[Authorize]` attributes

**Validation:**
- DataAnnotations on DTOs (e.g., `[Range(1, 5)]` on rating scores)
- Model validation in controllers (ModelState.IsValid)
- Manual validation for complex business rules

**Database Constraints:**
- Unique indices on: Users.Email, Genres.Name, Movies.ImdbId
- Composite unique indices on: (UserId, MovieId) for Ratings, Wishlists, HiddenRecommendations
- Cascading deletes on foreign keys
- Timestamp defaults: CURRENT_TIMESTAMP

---

*Architecture analysis: 2026-01-13*
*Update when major patterns change*
