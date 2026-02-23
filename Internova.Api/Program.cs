using Internova.Infrastructure.Data;
using Internova.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Startup Guard: Fail fast if secrets are missing ───────────────────────────

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "";

bool connectionStringMissing = string.IsNullOrWhiteSpace(connectionString) ||
    connectionString.Contains("{your_password", StringComparison.OrdinalIgnoreCase);
bool jwtKeyMissing = string.IsNullOrWhiteSpace(jwtKey) ||
    jwtKey.Contains("{set via", StringComparison.OrdinalIgnoreCase);

if (connectionStringMissing || jwtKeyMissing)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("""

    ╔══════════════════════════════════════════════════════════════════════╗
    ║  FATAL: Required secrets are not configured.                        ║
    ║  Run the following commands in /Internova.Api before starting:      ║
    ╚══════════════════════════════════════════════════════════════════════╝

    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
      "Server=tcp:internovacsp.database.windows.net,1433;Initial Catalog=internova_db;Persist Security Info=False;User ID=internova_CS;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    dotnet user-secrets set "Jwt:Key" "<generate-a-32+-character-random-string>"
    dotnet user-secrets set "Jwt:Issuer" "Internova"
    dotnet user-secrets set "Jwt:Audience" "InternovaUsers"

    """);
    Console.ResetColor();
    Environment.Exit(1);
}

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

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

builder.Services.AddAuthorization();

// Infrastructure (EF Core + repositories)
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

app.UseHttpsRedirection();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
