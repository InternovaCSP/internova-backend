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

public class InternshipsControllerTests
{
    private readonly Mock<IInternshipRepository> _repoMock;
    private readonly Mock<ICompanyProfileRepository> _companyRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ILogger<InternshipsController>> _loggerMock;
    private readonly InternshipsController _controller;

    public InternshipsControllerTests()
    {
        _repoMock = new Mock<IInternshipRepository>();
        _companyRepoMock = new Mock<ICompanyProfileRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<InternshipsController>>();
        _controller = new InternshipsController(
            _repoMock.Object, 
            _companyRepoMock.Object, 
            _userRepoMock.Object, 
            _loggerMock.Object);
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
        var internships = new List<Internship> 
        { 
            new Internship { Id = 1, Title = "Test", Status = "Active", IsPublished = true } 
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(internships);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedInternships = Assert.IsAssignableFrom<IEnumerable<Internship>>(okResult.Value);
        Assert.Single(returnedInternships);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        // Arrange
        var internship = new Internship { Id = 1, Title = "Test" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(internship);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<Internship>(okResult.Value);
        Assert.Equal(1, returned.Id);
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Internship?)null);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        // Arrange
        SetUserContext(10, "Company");
        var dto = new CreateInternshipDto { Title = "New Job", Description = "Desc" };
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Internship>()))
                 .ReturnsAsync((Internship i) => { i.Id = 1; return i; });

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returned = Assert.IsType<Internship>(createdResult.Value);
        Assert.Equal(1, returned.Id);
        Assert.Equal(10, returned.CompanyId);
    }

    [Fact]
    public async Task Update_ExistingOwner_ReturnsOk()
    {
        // Arrange
        SetUserContext(10, "Company");
        var existing = new Internship { Id = 1, CompanyId = 10, Title = "Old" };
        var dto = new CreateInternshipDto { Title = "Updated" };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Internship>())).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<Internship>(okResult.Value);
        Assert.Equal("Updated", returned.Title);
    }

    [Fact]
    public async Task Update_ExistingNotOwner_ReturnsForbid()
    {
        // Arrange
        SetUserContext(11, "Company");
        var existing = new Internship { Id = 1, CompanyId = 10, Title = "Old" };
        var dto = new CreateInternshipDto { Title = "Updated" };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingOwner_ReturnsNoContent()
    {
        // Arrange
        SetUserContext(10, "Company");
        var existing = new Internship { Id = 1, CompanyId = 10 };
        
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
