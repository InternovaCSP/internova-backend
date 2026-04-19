using Internova.Api.Controllers;
using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Internova.Tests;

/// <summary>
/// Unit tests for <see cref="AdminCompaniesController"/>.
/// Coverage: GetPending, GetAll, UpdateStatus (company), 
///           GetPendingInternships, GetCompanyInternships, UpdateInternshipStatus.
/// </summary>
public class AdminCompaniesControllerTests
{
    private readonly Mock<ICompanyProfileRepository>  _companyRepoMock;
    private readonly Mock<IInternshipRepository>      _internshipRepoMock;
    private readonly Mock<ILogger<AdminCompaniesController>> _loggerMock;
    private readonly AdminCompaniesController         _controller;

    public AdminCompaniesControllerTests()
    {
        _companyRepoMock     = new Mock<ICompanyProfileRepository>();
        _internshipRepoMock  = new Mock<IInternshipRepository>();
        _loggerMock          = new Mock<ILogger<AdminCompaniesController>>();
        _controller = new AdminCompaniesController(
            _companyRepoMock.Object,
            _internshipRepoMock.Object,
            _loggerMock.Object);
    }

    // ── GET PENDING COMPANIES ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPending_ReturnsOkWithPendingList()
    {
        var companies = new List<CompanyProfile>
        {
            new CompanyProfile { CompanyId = 1, CompanyName = "Pending Corp", Status = CompanyStatus.Pending }
        };
        _companyRepoMock.Setup(r => r.GetPendingCompaniesAsync()).ReturnsAsync(companies);

        var result = await _controller.GetPending();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(companies, ok.Value);
    }

    [Fact]
    public async Task GetPending_EmptyList_ReturnsOkWithEmpty()
    {
        _companyRepoMock.Setup(r => r.GetPendingCompaniesAsync()).ReturnsAsync(new List<CompanyProfile>());

        var result = await _controller.GetPending();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsAssignableFrom<IEnumerable<CompanyProfile>>(ok.Value);
    }

    // ── GET ALL COMPANIES ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithAllCompanies()
    {
        var companies = new List<CompanyProfile>
        {
            new CompanyProfile { CompanyId = 1, Status = CompanyStatus.Active },
            new CompanyProfile { CompanyId = 2, Status = CompanyStatus.Pending }
        };
        _companyRepoMock.Setup(r => r.GetAllCompaniesAsync()).ReturnsAsync(companies);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(companies, ok.Value);
    }

