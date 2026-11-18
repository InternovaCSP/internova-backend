using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/admin/companies")]
[Authorize(Roles = "Admin")]
public class AdminCompaniesController(
    ICompanyProfileRepository companyRepository,
    IInternshipRepository internshipRepository,
    ILogger<AdminCompaniesController> logger) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending() =>
        Ok(await companyRepository.GetPendingCompaniesAsync());

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await companyRepository.GetAllCompaniesAsync());

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCompanyStatusRequest request)
    {
        if (!Enum.IsDefined(typeof(CompanyStatus), request.Status))
            return BadRequest(new { error = "Invalid status." });

        var updated = await companyRepository.UpdateStatusAsync(id, request.Status);
        if (!updated)
            return NotFound(new { error = "Company not found." });

        logger.LogInformation("Admin updated company {CompanyId} status to {Status}.", id, request.Status);
        return Ok(new { message = $"Company status updated to {request.Status}." });
    }

    [HttpGet("/api/admin/internships/pending")]
    public async Task<IActionResult> GetPendingInternships()
    {
        var internships = await internshipRepository.GetAllAsync();
        return Ok(internships.Where(i => i.Status == "Pending Approval").ToList());
    }

    [HttpGet("{id:int}/internships")]
    public async Task<IActionResult> GetCompanyInternships(int id) =>
        Ok(await internshipRepository.GetByCompanyIdAsync(id));

    [HttpPatch("/api/admin/internships/{internshipId:int}/status")]
    public async Task<IActionResult> UpdateInternshipStatus(
        int internshipId,
        [FromBody] UpdateInternshipStatusRequest request)
    {
        var internship = await internshipRepository.GetByIdAsync(internshipId);
        if (internship is null)
            return NotFound(new { error = "Internship not found." });

        internship.Status = request.Status;
        var updated = await internshipRepository.UpdateAsync(internship);
        if (!updated)
            return StatusCode(500, new { error = "Failed to update internship status." });

        logger.LogInformation("Admin updated internship {InternshipId} status to {Status}.", internshipId, request.Status);
        return Ok(new { message = $"Internship status updated to {request.Status}." });
    }
}
