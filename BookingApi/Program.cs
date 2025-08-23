
using BookingApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BookingApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();

        using (IServiceScope scope = host.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            try
            {
                BookingDbContext dbContext = services.GetRequiredService<BookingDbContext>();
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}