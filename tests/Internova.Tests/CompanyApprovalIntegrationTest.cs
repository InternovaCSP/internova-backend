using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Internova.Core.DTOs;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.TestHost;

namespace Internova.Tests;

public class CompanyApprovalIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ICompanyProfileRepository> _companyRepoMock = new();
    private readonly Mock<IInternshipRepository> _internshipRepoMock = new();

    public CompanyApprovalIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove the real repositories
                var companyDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICompanyProfileRepository));
                if (companyDescriptor != null) services.Remove(companyDescriptor);

                var internshipDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IInternshipRepository));
                if (internshipDescriptor != null) services.Remove(internshipDescriptor);

                // Add mocks
                services.AddScoped(_ => _companyRepoMock.Object);
                services.AddScoped(_ => _internshipRepoMock.Object);

                // Add test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });
    }

    [Fact]
    public async Task CreateInternship_PendingCompany_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Company");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "10");

        _companyRepoMock.Setup(r => r.GetByCompanyIdAsync(10))
            .ReturnsAsync(new CompanyProfile { CompanyId = 10, Status = CompanyStatus.Pending });

        var dto = new CreateInternshipDto 
        { 
            Title = "Test Job",
            Description = "Test Desc",
            Duration = "3 months",
            Location = "Remote",
            Requirements = "Test Req",
            IsPublished = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/internships", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInternship_ActiveCompany_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Company");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "10");

        _companyRepoMock.Setup(r => r.GetByCompanyIdAsync(10))
            .ReturnsAsync(new CompanyProfile { CompanyId = 10, Status = CompanyStatus.Active });

        _internshipRepoMock.Setup(r => r.AddAsync(It.IsAny<Internship>()))
            .ReturnsAsync(new Internship { Id = 1, CompanyId = 10, Title = "Test Job" });

        var dto = new CreateInternshipDto 
        { 
            Title = "Test Job",
            Description = "Test Desc",
            Duration = "3 months",
            Location = "Remote",
            Requirements = "Test Req",
            IsPublished = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/internships", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanUpdateCompanyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "1");

        _companyRepoMock.Setup(r => r.UpdateStatusAsync(10, CompanyStatus.Active))
            .ReturnsAsync(true);

        var request = new { Status = CompanyStatus.Active };

        // Act
        var response = await client.PatchAsJsonAsync("/api/admin/companies/10/status", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("X-Test-Role", out var role) ||
            !Context.Request.Headers.TryGetValue("X-Test-UserId", out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, role!),
            new Claim("user_id", userId!),
            new Claim(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
