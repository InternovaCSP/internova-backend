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
    IInternshipRepository internshipRepository,
    ILogger<AdminCompaniesController> logger) : ControllerBase
{
    // ─── GET /api/admin/companies/pending ──────────────────────────────────────
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var pendingCompanies = await companyRepository.GetPendingCompaniesAsync();
        return Ok(pendingCompanies);
    }

    // ─── GET /api/admin/companies ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var companies = await companyRepository.GetAllCompaniesAsync();
        return Ok(companies);
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

        // If company is being approved, we should also auto-approve their pending jobs? 
        // Or at least allow admin to see them.
        
        logger.LogInformation("Admin updated company {CompanyId} status to {Status}.", id, request.Status);
        return Ok(new { message = $"Company status updated to {request.Status}." });
    }

    // ─── GET /api/admin/internships/pending ─────────────────────────────────────
    [HttpGet("/api/admin/internships/pending")]
    public async Task<IActionResult> GetPendingInternships()
    {
        var internships = await internshipRepository.GetAllAsync();
        var pending = internships.Where(i => i.Status == "Pending Approval").ToList();
        return Ok(pending);
    }

    // ─── GET /api/admin/companies/{id}/internships ──────────────────────────────
    [HttpGet("{id}/internships")]
    public async Task<IActionResult> GetCompanyInternships(int id)
    {
        var internships = await internshipRepository.GetByCompanyIdAsync(id);
        return Ok(internships);
    }

    // ─── PATCH /api/admin/internships/{internshipId}/status ──────────────────────
    [HttpPatch("/api/admin/internships/{internshipId}/status")]
    public async Task<IActionResult> UpdateInternshipStatus(int internshipId, [FromBody] UpdateInternshipStatusRequest request)
    {
        var internship = await internshipRepository.GetByIdAsync(internshipId);
        if (internship == null) return NotFound(new { error = "Internship not found." });

        internship.Status = request.Status;
        var success = await internshipRepository.UpdateAsync(internship);
        
        if (!success) return StatusCode(500, new { error = "Failed to update internship status." });

        logger.LogInformation("Admin updated internship {InternshipId} status to {Status}.", internshipId, request.Status);
        return Ok(new { message = $"Internship status updated to {request.Status}." });
    }

    public class UpdateStatusRequest
    {
        public CompanyStatus Status { get; set; }
    }

    public class UpdateInternshipStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
