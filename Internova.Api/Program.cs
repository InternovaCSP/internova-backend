using Internova.Infrastructure.Data;
using Internova.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using Internova.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ── Startup Guard: Fail fast if JWT secrets are missing ──────────────────────

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "";

bool jwtKeyMissing = string.IsNullOrWhiteSpace(jwtKey) ||
    jwtKey.Contains("{set via", StringComparison.OrdinalIgnoreCase);

if (jwtKeyMissing)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("""

    ╔══════════════════════════════════════════════════════════════════════╗
    ║  FATAL: JWT secrets are not configured.                             ║
    ║  Run the following commands in /Internova.Api before starting:      ║
    ╚══════════════════════════════════════════════════════════════════════╝

    dotnet user-secrets init
    dotnet user-secrets set "Jwt:Key" "<generate-a-32+-character-random-string>"
    dotnet user-secrets set "Jwt:Issuer" "Internova"
    dotnet user-secrets set "Jwt:Audience" "InternovaUsers"

    Also ensure appsettings.Development.json contains your SQL Server connection string:
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=internova_db;Trusted_Connection=True;"
    }

    """);
    Console.ResetColor();
    Environment.Exit(1);
}

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSignalR();

// Swagger / OpenAPI with JWT Bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Internova API",
        Version     = "v1",
        Description = "University Internship & Industry Matching Portal – REST API"
    });

    // Add JWT Bearer input to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token. Example: eyJhbGci..."
    });

    // Use the XML documentation file
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Fix schema ID collisions by using full names
    c.CustomSchemaIds(type => type.FullName);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireCompanyApproval", policy =>
        policy.AddRequirements(new CompanyApprovalRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, CompanyApprovalHandler>();

// CORS — allow Vite dev server to call the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins(
                "http://localhost:5173",   // Vite dev server (default)
                "http://localhost:5174",   // Vite dev server (fallback)
                "http://localhost:5128")   // direct API access
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Infrastructure (ADO.NET + repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// ── Pipeline ──────────────────────────────────────────────────────────────────

var app = builder.Build();

// Database bootstrap (idempotent — safe on every startup)
var logger = app.Services.GetRequiredService<ILogger<Program>>();
await DatabaseInitializer.InitializeAsync(app.Configuration, logger);

// Always expose Swagger in all environments for this project
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internova API v1");
    c.RoutePrefix = "swagger";
});

// Configure Forwarded Headers to handle SSL termination in Azure/proxies
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Only use HTTPS redirection in production/Azure. 
// Locally, the 'http' profile triggers a warning if this is active.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS must be placed before Authentication/Authorization
app.UseCors("ViteDev");

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<Internova.Api.Hubs.NotificationHub>("/api/hubs/notifications");

app.Run();

public partial class Program { }

