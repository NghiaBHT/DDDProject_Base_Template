using DDDProject.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;

namespace DDDProject.Api.IntegrationTests.Common;

// Generic type argument points to the API project's Program or Startup class
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's ApplicationDbContext registration.
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Also remove the DbContext registration itself, if present
            var dbContextServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextServiceDescriptor != null)
            {
                services.Remove(dbContextServiceDescriptor);
            }

            // Remove the DbConnection registration if it exists
            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            // Create open SqliteConnection so EF Core won't automatically close it.
            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                return connection;
            });

            // Add ApplicationDbContext using an in-memory database for testing.
            services.AddDbContext<ApplicationDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            // --- Mock External Services --- 
            // Remove existing IEmailSender registration if present (safer)
            var emailSenderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailSender));
            if (emailSenderDescriptor != null)
            {
                services.Remove(emailSenderDescriptor);
            }
            // Register Mock IEmailSender
            services.AddSingleton(new Mock<IEmailSender>().Object);
            
            // Add other mock services here if needed (e.g., IJwtTokenGenerator if not using real tokens)
        });

        builder.UseEnvironment("Testing"); // Use a specific environment for tests if needed
    }
} 