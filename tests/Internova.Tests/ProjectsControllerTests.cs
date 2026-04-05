using Internova.Api.Controllers;
using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using AventStack.ExtentReports;

namespace Internova.Tests;

public class ProjectsControllerTests : IAsyncLifetime
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ILogger<ProjectsController>> _loggerMock;
    private readonly ProjectsController _controller;
    private ExtentTest? _test;

    public ProjectsControllerTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _loggerMock = new Mock<ILogger<ProjectsController>>();
        _controller = new ProjectsController(_projectRepoMock.Object, _loggerMock.Object);

        // Mock User identity
        var claims = new List<Claim> { new Claim("user_id", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        TestReportManager.Flush();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetProjects_ReturnsOk_WithFilter()
    {
        _test = TestReportManager.Instance.CreateTest("Project Controller: Get With Filter", "Tests that filtering by category returns OK.");

        try
        {
            // Arrange
            var projects = new List<ProjectResponseDto> { new ProjectResponseDto { Id = 1, Title = "Res", Category = "Research" } };
            _projectRepoMock.Setup(r => r.GetProjectsAsync("Research")).ReturnsAsync(projects);

            // Act
            var result = await _controller.GetProjects("Research");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(projects, okResult.Value);
            _test.Log(Status.Pass, "Successfully filtered projects at controller level.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task CreateProject_ReturnsCreated_WhenValid()
    {
        _test = TestReportManager.Instance.CreateTest("Project Controller: Create Success", "Tests project creation and auto-assignment of leader.");

        try
        {
            // Arrange
            var dto = new CreateProjectDto { Title = "Title", Description = "Desc", Category = "Test" };
            var project = new Project { Id = 10, LeaderId = 1, Title = dto.Title };
            
            _projectRepoMock.Setup(r => r.CreateProjectAsync(It.IsAny<Project>())).ReturnsAsync(project);
            _projectRepoMock.Setup(r => r.AddProjectParticipationAsync(10, 1, "Leader", "Accepted")).ReturnsAsync(true);

            // Act
            var result = await _controller.CreateProject(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(project, createdResult.Value);
            _test.Log(Status.Pass, "Successfully created project and assigned leader.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task RequestToJoin_ReturnsOk_WhenSuccessful()
    {
        _test = TestReportManager.Instance.CreateTest("Project Controller: Join Request", "Tests join request generation for a student.");

        try
        {
            // Arrange
            int projectId = 5;
            var project = new Project { Id = 5, LeaderId = 2 }; // Leader is someone else
            _projectRepoMock.Setup(r => r.GetProjectByIdAsync(projectId)).ReturnsAsync(project);
            _projectRepoMock.Setup(r => r.AddProjectParticipationAsync(projectId, 1, "Member", "Pending")).ReturnsAsync(true);

            // Act
            var result = await _controller.RequestToJoin(projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _test.Log(Status.Pass, "Successfully generated join request.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
