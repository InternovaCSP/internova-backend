using Internova.Core.Interfaces;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using AventStack.ExtentReports;

namespace Internova.Tests;

public class MeetingServiceTests : IAsyncLifetime
{
    private ExtentTest? _test;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        TestReportManager.Flush();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GenerateMeetingLink_ReturnsValidUrl()
    {
        _test = TestReportManager.Instance.CreateTest("MeetingService: Generate Link", "Tests that the meeting service returns a valid URL for Teams/Zoom.");

        try
        {
            // Note: Since MeetingService is not yet implemented, we expect this to fail 
            // once we try to instantiate it. For now, we'll try to use the interface.
            
            // This test is designed to fail (TDD)
            _test.Log(Status.Info, "Attempting to test MeetingService implementation.");
            
            // Once implemented, it would be:
            // IMeetingService service = new MeetingService();
            // var link = await service.GenerateMeetingLinkAsync("Test Seminar", DateTime.Now.AddDays(1));
            // Assert.StartsWith("http", link);

            throw new NotImplementedException("MeetingService implementation not found.");
        }
        catch (Exception ex)
        {
            _test.Log(Status.Fail, ex.Message);
            throw;
        }
    }
}
