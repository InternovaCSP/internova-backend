using Internova.Api.Controllers;
using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Internova.Tests;

/// <summary>
/// Unit tests for <see cref="ApplicationsController"/>.
/// Coverage: Apply, GetStudentApplications, GetCompanyApplications,
///           UpdateStatus, GetStudentProfile, GetPipelineStats, GetKpiStats.
/// </summary>
public class ApplicationsControllerTests
{
    private readonly Mock<IInternshipApplicationRepository> _appRepoMock;
    private readonly Mock<IStudentProfileRepository> _profileRepoMock;
    private readonly Mock<ILogger<ApplicationsController>> _loggerMock;
    private readonly ApplicationsController _controller;

    public ApplicationsControllerTests()
    {
        _appRepoMock     = new Mock<IInternshipApplicationRepository>();
        _profileRepoMock = new Mock<IStudentProfileRepository>();
        _loggerMock      = new Mock<ILogger<ApplicationsController>>();
        _controller = new ApplicationsController(
            _appRepoMock.Object,
            _profileRepoMock.Object,
            _loggerMock.Object);
    }

    private void SetUserContext(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity  = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ── APPLY ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Apply_NewApplication_ReturnsOk()
    {
        SetUserContext(5, "Student");
        _appRepoMock
            .Setup(r => r.GetByStudentIdAsync(5))
            .ReturnsAsync(new List<InternshipApplication>());
        _appRepoMock
            .Setup(r => r.AddAsync(It.IsAny<InternshipApplication>()))
            .ReturnsAsync(new InternshipApplication { Id = 1 });

        var result = await _controller.Apply(new ApplicationsController.ApplyRequest { InternshipId = 10 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Apply_DuplicateApplication_ReturnsBadRequest()
    {
        SetUserContext(5, "Student");
        // Student already applied for InternshipId=10
        var existing = new List<InternshipApplication>
        {
            new InternshipApplication { Id = 1, InternshipId = 10, StudentId = 5 }
        };
        _appRepoMock.Setup(r => r.GetByStudentIdAsync(5)).ReturnsAsync(existing);

        var result = await _controller.Apply(new ApplicationsController.ApplyRequest { InternshipId = 10 });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Apply_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.Apply(new ApplicationsController.ApplyRequest { InternshipId = 10 });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Apply_AddsApplicationWithCorrectStudentIdAndStatus()
    {
        SetUserContext(7, "Student");
        _appRepoMock.Setup(r => r.GetByStudentIdAsync(7)).ReturnsAsync(new List<InternshipApplication>());

        InternshipApplication? captured = null;
        _appRepoMock
            .Setup(r => r.AddAsync(It.IsAny<InternshipApplication>()))
            .Callback<InternshipApplication>(a => captured = a)
            .ReturnsAsync(new InternshipApplication());

        await _controller.Apply(new ApplicationsController.ApplyRequest { InternshipId = 20 });

        Assert.NotNull(captured);
        Assert.Equal(7,  captured!.StudentId);
        Assert.Equal(20, captured.InternshipId);
        Assert.Equal(ApplicationStatus.Applied, captured.Status);
    }

    // ── GET STUDENT APPLICATIONS ─────────────────────────────────────────────

    [Fact]
    public async Task GetStudentApplications_ReturnsOkWithList()
    {
        SetUserContext(5, "Student");
        var apps = new List<InternshipApplication>
        {
            new InternshipApplication { Id = 1, StudentId = 5 },
            new InternshipApplication { Id = 2, StudentId = 5 }
        };
        _appRepoMock.Setup(r => r.GetByStudentIdAsync(5)).ReturnsAsync(apps);

        var result = await _controller.GetStudentApplications();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(apps, ok.Value);
    }

    [Fact]
    public async Task GetStudentApplications_NoApplications_ReturnsEmptyList()
    {
        SetUserContext(5, "Student");
        _appRepoMock.Setup(r => r.GetByStudentIdAsync(5)).ReturnsAsync(new List<InternshipApplication>());

        var result = await _controller.GetStudentApplications();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsAssignableFrom<IEnumerable<InternshipApplication>>(ok.Value);
    }

    [Fact]
    public async Task GetStudentApplications_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetStudentApplications();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── GET COMPANY APPLICATIONS ─────────────────────────────────────────────

    [Fact]
    public async Task GetCompanyApplications_ReturnsOkWithList()
    {
        SetUserContext(10, "Company");
        var apps = new List<InternshipApplication>
        {
            new InternshipApplication { Id = 3, InternshipId = 1 }
        };
        _appRepoMock.Setup(r => r.GetByCompanyIdAsync(10)).ReturnsAsync(apps);

        var result = await _controller.GetCompanyApplications();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(apps, ok.Value);
    }

    [Fact]
    public async Task GetCompanyApplications_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetCompanyApplications();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── UPDATE STATUS ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidStatus_ReturnsOk()
    {
        SetUserContext(10, "Company");
        _appRepoMock.Setup(r => r.UpdateStatusAsync(1, ApplicationStatus.Shortlisted)).ReturnsAsync(true);

        var result = await _controller.UpdateStatus(1, new ApplicationsController.UpdateStatusRequest { Status = "Shortlisted" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsBadRequest()
    {
        SetUserContext(10, "Company");

        var result = await _controller.UpdateStatus(1, new ApplicationsController.UpdateStatusRequest { Status = "NotAStatus" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ApplicationNotFound_ReturnsNotFound()
    {
        SetUserContext(10, "Company");
        _appRepoMock.Setup(r => r.UpdateStatusAsync(99, It.IsAny<ApplicationStatus>())).ReturnsAsync(false);

        var result = await _controller.UpdateStatus(99, new ApplicationsController.UpdateStatusRequest { Status = "Rejected" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("Selected")]
    [InlineData("Rejected")]
    [InlineData("InterviewScheduled")]
    [InlineData("Interviewing")]
    public async Task UpdateStatus_AllValidStatuses_ReturnOk(string status)
    {
        SetUserContext(10, "Company");
        _appRepoMock.Setup(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<ApplicationStatus>())).ReturnsAsync(true);

        var result = await _controller.UpdateStatus(1, new ApplicationsController.UpdateStatusRequest { Status = status });

        Assert.IsType<OkObjectResult>(result);
    }

    // ── GET STUDENT PROFILE ──────────────────────────────────────────────────

    [Fact]
    public async Task GetStudentProfile_ExistingStudent_ReturnsOk()
    {
        SetUserContext(10, "Company");
        var profile = new StudentProfile { UserId = 5 };
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(profile);

        var result = await _controller.GetStudentProfile(5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    [Fact]
    public async Task GetStudentProfile_MissingStudent_ReturnsNotFound()
    {
        SetUserContext(10, "Company");
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync((StudentProfile?)null);

        var result = await _controller.GetStudentProfile(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── PIPELINE STATS ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPipelineStats_ReturnsOkWithDictionary()
    {
        SetUserContext(5, "Student");
        var stats = new Dictionary<string, int> { ["Applied"] = 3, ["Shortlisted"] = 1 };
        _appRepoMock.Setup(r => r.GetPipelineStatsAsync(5)).ReturnsAsync(stats);

        var result = await _controller.GetPipelineStats();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(stats, ok.Value);
    }

    [Fact]
    public async Task GetPipelineStats_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetPipelineStats();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── KPI STATS ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetKpiStats_ReturnsOkWithDictionary()
    {
        SetUserContext(5, "Student");
        var kpi = new Dictionary<string, string> { ["TotalApplications"] = "5" };
        _appRepoMock.Setup(r => r.GetKpiStatsAsync(5)).ReturnsAsync(kpi);

        var result = await _controller.GetKpiStats();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(kpi, ok.Value);
    }

    [Fact]
    public async Task GetKpiStats_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetKpiStats();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