    // ── UPDATE COMPANY STATUS ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidActiveStatus_ReturnsOk()
    {
        _companyRepoMock.Setup(r => r.UpdateStatusAsync(5, CompanyStatus.Active)).ReturnsAsync(true);

        var result = await _controller.UpdateStatus(
            5, new AdminCompaniesController.UpdateStatusRequest { Status = CompanyStatus.Active });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ValidRejectedStatus_ReturnsOk()
    {
        _companyRepoMock.Setup(r => r.UpdateStatusAsync(5, CompanyStatus.Rejected)).ReturnsAsync(true);

        var result = await _controller.UpdateStatus(
            5, new AdminCompaniesController.UpdateStatusRequest { Status = CompanyStatus.Rejected });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_CompanyNotFound_ReturnsNotFound()
    {
        _companyRepoMock.Setup(r => r.UpdateStatusAsync(99, It.IsAny<CompanyStatus>())).ReturnsAsync(false);

        var result = await _controller.UpdateStatus(
            99, new AdminCompaniesController.UpdateStatusRequest { Status = CompanyStatus.Active });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_CallsRepositoryWithCorrectArguments()
    {
        _companyRepoMock.Setup(r => r.UpdateStatusAsync(7, CompanyStatus.Active)).ReturnsAsync(true);

        await _controller.UpdateStatus(
            7, new AdminCompaniesController.UpdateStatusRequest { Status = CompanyStatus.Active });

        _companyRepoMock.Verify(r => r.UpdateStatusAsync(7, CompanyStatus.Active), Times.Once);
    }

    // ── GET PENDING INTERNSHIPS ──────────────────────────────────────────────

    [Fact]
    public async Task GetPendingInternships_ReturnsOnlyPendingApproval()
    {
        var all = new List<Internship>
        {
            new Internship { Id = 1, Status = "Active" },
            new Internship { Id = 2, Status = "Pending Approval" },
            new Internship { Id = 3, Status = "Pending Approval" }
        };
        _internshipRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(all);

        var result = await _controller.GetPendingInternships();

        var ok      = Assert.IsType<OkObjectResult>(result);
        var pending = Assert.IsAssignableFrom<IEnumerable<Internship>>(ok.Value);
        Assert.Equal(2, pending.Count());
        Assert.All(pending, i => Assert.Equal("Pending Approval", i.Status));
    }

    [Fact]
    public async Task GetPendingInternships_NoPending_ReturnsEmptyList()
    {
        _internshipRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Internship>
        {
            new Internship { Id = 1, Status = "Active" }
        });

        var result = await _controller.GetPendingInternships();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Internship>>(ok.Value);
        Assert.Empty(list);
    }

    // ── GET COMPANY INTERNSHIPS ──────────────────────────────────────────────

    [Fact]
    public async Task GetCompanyInternships_ReturnsOkWithList()
    {
        var internships = new List<Internship>
        {
            new Internship { Id = 1, CompanyId = 10 },
            new Internship { Id = 2, CompanyId = 10 }
        };
        _internshipRepoMock.Setup(r => r.GetByCompanyIdAsync(10)).ReturnsAsync(internships);

        var result = await _controller.GetCompanyInternships(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(internships, ok.Value);
    }

    [Fact]
    public async Task GetCompanyInternships_NoInternships_ReturnsEmptyList()
    {
        _internshipRepoMock.Setup(r => r.GetByCompanyIdAsync(99)).ReturnsAsync(new List<Internship>());

        var result = await _controller.GetCompanyInternships(99);

        var ok   = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Internship>>(ok.Value);
        Assert.Empty(list);
    }

    // ── UPDATE INTERNSHIP STATUS ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateInternshipStatus_ExistingInternship_ReturnsOk()
    {
        var internship = new Internship { Id = 1, Status = "Pending Approval" };
        _internshipRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(internship);
        _internshipRepoMock.Setup(r => r.UpdateAsync(internship)).ReturnsAsync(true);

        var result = await _controller.UpdateInternshipStatus(
            1, new AdminCompaniesController.UpdateInternshipStatusRequest { Status = "Active" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Active", internship.Status);
    }

    [Fact]
    public async Task UpdateInternshipStatus_NonExistingInternship_ReturnsNotFound()
    {
        _internshipRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Internship?)null);

        var result = await _controller.UpdateInternshipStatus(
            99, new AdminCompaniesController.UpdateInternshipStatusRequest { Status = "Active" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateInternshipStatus_UpdateFails_Returns500()
    {
        var internship = new Internship { Id = 1, Status = "Pending Approval" };
        _internshipRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(internship);
        _internshipRepoMock.Setup(r => r.UpdateAsync(internship)).ReturnsAsync(false);

        var result = await _controller.UpdateInternshipStatus(
            1, new AdminCompaniesController.UpdateInternshipStatusRequest { Status = "Active" });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, obj.StatusCode);
    }

    [Fact]
    public async Task UpdateInternshipStatus_SetsStatusCorrectly()
    {
        var internship = new Internship { Id = 3, Status = "Pending Approval" };
        _internshipRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(internship);
        _internshipRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Internship>())).ReturnsAsync(true);

        await _controller.UpdateInternshipStatus(
            3, new AdminCompaniesController.UpdateInternshipStatusRequest { Status = "Rejected" });

        Assert.Equal("Rejected", internship.Status);
    }
}
