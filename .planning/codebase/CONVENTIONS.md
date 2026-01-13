# Coding Conventions

**Analysis Date:** 2026-01-13

## Naming Patterns

**Files:**
- C#: PascalCase for all files (AccountController.cs, MlRecommendationService.cs, UserDto.cs)
- Python: snake_case for modules (app.py, recommender.py, database.py)
- Test files: {ClassName}Tests.cs (JwtServiceTests.cs, MlRecommendationServiceTests.cs)
- Razor views: Match action name (Index.cshtml, Details.cshtml, Login.cshtml)

**Functions:**
- C#: PascalCase for all methods (GenerateAccessToken, GetRecommendationsAsync)
- Python: snake_case for functions (get_recommendations, get_similar_movies, is_trained)
- Async: Suffix with `Async` (GetRecommendationsAsync, SaveChangesAsync)

**Variables:**
- C#: camelCase for local variables and parameters (userId, movieId, connectionString)
- C#: PascalCase for properties (UserId, MovieId, CreatedAt)
- C#: Private fields with underscore prefix (_context, _jwtService, _httpClientFactory, _logger)
- Python: snake_case for all variables (user_id, movie_id, ratings_df)
- Constants: UPPER_SNAKE_CASE (implied in Python), PascalCase in C#

**Types:**
- Classes: PascalCase (User, Movie, RecommenderModel, JwtService)
- Interfaces: Prefix with `I` (IJwtService, IMlRecommendationService, ICurrentUserService)
- DTOs: Suffix with `Dto` (UserDto, MovieDto, AuthDto, RatingDto)
- ViewModels: Suffix with `ViewModel` (RecommendationsViewModel, MovieCatalogViewModel)
- Enums: PascalCase singular (UserRole with values: User, Admin)

## Code Style

**Formatting:**
- Indentation: 4 spaces (both C# and Python)
- Brace style: C# opening brace on same line
- Quotes: C# uses double quotes `"`, Python has no enforced style
- Semicolons: Required in C# (end of all statements)

**Linting:**
- No explicit linting configuration files found
- C# defaults: .NET 9.0 conventions with `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`
- Python: Implicit PEP 8 compliance

## Import Organization

**C# Namespaces:**
- Order (implicit):
  1. System namespaces
  2. Microsoft namespaces
  3. Third-party packages
  4. Project namespaces (movieRecom.*)
- Grouping: No blank lines between groups (standard C# style)
- Example from `plt/Program.cs`:
  ```csharp
  using Microsoft.AspNetCore.Authentication.Cookies;
  using Microsoft.AspNetCore.Authentication.JwtBearer;
  using Microsoft.EntityFrameworkCore;
  using movieRecom.Models.Model;
  using movieRecom.Services;
  using Serilog;
  ```

**Python Imports:**
- Order:
  1. Standard library (os, sys)
  2. Third-party packages (flask, pandas, numpy, psycopg2)
  3. Local modules (./database, ./recommender)
- Type hints: Used in function signatures (`def func(user_id: int) -> List[Dict[str, Any]]:`)

## Error Handling

**Patterns:**
- C#: Throw exceptions, catch at controller boundaries
- C#: Return `ApiResponse.Fail(error)` for API errors
- Python: Raise exceptions with descriptive messages
- ML service failures: Graceful degradation with local fallback algorithm

**Error Types:**
- C#: Standard exceptions (ArgumentException, InvalidOperationException)
- No custom exception classes detected
- Logging: Serilog for structured error logging with context

## Logging

**Framework:**
- Serilog 4.3.0 + Serilog.AspNetCore 8.0.3 - Structured logging
- Serilog.Sinks.File 6.0.0 - File output to `logs/app-{date}.log`
- Console output for development

**Patterns:**
- Structured logging: `_logger.LogInformation("Message {UserId}", userId)`
- Log at controller entry points and service boundaries
- Audit logging in EducationDbContext.SaveChangesAsync override
- Python: print statements with `[INFO]`, `[ERROR]` prefixes (no formal logger)

## Comments

**When to Comment:**
- C#: XML documentation comments for public APIs
- C#: Inline Russian comments for domain logic
- Python: Docstrings for modules and functions

**XML Documentation (C#):**
- Triple-slash `///` for public APIs
- Tags: `<summary>`, `<param name="">`, `<returns>`
- Example:
  ```csharp
  /// <summary>
  /// Получить список фильмов с фильтрацией и пагинацией
  /// </summary>
  [HttpGet]
  public async Task<IActionResult> GetMovies(...)
  ```

**Python Docstrings:**
- Triple-quoted strings for modules and functions
- Numpy/Google style format
- Example:
  ```python
  def get_recommendations(self, user_id: int, n: int = 10) -> List[Dict[str, Any]]:
      """
      Get top-N movie recommendations for a user

      Uses hybrid approach:
      - Primary: Collaborative filtering (SVD)
      - Fallback: Content-based for cold start
      """
  ```

**Inline Comments:**
- C#: `//` for single-line comments in Russian
- Python: `#` for inline comments
- Explanatory comments for complex business logic

## Function Design

**Size:**
- No strict limit enforced
- Controllers range from 50-200 lines
- Service methods typically under 100 lines

**Parameters:**
- C#: Use nullable types with `?` for optional parameters
- C#: DTOs for complex parameter groups (RegisterDto, CreateMovieDto)
- Python: Type hints with default values (`n: int = 10`)

**Return Values:**
- C#: Explicit return types (Task<IActionResult>, Task<List<MlRecommendation>?>)
- C# API: Return ApiResponse<T> wrapper for consistent structure
- Python: Type hints for return values (`-> List[Dict[str, Any]]`)
- Early returns for guard clauses

## Module Design

**Exports:**
- C#: Public classes and interfaces in separate files
- C#: Internal classes marked with `internal` keyword
- Python: All module-level functions exported by default

**Dependency Injection (C#):**
- Constructor injection pattern
- Services registered in `plt/Program.cs`:
  ```csharp
  builder.Services.AddScoped<IJwtService, JwtService>();
  builder.Services.AddScoped<IMlRecommendationService, MlRecommendationService>();
  ```
- DbContext registered with scoped lifetime

**Nullable Reference Types (C#):**
- Enabled in project: `<Nullable>enable</Nullable>`
- Optional types marked with `?` (string?, int?, double?)
- Explicit null checks in code

---

*Convention analysis: 2026-01-13*
*Update when patterns change*
