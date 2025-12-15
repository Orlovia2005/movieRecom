using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using plt.Models.Model; // твой DbContext

namespace plt
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            // читаем строку подключения из appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // регистрируем DbContext с PostgreSQL
            builder.Services.AddDbContext<EducationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // HttpContextAccessor нужен, так как он используется в DbContext
            builder.Services.AddHttpContextAccessor();

            // Добавляем контроллеры с представлениями
            builder.Services.AddControllersWithViews();

            // ? Добавляем аутентификацию Cookie
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";   // куда перекидывать, если не авторизован
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                    options.SlidingExpiration = true;
                });

            // Добавляем авторизацию
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ? МIDDLEWARE для аутентификации и авторизации
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");



            app.Run();

        }
    }
}
