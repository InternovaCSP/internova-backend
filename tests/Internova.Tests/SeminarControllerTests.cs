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

public class SeminarControllerTests : IAsyncLifetime
{
    private readonly Mock<ISeminarRepository> _seminarRepoMock;
    private readonly Mock<ILogger<SeminarController>> _loggerMock;
    private readonly SeminarController _controller;
    private ExtentTest? _test;

    public SeminarControllerTests()
    {
        _seminarRepoMock = new Mock<ISeminarRepository>();
        _loggerMock = new Mock<ILogger<SeminarController>>();
        _controller = new SeminarController(_seminarRepoMock.Object, _loggerMock.Object);

        // Mock User identity for student 123
        var claims = new List<Claim> { new Claim("user_id", "123") };
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
    public async Task Create_ReturnsCreatedAtAction_WhenValid()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar Controller: Create Success", "Verifies that creating a seminar request returns 201 Created.");

        try
        {
            // Arrange
            var dto = new SeminarRequestCreateDto { Topic = "Topic", Description = "Desc" };
            _seminarRepoMock.Setup(r => r.CreateAsync(It.IsAny<SeminarRequest>())).ReturnsAsync(1);

            // Act
            _test.Log(Status.Info, "Calling Create action.");
            var result = await _controller.Create(dto);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(1, createdAtResult.RouteValues?["id"]);
            _test.Log(Status.Pass, "Successfully created seminar request at controller level.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task Vote_ReturnsOk_WhenValid()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar Controller: Vote Success", "Verifies that voting returns OK and updates status if needed.");

        try
        {
            // Arrange
            int seminarId = 1;
            var seminar = new SeminarRequest { Id = 1, StudentId = 456, Topic = "Topic", Threshold = 5, Status = "Pending" };
            
            _seminarRepoMock.Setup(r => r.GetByIdAsync(seminarId)).ReturnsAsync(seminar);
            _seminarRepoMock.Setup(r => r.HasStudentVotedAsync(seminarId, 123)).ReturnsAsync(false);
            _seminarRepoMock.Setup(r => r.VoteAsync(seminarId, 123)).ReturnsAsync(true);
            _seminarRepoMock.Setup(r => r.GetVoteCountAsync(seminarId)).ReturnsAsync(1);

            // Act
            _test.Log(Status.Info, "Calling Vote action.");
            var result = await _controller.Vote(seminarId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _test.Log(Status.Pass, "Successfully voted on seminar request.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task Vote_ReturnsBadRequest_WhenAlreadyVoted()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar Controller: Already Voted", "Verifies that double-voting returns BadRequest.");

        try
        {
            // Arrange
            int seminarId = 1;
            var seminar = new SeminarRequest { Id = 1, Topic = "Topic" };
            
            _seminarRepoMock.Setup(r => r.GetByIdAsync(seminarId)).ReturnsAsync(seminar);
            _seminarRepoMock.Setup(r => r.HasStudentVotedAsync(seminarId, 123)).ReturnsAsync(true);

            // Act
            _test.Log(Status.Info, "Attempting to vote a second time.");
            var result = await _controller.Vote(seminarId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _test.Log(Status.Pass, "Successfully blocked duplicate vote.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task Vote_ApprovesSeminar_WhenThresholdReached()
    {
        _test = TestReportManager.Instance.CreateTest("Seminar Controller: Threshold Approval", "Verifies that reaching threshold updates the status to Approved.");

        try
        {
            // Arrange
            int seminarId = 1;
            var seminar = new SeminarRequest { Id = 1, Topic = "Topic", Threshold = 1, Status = "Pending" };
            
            _seminarRepoMock.Setup(r => r.GetByIdAsync(seminarId)).ReturnsAsync(seminar);
            _seminarRepoMock.Setup(r => r.HasStudentVotedAsync(seminarId, 123)).ReturnsAsync(false);
            _seminarRepoMock.Setup(r => r.VoteAsync(seminarId, 123)).ReturnsAsync(true);
            _seminarRepoMock.Setup(r => r.GetVoteCountAsync(seminarId)).ReturnsAsync(1);
            _seminarRepoMock.Setup(r => r.UpdateStatusAsync(seminarId, "Approved")).ReturnsAsync(true);

            // Act
            _test.Log(Status.Info, "Casting vote to reach threshold.");
            var result = await _controller.Vote(seminarId);

            // Assert
            _test.Log(Status.Info, "Verifying UpdateStatusAsync('Approved') was called.");
            _seminarRepoMock.Verify(r => r.UpdateStatusAsync(seminarId, "Approved"), Times.Once());
            _test.Log(Status.Pass, "Seminar successfully approved upon reaching threshold.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
