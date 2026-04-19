using Internova.Api.Controllers;
using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Internova.Tests;

/// <summary>
/// Unit tests for <see cref="CompetitionsController"/>.
/// Coverage: GetAll, Create (Admin-only), Update, Delete.
/// </summary>
public class CompetitionsControllerTests
{
    private readonly Mock<ICompetitionRepository> _repoMock;
    private readonly Mock<ILogger<CompetitionsController>> _loggerMock;
    private readonly CompetitionsController _controller;

    public CompetitionsControllerTests()
    {
        _repoMock    = new Mock<ICompetitionRepository>();
        _loggerMock  = new Mock<ILogger<CompetitionsController>>();
        _controller  = new CompetitionsController(_repoMock.Object, _loggerMock.Object);
    }

    // Helper builder for a default competition
    private static Competition MakeCompetition(int id = 1) => new Competition
    {
        Id              = id,
        OrganizerId     = 1,
        Title           = $"Hackathon #{id}",
        Description     = "A great competition",
        Category        = "Tech",
        EligibilityCriteria = "All students",
        StartDate       = DateTime.Today.AddDays(7),
        EndDate         = DateTime.Today.AddDays(14),
        RegistrationLink = "https://register.example.com",
        IsApproved      = true
    };

    private static CreateCompetitionDto MakeCreateDto() => new CreateCompetitionDto
    {
        OrganizerId     = 1,
        Title           = "New Hackathon",
        Description     = "Open to all.",
        Category        = "Tech",
        EligibilityCriteria = "Students Only",
        StartDate       = DateTime.Today.AddDays(7),
        EndDate         = DateTime.Today.AddDays(14),
        RegistrationLink = "https://register.example.com"
    };

    // ── GET ALL ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var competitions = new List<Competition> { MakeCompetition(1), MakeCompetition(2) };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(competitions);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<CompetitionDto>>(ok.Value);
        Assert.Equal(2, dtos.Count());
    }

    [Fact]
    public async Task GetAll_EmptyRepository_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Competition>());

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<CompetitionDto>>(ok.Value));
    }

    [Fact]
    public async Task GetAll_MapsFieldsCorrectly()
    {
        var comp = MakeCompetition(42);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Competition> { comp });

        var result = await _controller.GetAll();

        var ok  = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsAssignableFrom<IEnumerable<CompetitionDto>>(ok.Value).Single();

        Assert.Equal(42,          dto.Id);
        Assert.Equal("Hackathon #42", dto.Title);
        Assert.Equal("Tech",      dto.Category);
        Assert.True(dto.IsApproved);
    }

    [Fact]
    public async Task GetAll_RepositoryThrows_Returns500()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetAll();

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }

    // ── CREATE ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var added = MakeCompetition(5);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Competition>())).ReturnsAsync(added);

        var result = await _controller.Create(MakeCreateDto());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto     = Assert.IsType<CompetitionDto>(created.Value);
        Assert.Equal(5, dto.Id);
    }

    [Fact]
    public async Task Create_DefaultsIsApprovedToFalse()
    {
        Competition? captured = null;
        var added = new Competition { Id = 1 };
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Competition>()))
            .Callback<Competition>(c => captured = c)
            .ReturnsAsync(added);

        await _controller.Create(MakeCreateDto());

        Assert.NotNull(captured);
        Assert.False(captured!.IsApproved); // should default to unapproved
    }

    [Fact]
    public async Task Create_RepositoryThrows_Returns500()
    {
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Competition>())).ThrowsAsync(new Exception("DB fail"));

        var result = await _controller.Create(MakeCreateDto());

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }

    // ── UPDATE ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingCompetition_ReturnsNoContent()
    {
        var existing = MakeCompetition(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(true);

        var dto = new UpdateCompetitionDto
        {
            Title       = "Updated Title",
            Category    = "Design",
            IsApproved  = false,
            StartDate   = DateTime.Today,
            EndDate     = DateTime.Today.AddDays(3)
        };

        var result = await _controller.Update(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_NonExistingCompetition_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Competition?)null);

        var result = await _controller.Update(99, new UpdateCompetitionDto { Title = "X" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_RepositoryReturnsFalse_Returns500()
    {
        var existing = MakeCompetition(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(false);

        var result = await _controller.Update(1, new UpdateCompetitionDto { Title = "X" });

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task Update_AppliesAllFields()
    {
        var existing = MakeCompetition(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(true);

        var dto = new UpdateCompetitionDto
        {
            Title               = "New Title",
            Description         = "New Desc",
            Category            = "AI",
            EligibilityCriteria = "Grad students",
            StartDate           = DateTime.Today,
            EndDate             = DateTime.Today.AddDays(5),
            RegistrationLink    = "https://new.com",
            IsApproved          = true
        };

        await _controller.Update(1, dto);

        Assert.Equal("New Title",    existing.Title);
        Assert.Equal("New Desc",     existing.Description);
        Assert.Equal("AI",           existing.Category);
        Assert.Equal("Grad students",existing.EligibilityCriteria);
        Assert.Equal("https://new.com", existing.RegistrationLink);
        Assert.True(existing.IsApproved);
    }

    // ── DELETE ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingCompetition_ReturnsNoContent()
    {
        var existing = MakeCompetition(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingCompetition_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Competition?)null);

        var result = await _controller.Delete(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_RepositoryReturnsFalse_Returns500()
    {
        var existing = MakeCompetition(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(false);

        var result = await _controller.Delete(1);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
