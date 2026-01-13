# Codebase Structure

**Analysis Date:** 2026-01-13

## Directory Layout

```
movieRecom/
├── movieRecom.sln              # Visual Studio solution file
├── docker-compose.yml          # Container orchestration
├── CLAUDE.md                   # Project guidelines
├── .gitignore, .gitattributes
│
├── plt/                        # ASP.NET Core 9.0 MVC Web Application
│   ├── Program.cs              # Application entry point
│   ├── movieRecom.csproj       # Project file
│   ├── appsettings.json        # Configuration
│   ├── Dockerfile              # Container definition
│   │
│   ├── Controllers/            # HTTP request handlers
│   │   ├── BaseController.cs
│   │   ├── AccountController.cs
│   │   ├── MoviesController.cs
│   │   ├── RecommendationsController.cs
│   │   ├── WishlistController.cs
│   │   ├── AdminController.cs
│   │   ├── HistoryController.cs
│   │   └── Api/                # REST API Controllers
│   │       ├── AuthApiController.cs
│   │       ├── MoviesApiController.cs
│   │       ├── RecommendationsApiController.cs
│   │       ├── AdminApiController.cs
│   │       ├── WishlistApiController.cs
│   │       └── HistoryApiController.cs
│   │
│   ├── Services/               # Business logic
│   │   ├── MlRecommendationService.cs
│   │   ├── IMlRecommendationService.cs
│   │   └── JwtService.cs
│   │
│   ├── Models/
│   │   ├── Model/              # Domain entities
│   │   │   ├── EducationDbContext.cs
│   │   │   ├── User.cs
│   │   │   ├── Movie.cs
│   │   │   ├── Genre.cs
│   │   │   ├── MovieGenre.cs
│   │   │   ├── Rating.cs
│   │   │   ├── Wishlist.cs
│   │   │   ├── Comment.cs
│   │   │   ├── RefreshToken.cs
│   │   │   └── HiddenRecommendation.cs
│   │   ├── DTO/                # Data transfer objects
│   │   │   ├── ApiResponse.cs
│   │   │   ├── AuthDto.cs
│   │   │   ├── MovieDto.cs
│   │   │   ├── RatingDto.cs
│   │   │   ├── RecommendationDto.cs
│   │   │   └── MlServiceDto.cs
│   │   └── ViewModel/          # Server-rendered view data
│   │       ├── BaseViewModel.cs
│   │       ├── RecommendationsViewModel.cs
│   │       ├── MovieDetailsViewModel.cs
│   │       └── MovieCatalogViewModel.cs
│   │
│   ├── Views/                  # Razor view templates
│   │   ├── _ViewImports.cshtml
│   │   ├── _ViewStart.cshtml
│   │   ├── Account/
│   │   ├── Movies/
│   │   ├── Recommendations/
│   │   ├── Wishlist/
│   │   ├── History/
│   │   ├── Admin/
│   │   ├── Home/
│   │   └── Shared/
│   │
│   ├── wwwroot/                # Static files
│   │   ├── css/
│   │   ├── js/
│   │   ├── lib/
│   │   └── Images/
│   │
│   └── Migrations/             # EF Core migrations
│
├── ml_service/                 # Python Flask ML Microservice
│   ├── app.py                  # Flask entry point
│   ├── recommender.py          # ML algorithms (SVD + TF-IDF)
│   ├── database.py             # PostgreSQL connection
│   ├── data_loader.py
│   ├── requirements.txt
│   ├── .env
│   ├── .env.example
│   ├── Dockerfile
│   └── README.md
│
├── tests/                      # Test project
│   └── movieRecom.Tests/
│       ├── movieRecom.Tests.csproj
│       ├── Unit/
│       │   └── Services/
│       ├── Integration/
│       └── Helpers/
│
└── .planning/                  # GSD planning docs
    └── codebase/               # Codebase documentation
```

## Directory Purposes

