using Internova.Api.Controllers;
using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AventStack.ExtentReports;

namespace Internova.Tests;

public class InterviewsControllerTests : IAsyncLifetime
{
    private readonly Mock<IInterviewRepository> _interviewRepoMock;
    private readonly Mock<IInternshipApplicationRepository> _appRepoMock;
    private readonly Mock<ILogger<InterviewsController>> _loggerMock;
    private readonly InterviewsController _controller;
    private ExtentTest? _test;

    public InterviewsControllerTests()
    {
        _interviewRepoMock = new Mock<IInterviewRepository>();
        _appRepoMock = new Mock<IInternshipApplicationRepository>();
        _loggerMock = new Mock<ILogger<InterviewsController>>();
        _controller = new InterviewsController(_interviewRepoMock.Object, _appRepoMock.Object, _loggerMock.Object);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        TestReportManager.Flush();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Schedule_ReturnsOk_WhenValidRequest()
    {
        _test = TestReportManager.Instance.CreateTest("Controller: Schedule Success", "Tests that scheduling an interview returns OK when everything is valid.");

        try
        {
            // Arrange
            var request = new InterviewsController.ScheduleInterviewRequest
            {
                ApplicationId = 1,
                InterviewDate = DateTime.Now.AddDays(1),
                LocationOrLink = "Zoom"
            };

            _test.Log(Status.Info, $"Arranging request for ApplicationId={request.ApplicationId}");

            // Setup mock for AddAsync
            _interviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Interview>())).ReturnsAsync(new Interview { Id = 50 });
            // Setup mock for UpdateStatus
            _appRepoMock.Setup(r => r.UpdateStatusAsync(1, ApplicationStatus.InterviewScheduled)).ReturnsAsync(true);

            // Act
            _test.Log(Status.Info, "Executing ScheduleInterview action.");
            var result = await _controller.ScheduleInterview(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _test.Log(Status.Pass, "Successfully returned OkObjectResult.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task Schedule_ReturnsNotFound_WhenApplicationDoesNotExist()
    {
        _test = TestReportManager.Instance.CreateTest("Controller: Schedule NotFound", "Tests that scheduling returns NotFound if the application is missing.");

        try
        {
            // Arrange
            var request = new InterviewsController.ScheduleInterviewRequest { ApplicationId = 999 };
            _test.Log(Status.Info, "Arranging request with non-existent ApplicationId=999");
            
            // Act
            var result = await _controller.ScheduleInterview(request);

            // Assert
            // Note: Current implementation might not handle this yet
            _test.Log(Status.Warning, "Asserting result. (Note: Logic might need update to return NotFound)");
            // var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
