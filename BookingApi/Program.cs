
using BookingApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BookingApi;

/// <summary>
/// The main entry point class for the application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point for the application. This method builds the web host,
    /// applies any pending database migrations, and then runs the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();

        using (IServiceScope scope = host.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            try
            {
                BookingDbContext dbContext = services.GetRequiredService<BookingDbContext>();
                if (dbContext.Database.IsRelational())
                {
                    await dbContext.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        await host.RunAsync();
    }

    /// <summary>
    /// Creates and configures the application's web host builder.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A configured IHostBuilder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}