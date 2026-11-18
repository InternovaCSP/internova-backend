using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Internova.Api.Presentation.Controllers;

/// <summary>
/// Health-check endpoints to verify API liveness and database connectivity.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping() => Ok(new { status = "ok" });

    [HttpGet("db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DbHealth()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "error", message = "Database connection string is not configured." });
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT COUNT(*) FROM sys.tables WHERE name = 'User'";
            await using var cmd = new SqlCommand(sql, connection);
            var tableExists = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0) >= 1;

            if (!tableExists)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { status = "degraded", message = "Connected to DB but User table does not exist." });
            }

            return Ok(new { status = "healthy", database = connection.Database });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "error", message = ex.Message });
        }
    }
}
