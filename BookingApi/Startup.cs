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

        DbConnectionInfoDto dbInfo = BookingApi.Helpers.DatabaseConfigHelper.GetDbConnectionInfoAsync()
            .GetAwaiter().GetResult();

        string connectionString =
            $"Host={dbInfo.Host};" +
            $"Port=5432;" +
            $"Database={dbInfo.Database};" +
            $"Username={dbInfo.Username};" +
            $"Password={dbInfo.Password};" +
            $"Pooling=true;SSL Mode=Require;Trust Server Certificate=true;";

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(connectionString));

        string? jwtKeyEnv = Environment.GetEnvironmentVariable("Jwt__Key")
                    ?? Configuration["Jwt:Key"];
        string? jwtKeyValue = SecretHelper.ResolveAsync(jwtKeyEnv).GetAwaiter().GetResult();

        string? issuer = Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("Jwt__Issuer");

        string? setupKeyEnv = Environment.GetEnvironmentVariable("AdminSetupKey")
                      ?? Configuration["AdminSetupKey"];
        string? setupKeyValue = SecretHelper.ResolveAsync(setupKeyEnv).GetAwaiter().GetResult();

        Configuration["Jwt:Key"] = jwtKeyValue;
        Configuration["Jwt:Issuer"] = issuer;
        Configuration["AdminSetupKey"] = setupKeyValue;  

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
                ValidIssuer = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyValue!))
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