using Internova.Api.Controllers;
using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Internova.Tests;

public class CompetitionsControllerTests
{
    private readonly Mock<ICompetitionRepository> _repoMock;
    private readonly Mock<ILogger<CompetitionsController>> _loggerMock;
    private readonly CompetitionsController _controller;

    public CompetitionsControllerTests()
    {
        _repoMock = new Mock<ICompetitionRepository>();
        _loggerMock = new Mock<ILogger<CompetitionsController>>();
        _controller = new CompetitionsController(_repoMock.Object, _loggerMock.Object);
    }

    private void SetUserContext(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        // Arrange
        var competitions = new List<Competition> 
        { 
            new Competition { Id = 1, Title = "Test Comp", OrganizerId = 10 } 
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(competitions);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<CompetitionDto>>(okResult.Value);
        Assert.Single(dtos);
    }

    [Fact]
    public async Task GetAll_Exception_ReturnsInternalServerError()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new System.Exception("Database error"));

        // Act
        var result = await _controller.GetAll();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("Internal server error", objectResult.Value);
    }

    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        // Arrange
        SetUserContext(10, "Admin");
        var createDto = new CreateCompetitionDto { Title = "New Comp", OrganizerId = 10 };
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Competition>()))
                 .ReturnsAsync((Competition c) => { c.Id = 1; return c; });

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<CompetitionDto>(createdResult.Value);
        Assert.Equal(1, returned.Id);
        Assert.Equal("New Comp", returned.Title);
    }

    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Title", "Required");
        var createDto = new CreateCompetitionDto();

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_ExistingId_ReturnsNoContent()
    {
        // Arrange
        SetUserContext(10, "Admin");
        var existing = new Competition { Id = 1, Title = "Old Title" };
        var updateDto = new UpdateCompetitionDto { Title = "New Title" };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Competition>())).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_MissingId_ReturnsNotFound()
    {
        // Arrange
        SetUserContext(10, "Admin");
        var updateDto = new UpdateCompetitionDto { Title = "New Title" };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Competition?)null);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Competition with ID 1 not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task Update_FailedUpdate_ReturnsInternalServerError()
    {
        // Arrange
        SetUserContext(10, "Admin");
        var existing = new Competition { Id = 1, Title = "Old Title" };
        var updateDto = new UpdateCompetitionDto { Title = "New Title" };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Competition>())).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("Failed to update competition.", objectResult.Value);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        // Arrange
        SetUserContext(10, "Admin");
        var existing = new Competition { Id = 1, Title = "Title" };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingId_ReturnsNotFound()
    {
        // Arrange
        SetUserContext(10, "Admin");
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Competition?)null);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Competition with ID 1 not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task Delete_Exception_ReturnsInternalServerError()
    {
        // Arrange
        SetUserContext(10, "Admin");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ThrowsAsync(new System.Exception("DB Error"));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("Internal server error", objectResult.Value);
    }
}
