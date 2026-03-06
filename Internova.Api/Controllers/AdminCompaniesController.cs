using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/admin/companies")]
[Authorize(Roles = "Admin")]
public class AdminCompaniesController(
    ICompanyProfileRepository companyRepository,
    ILogger<AdminCompaniesController> logger) : ControllerBase
{
    // ─── GET /api/admin/companies/pending ──────────────────────────────────────
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var pendingCompanies = await companyRepository.GetPendingCompaniesAsync();
        return Ok(pendingCompanies);
    }

    // ─── PATCH /api/admin/companies/{id}/status ────────────────────────────────
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        if (!Enum.IsDefined(typeof(CompanyStatus), request.Status))
        {
            return BadRequest(new { error = "Invalid status." });
        }

        var success = await companyRepository.UpdateStatusAsync(id, request.Status);
        if (!success)
        {
            return NotFound(new { error = "Company not found." });
        }

        logger.LogInformation("Admin updated company {CompanyId} status to {Status}.", id, request.Status);
        return Ok(new { message = $"Company status updated to {request.Status}." });
    }

    public class UpdateStatusRequest
    {
        public CompanyStatus Status { get; set; }
    }
}
