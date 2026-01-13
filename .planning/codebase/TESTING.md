# Testing Patterns

**Analysis Date:** 2026-01-13

## Test Framework

**Runner:**
- xUnit 2.9.2 - Primary test framework
- Config: `tests/movieRecom.Tests/movieRecom.Tests.csproj`

**Assertion Library:**
- FluentAssertions 6.12.2 - Fluent syntax for assertions
- Matchers: `.Should().Be()`, `.Should().NotBeNullOrEmpty()`, `.Should().HaveCount()`

**Run Commands:**
```bash
dotnet test                           # Run all tests
dotnet test --filter "FullyQualifiedName~JwtService"  # Run specific test class
dotnet test --collect:"XPlat Code Coverage"  # Coverage report
```

## Test File Organization

**Location:**
- Tests in separate project: `tests/movieRecom.Tests/`
- Subdirectories: `Unit/Services/`, `Integration/`, `Helpers/`

**Naming:**
- Test classes: `{ClassName}Tests.cs` (JwtServiceTests.cs, MlRecommendationServiceTests.cs)
- Test methods: `{MethodName}_{Condition}_{ExpectedResult}` (e.g., `GenerateAccessToken_ValidUser_ReturnsNonEmptyToken`)

**Structure:**
```
tests/
  movieRecom.Tests/
    Unit/
      Services/
        JwtServiceTests.cs           # 12 test methods
        MlRecommendationServiceTests.cs  # 9 test methods
    Integration/
      ApiIntegrationTests.cs         # 11 test methods
    Helpers/
      TestDbContextFactory.cs        # Test utilities
```

## Test Structure

**Suite Organization:**
```csharp
using Xunit;
using FluentAssertions;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly User _testUser;

    public JwtServiceTests()
    {
        // Arrange - shared setup in constructor
        var configuration = CreateTestConfiguration();
        _jwtService = new JwtService(configuration);
        _testUser = CreateTestUser();
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyToken()
    {
        // Act
        var token = _jwtService.GenerateAccessToken(_testUser);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("admin@test.com")]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal(string email)
    {
        // Arrange
        var user = CreateTestUser(email);
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal.FindFirst("Email")?.Value.Should().Be(email);
    }
}
```

**Patterns:**
- Constructor-based setup: Shared dependencies initialized in constructor
- AAA pattern: Arrange, Act, Assert with comments
- [Fact] for single test cases
- [Theory] with [InlineData] for parameterized tests

## Mocking

**Framework:**
- Moq 4.20.72 - Mocking library

**Patterns:**
```csharp
// Custom mock HTTP message handler
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public MockHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}

// Usage in test
var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
{
    StatusCode = HttpStatusCode.OK,
    Content = new StringContent(jsonResponse)
});
var httpClient = new HttpClient(mockHandler);
```

**What to Mock:**
- HTTP calls to external services (ML service)
- Database context in unit tests (use in-memory database)
- Time/dates for time-sensitive tests

**What NOT to Mock:**
- Pure business logic functions
- DTOs and domain models
- Simple utility methods

## Fixtures and Factories

**Test Data:**
```csharp
// Factory pattern in test helper
public static class TestDbContextFactory
{
    public static async Task<EducationDbContext> CreateSeededContextAsync()
    {
        var options = new DbContextOptionsBuilder<EducationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new EducationDbContext(options);

        // Seed users
        var users = new List<User>
        {
            new User { Id = 1, Email = "test@example.com", ... },
            new User { Id = 2, Email = "admin@example.com", ... }
        };
        context.Users.AddRange(users);

        // Seed movies, genres, ratings
        await context.SaveChangesAsync();
        return context;
    }
}
```

**Location:**
- Factory functions: `tests/movieRecom.Tests/Helpers/TestDbContextFactory.cs`
- Inline test data: Created in test methods or constructor

## Coverage

**Requirements:**
- No enforced coverage target
- Coverage tracked for awareness with Coverlet

**Configuration:**
- Tool: coverlet.collector 6.0.2
- Run: `dotnet test --collect:"XPlat Code Coverage"`
- Output: TestResults/ directory with coverage.cobertura.xml

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
# Open TestResults/{guid}/coverage.cobertura.xml with coverage viewer
```

## Test Types

**Unit Tests:**
- Scope: Test single service/method in isolation
- Mocking: Mock all external dependencies (HTTP clients, DbContext)
- Speed: Fast execution (each test <100ms)
- Examples:
  - `tests/movieRecom.Tests/Unit/Services/JwtServiceTests.cs` - 12 tests for JWT generation/validation
  - `tests/movieRecom.Tests/Unit/Services/MlRecommendationServiceTests.cs` - 9 tests for ML service integration

**Integration Tests:**
- Scope: Test multiple components together with in-memory database
- Mocking: Mock only external boundaries (file system, external APIs)
- Setup: WebApplicationFactory with in-memory database
- Examples:
  - `tests/movieRecom.Tests/Integration/ApiIntegrationTests.cs` - 11 tests for full API workflows

**E2E Tests:**
- Not currently implemented
- Python ML service: No tests detected

## Common Patterns

**Async Testing:**
```csharp
[Fact]
public async Task GetRecommendationsAsync_ValidUserId_ReturnsRecommendations()
{
    // Arrange
    var userId = 1;

    // Act
    var recommendations = await _mlService.GetRecommendationsAsync(userId);

    // Assert
    recommendations.Should().NotBeNull();
    recommendations.Should().HaveCountGreaterThan(0);
}
```

**Error Testing:**
```csharp
[Fact]
public void ValidateToken_InvalidToken_ThrowsSecurityTokenException()
{
    // Arrange
    var invalidToken = "invalid.jwt.token";

    // Act & Assert
    Assert.Throws<SecurityTokenException>(() =>
        _jwtService.ValidateToken(invalidToken));
}

// FluentAssertions style
[Fact]
public void ValidateToken_ExpiredToken_ThrowsException()
{
    // Arrange
    var expiredToken = GenerateExpiredToken();

    // Act
    Action act = () => _jwtService.ValidateToken(expiredToken);

    // Assert
    act.Should().Throw<SecurityTokenException>()
       .WithMessage("*expired*");
}
```

**Integration Test Setup:**
```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EducationDbContext>));
                services.Remove(descriptor);

                services.AddDbContext<EducationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsSuccessResponse()
    {
        // Arrange
        var registerDto = new { Email = "test@example.com", Password = "Test123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Snapshot Testing:**
- Not used in this codebase
- All assertions are explicit (no snapshot files)

## Test Count Summary

- **Unit Tests**: ~21 test methods
  - JwtServiceTests: 12 methods
  - MlRecommendationServiceTests: 9 methods
- **Integration Tests**: ~11 test methods
  - ApiIntegrationTests: 11 methods
- **Total**: ~32 test methods

---

*Testing analysis: 2026-01-13*
*Update when test patterns change*
