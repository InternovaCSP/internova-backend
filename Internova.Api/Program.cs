using Internova.Api.Infrastructure;
using Internova.Api.Infrastructure.Data;
using Internova.Api.Presentation.Extensions;
using Internova.Api.Presentation.Hubs;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.EnsureJwtConfigured();

builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddInfrastructure();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    await DatabaseInitializer.InitializeAsync(app.Configuration, logger);
}

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internova API v1");
    c.RoutePrefix = "swagger";
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(app.Environment.IsDevelopment() ? "ViteDev" : "DeployedCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public partial class Program { }