**plt/**
- Purpose: Main ASP.NET Core MVC web application
- Contains: Controllers, Services, Models, Views, Migrations
- Key files: Program.cs (entry), movieRecom.csproj (dependencies), appsettings.json (config)

**plt/Controllers/**
- Purpose: HTTP request handlers
- Contains: MVC controllers (server-rendered views) and API controllers (JSON responses)
- Subdirectories: Api/ (REST endpoints)

**plt/Services/**
- Purpose: Business logic layer
- Contains: Service implementations and interfaces
- Key files: MlRecommendationService.cs (ML integration), JwtService.cs (authentication)

**plt/Models/**
- Purpose: Data structures
- Subdirectories:
  - Model/ - Domain entities (EF Core)
  - DTO/ - API serialization contracts
  - ViewModel/ - Server-rendered view data

**plt/Views/**
- Purpose: Razor view templates
- Contains: .cshtml files organized by feature
- Key files: _ViewStart.cshtml, _Layout.cshtml (shared layout)

**plt/Migrations/**
- Purpose: EF Core database migrations
- Contains: Migration files with timestamps (*.cs, *.Designer.cs)
- Key file: EducationDbContextModelSnapshot.cs (current schema)

**ml_service/**
- Purpose: Python Flask ML recommendation service
- Contains: Flask app, recommendation algorithms, database utilities
- Key files: app.py (entry), recommender.py (ML), database.py (queries)

**tests/movieRecom.Tests/**
- Purpose: Unit and integration tests
- Subdirectories: Unit/Services/, Integration/, Helpers/
- Key files: JwtServiceTests.cs, MlRecommendationServiceTests.cs, ApiIntegrationTests.cs

## Key File Locations

**Entry Points:**
- `plt/Program.cs` - ASP.NET Core application entry
- `ml_service/app.py` - Flask ML service entry
- `docker-compose.yml` - Multi-service orchestration

**Configuration:**
- `plt/appsettings.json` - Database connection, JWT secrets, ML service URL
- `ml_service/.env` - Python service environment variables
- `plt/movieRecom.csproj` - NuGet dependencies

**Core Logic:**
- `plt/Services/` - Business services
- `plt/Models/Model/EducationDbContext.cs` - Database context
- `ml_service/recommender.py` - ML algorithms

**Testing:**
- `tests/movieRecom.Tests/Unit/Services/` - Service unit tests
- `tests/movieRecom.Tests/Integration/` - API integration tests

**Documentation:**
- `CLAUDE.md` - Project instructions for Claude Code
- `ml_service/README.md` - ML service documentation

## Naming Conventions

**Files:**
- PascalCase for C# files (AccountController.cs, MovieDto.cs)
- snake_case for Python files (app.py, recommender.py)
- *.test.cs for test files

**Directories:**
- PascalCase for C# directories (Controllers/, Services/, Models/)
- snake_case for Python directories (ml_service/)
- Plural for collections (Controllers/, Views/)

**Special Patterns:**
- {Feature}Controller.cs - MVC controllers
- {Feature}ApiController.cs - API controllers
- I{Service}.cs - Interface definitions
- {Domain}Dto.cs - Data transfer objects
- {Feature}ViewModel.cs - View models
- _Layout.cshtml - Shared layout (underscore prefix)

## Where to Add New Code

**New Feature:**
- Primary code: `plt/Controllers/{Feature}Controller.cs`
- Tests: `tests/movieRecom.Tests/Unit/` or `/Integration/`
- Views: `plt/Views/{Feature}/`

**New API Endpoint:**
- Implementation: `plt/Controllers/Api/{Feature}ApiController.cs`
- DTO: `plt/Models/DTO/{Feature}Dto.cs`
- Tests: `tests/movieRecom.Tests/Integration/ApiIntegrationTests.cs`

**New Service:**
- Interface: `plt/Services/I{Service}.cs`
- Implementation: `plt/Services/{Service}.cs`
- Register in `plt/Program.cs` (dependency injection)

**New Entity:**
- Domain model: `plt/Models/Model/{Entity}.cs`
- Add DbSet to `plt/Models/Model/EducationDbContext.cs`
- Create migration: `dotnet ef migrations add Add{Entity}Models`

**New View:**
- Template: `plt/Views/{Feature}/{Action}.cshtml`
- ViewModel: `plt/Models/ViewModel/{Feature}ViewModel.cs`

## Special Directories

**plt/wwwroot/**
- Purpose: Static files (CSS, JS, images)
- Source: Manually created, not generated
- Committed: Yes

**plt/Migrations/**
- Purpose: EF Core database migrations
- Source: Auto-generated by `dotnet ef migrations add`
- Committed: Yes (required for database schema)

**ml_service/models/**
- Purpose: Trained ML model storage
- Source: Generated by POST /train endpoint
- Committed: No (excluded in .gitignore)

**.planning/**
- Purpose: GSD (Get Shit Done) planning documents
- Source: Created by /gsd:map-codebase and related commands
- Committed: Yes

---

*Structure analysis: 2026-01-13*
*Update when directory structure changes*
