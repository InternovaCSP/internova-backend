using System.Text;
using System.Text.Json.Serialization;
using Internova.Api.Presentation.Authorization;
using Internova.Api.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Internova.Api.Presentation.Extensions;

public static class ServiceCollectionExtensions
{
    public static void EnsureJwtConfigured(this IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "";
        var missing = string.IsNullOrWhiteSpace(jwtKey) ||
                      jwtKey.Contains("{set via", StringComparison.OrdinalIgnoreCase);

        if (!missing) return;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("""

            FATAL: JWT secrets are not configured.
            Run the following in Internova.Api before starting:
              dotnet user-secrets init
              dotnet user-secrets set "Jwt:Key" "<generate-a-32+-character-random-string>"
              dotnet user-secrets set "Jwt:Issuer" "Internova"
              dotnet user-secrets set "Jwt:Audience" "InternovaUsers"

            """);
        Console.ResetColor();
        Environment.Exit(1);
    }

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddSignalR();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddScoped<IAuthorizationHandler, CompanyApprovalHandler>();

        services.AddSwaggerDocs();
        services.AddJwtAuth(configuration);
        services.AddCorsPolicies(configuration);

        return services;
    }

    private static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Internova API",
                Version = "v1",
                Description = "University Internship & Industry Matching Portal – REST API"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token. Example: eyJhbGci..."
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

        return services;
    }

    private static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "";

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
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireCompanyApproval", policy =>
                policy.AddRequirements(new CompanyApprovalRequirement()));
        });

        return services;
    }

    private static IServiceCollection AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("ViteDev", policy =>
                policy.WithOrigins(
                        "http://localhost:5173",
                        "http://localhost:5174",
                        "http://localhost:5128")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());

            options.AddPolicy("DeployedCorsPolicy", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}
