using BookingApi.Data;
using BookingApi.Data.Dtos;
using BookingApi.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

namespace BookingApi;

/// <summary>
/// Configures the services and the application's request pipeline.
/// This class is used by the host builder in Program.cs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Startup"/> class.
/// </remarks>
/// <param name="configuration">The application's configuration properties.</param>
public class Startup(IConfiguration configuration)
{
    /// <summary>
    /// Gets the application's configuration properties.
    /// </summary>
    public IConfiguration Configuration { get; } = configuration;

    /// <summary>
    /// This method gets called by the runtime. Use this method to add services to the container.
    /// </summary>
    /// <param name="services">The collection of services to add to the application.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        string? connectionString;
        string? jwtKeyValue;
        string? issuer;
        string? setupKeyValue;

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (env == "Production")
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
        else
        {
            connectionString = Configuration.GetConnectionString("DefaultConnection");
            jwtKeyValue = Configuration["Jwt:Key"];
            issuer = Configuration["Jwt:Issuer"];
            setupKeyValue = Configuration["AdminSetupKey"];
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

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Booking API",
                Version = "v1",
                Description = "A professional REST API for a booking system built with .NET."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new string[]{}
                }
            });

            string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });
    }

    /// <summary>
    /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The application's request pipeline builder.</param>
    /// <param name="env">The hosting environment information.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (IServiceScope scope = app.ApplicationServices.CreateScope())
        {
            BookingDbContext db = scope.ServiceProvider.GetRequiredService<BookingApi.Data.BookingDbContext>();
            if (!env.IsEnvironment("Testing") && db.Database.IsRelational())
            {
                db.Database.Migrate();
            }
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}