
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Middleware;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<ThreadfolioContext>(options =>
                options.UseSqlite(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevCors", policy =>
                {
                    policy.WithOrigins("https://localhost:7296", "http://localhost:5190")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/login";
                    o.LogoutPath = "/logout";
                    o.SlidingExpiration = true;
                    o.ExpireTimeSpan = TimeSpan.FromHours(8);
                    o.Cookie.HttpOnly = true;
                    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    o.Cookie.SameSite = SameSiteMode.Lax;
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();

            //Seed Data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ThreadfolioContext>();
                context.Database.EnsureCreated();
                try
                {
                    SeedData.Initialize(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            //Middleware
            app.UseCors("DevCors"); 
            app.UseRequestLogging();
            app.UseAuthentication();
            app.UseAuthorizationLogging();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseRouting();


            app.MapControllers();
            app.MapRazorPages();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
