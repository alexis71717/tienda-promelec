using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductApi.Models;
using TiendaPromElec.Services;

namespace TiendaPromElec.IntegrationTests.Helpers
{
    /// <summary>
    /// Factoría que levanta la API en memoria usando SQLite InMemory.
    /// Las variables de entorno se establecen en el constructor estático para
    /// que estén disponibles ANTES de que Program.cs llame a WebApplication.CreateBuilder.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string TestJwtSecret = "TestJwtSecret_AtLeast32CharsRequired_!!ABCDE";

        static CustomWebApplicationFactory()
        {
            // Estas variables DEBEN setearse antes de que Program.cs construya el builder.
            // Doble guion bajo (__) es la convención de ASP.NET Core para secciones anidadas.
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "DataSource=:memory:");
            Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", "TiendaPromElec");
            Environment.SetEnvironmentVariable("Jwt__Audience", "TiendaPromElecClients");
            Environment.SetEnvironmentVariable("Jwt__ExpiresMinutes", "60");
            Environment.SetEnvironmentVariable("Seed__AdminEmail", "admin@promelec.com");
            Environment.SetEnvironmentVariable("Seed__AdminPassword", "Admin#2025!");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        private DbConnection? _connection;

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Quitamos el registro original de DbContext (SQL Server) y lo reemplazamos por SQLite InMemory.
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

                var dbConnectionDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbConnection));
                if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);

                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddSingleton(_connection);
                services.AddDbContext<AppDbContext>((sp, options) =>
                {
                    options.UseSqlite(sp.GetRequiredService<DbConnection>());
                });
            });

            var host = base.CreateHost(builder);

            // Crea la BD en memoria y siembra roles + admin
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                foreach (var roleName in new[] { AuthService.RoleAdmin, AuthService.RoleUser })
                {
                    if (!roleManager.RoleExistsAsync(roleName).Result)
                        roleManager.CreateAsync(new IdentityRole(roleName)).Wait();
                }

                if (userManager.FindByEmailAsync("admin@promelec.com").Result == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = "admin@promelec.com",
                        Email = "admin@promelec.com",
                        FullName = "Admin Test",
                        EmailConfirmed = true
                    };
                    userManager.CreateAsync(admin, "Admin#2025!").Wait();
                    userManager.AddToRoleAsync(admin, AuthService.RoleAdmin).Wait();
                }
            }

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection?.Dispose();
                _connection = null;
            }
        }
    }
}