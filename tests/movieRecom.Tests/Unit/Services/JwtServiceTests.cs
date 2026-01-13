using Microsoft.Extensions.Configuration;
using movieRecom.Models.Model;
using movieRecom.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace movieRecom.Tests.Unit.Services;

public class JwtServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtService _jwtService;
    private readonly User _testUser;

    public JwtServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "YourSuperSecretKeyForJWT_MinLength32Chars!",
            ["Jwt:Issuer"] = "MovieRecomAPI",
            ["Jwt:Audience"] = "MovieRecomClient",
            ["Jwt:ExpireMinutes"] = "60"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _jwtService = new JwtService(_configuration);

        _testUser = new User
        {
            Id = 1,
            Name = "TestUser",
            LastName = "TestLastName",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = UserRole.User,
            AvatarUrl = "https://example.com/avatar.jpg"
        };
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyToken()
    {
        // Act
        var token = _jwtService.GenerateAccessToken(_testUser);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsValidJwtFormat()
    {
        // Act
        var token = _jwtService.GenerateAccessToken(_testUser);

        // Assert
        token.Should().Contain(".");
        token.Split('.').Should().HaveCount(3); // Header.Payload.Signature
    }

    [Fact]
    public void GenerateAccessToken_ContainsUserClaims()
    {
        // Act
        var token = _jwtService.GenerateAccessToken(_testUser);

        // Decode and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "TestUser");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectExpiration()
    {
        // Act
        var token = _jwtService.GenerateAccessToken(_testUser);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert - должен истечь примерно через 60 минут
        var expectedExpiration = DateTime.UtcNow.AddMinutes(60);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyToken()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert - должен быть валидный Base64
        var action = () => Convert.FromBase64String(refreshToken);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var token = _jwtService.GenerateAccessToken(_testUser);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        // Arrange
        var token = _jwtService.GenerateAccessToken(_testUser);
        var tamperedToken = token + "tampered";

        // Act
        var principal = _jwtService.ValidateToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        // Arrange
        var token = _jwtService.GenerateAccessToken(_testUser);

        // Act
        var userId = _jwtService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(_testUser.Id);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token";

        // Act
        var userId = _jwtService.GetUserIdFromToken(invalidToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GenerateAccessToken_AdminUser_HasAdminRole()
    {
        // Arrange
        var adminUser = new User
        {
            Id = 2,
            Name = "Admin",
            Email = "admin@example.com",
            Password = "hash",
            Role = UserRole.Admin
        };

        // Act
        var token = _jwtService.GenerateAccessToken(adminUser);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }
}
