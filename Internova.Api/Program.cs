using Internova.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Internova API",
        Version     = "v1",
        Description = "University Internship & Industry Matching Portal – REST API"
    });
});

// Infrastructure (EF Core + repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// ── Pipeline ──────────────────────────────────────────────────────────────────

var app = builder.Build();

// Always expose Swagger in all environments for this project
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internova API v1");
    c.RoutePrefix = "swagger"; // accessible at /swagger
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
