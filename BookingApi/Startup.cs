using BookingApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BookingApi.Data.Dtos;
using BookingApi.Helpers;

namespace BookingApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        string? connectionString;
        string? jwtKeyValue;
        string? issuer;
        string? setupKeyValue;

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (env == "Development")
        {
            connectionString = Configuration.GetConnectionString("DefaultConnection");
            jwtKeyValue = Configuration["Jwt:Key"];
            issuer = Configuration["Jwt:Issuer"];
            setupKeyValue = Configuration["AdminSetupKey"];
        }
        else
        {
            DbConnectionInfoDto dbInfo = BookingApi.Helpers.DatabaseConfigHelper.GetDbConnectionInfoAsync()
            .GetAwaiter().GetResult();

            connectionString =
                $"Host={dbInfo.Host};" +
                $"Port=5432;" +
                $"Database={dbInfo.Database};" +
                $"Username={dbInfo.Username};" +
                $"Password={dbInfo.Password};" +
                $"Pooling=true;SSL Mode=Require;Trust Server Certificate=true;";

            string? jwtKeyEnv = Environment.GetEnvironmentVariable("Jwt__Key")
                    ?? Configuration["Jwt:Key"];

            jwtKeyValue = SecretHelper.ResolveAsync(jwtKeyEnv).GetAwaiter().GetResult();

            issuer = Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("Jwt__Issuer");

            string? setupKeyEnv = Environment.GetEnvironmentVariable("AdminSetupKey")
                      ?? Configuration["AdminSetupKey"];
            setupKeyValue = SecretHelper.ResolveAsync(setupKeyEnv).GetAwaiter().GetResult();
        }

        Configuration["Jwt:Key"] = jwtKeyValue;
        Configuration["Jwt:Issuer"] = issuer;
        Configuration["AdminSetupKey"] = setupKeyValue;

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]!))
            };
        });

        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (IServiceScope scope = app.ApplicationServices.CreateScope())
        {
            BookingDbContext db = scope.ServiceProvider.GetRequiredService<BookingApi.Data.BookingDbContext>();
            db.Database.Migrate();
        }

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}