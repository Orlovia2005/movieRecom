using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using movieRecom.Models.Model;
using movieRecom.Services;
using Serilog;
using System.Text;
using System.Text.Json;

namespace movieRecom
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Настройка Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/app-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Запуск приложения MovieRecom");

                var builder = WebApplication.CreateBuilder(args);

                // Использование Serilog
                builder.Host.UseSerilog();

                // Чтение строки подключения из appsettings.json
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                var mlServiceUrl = builder.Configuration["MlService:BaseUrl"] ?? "http://localhost:5001";

                // Конфигурация DbContext с PostgreSQL
                builder.Services.AddDbContext<EducationDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // HttpContextAccessor
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

                // JWT Service
                builder.Services.AddScoped<IJwtService, JwtService>();

                // ML Recommendation Service
                builder.Services.AddScoped<IMlRecommendationService, MlRecommendationService>();

                // HttpClient для ML сервиса
                builder.Services.AddHttpClient("MlService", client =>
                {
                    client.BaseAddress = new Uri(mlServiceUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // Health Checks
                builder.Services.AddHealthChecks()
                    .AddNpgSql(
                        connectionString!,
                        name: "database",
                        failureStatus: HealthStatus.Unhealthy,
                        tags: new[] { "db", "postgres" })
                    .AddUrlGroup(
                        new Uri($"{mlServiceUrl}/health"),
                        name: "ml-service",
                        failureStatus: HealthStatus.Degraded,
                        tags: new[] { "ml", "external" });

                // Добавляем контроллеры с Views
                builder.Services.AddControllersWithViews();

                // Настройка аутентификации (Cookie + JWT)
                var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWT_MinLength32Chars!";
                var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MovieRecomAPI";
                var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MovieRecomClient";

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                    options.SlidingExpiration = true;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

                builder.Services.AddAuthorization();

                // Swagger/OpenAPI
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "MovieRecom API",
                        Version = "v1",
                        Description = "API для рекомендательной системы фильмов"
                    });

                    // JWT авторизация в Swagger
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header. Введите токен в формате: Bearer {token}",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                var app = builder.Build();

                // Apply pending migrations automatically on startup
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<EducationDbContext>();
                    try
                    {
                        Log.Information("Applying database migrations...");
                        dbContext.Database.Migrate();
                        Log.Information("Database migrations applied successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error applying database migrations");
                        throw;
                    }
                }

                // Serilog request logging
                app.UseSerilogRequestLogging(options =>
                {
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());

                        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            diagnosticContext.Set("UserId", userId);
                        }
                    };
                });

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MovieRecom API v1");
                        c.RoutePrefix = "swagger";
                    });
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                // Middleware для аутентификации и авторизации
                app.UseAuthentication();
                app.UseAuthorization();

                // Health Checks endpoint
                app.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = WriteHealthCheckResponse
                });

                // Детальный health check для внутреннего использования
                app.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("db"),
                    ResponseWriter = WriteHealthCheckResponse
                });

                app.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = _ => false // Просто проверяет, что приложение запущено
                });

                // MVC routes
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // API routes
                app.MapControllers();

                Log.Information("Приложение запущено успешно");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Приложение аварийно завершило работу");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Форматирует ответ health check в JSON
        /// </summary>
        private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                    exception = e.Value.Exception?.Message,
                    tags = e.Value.Tags
                })
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
