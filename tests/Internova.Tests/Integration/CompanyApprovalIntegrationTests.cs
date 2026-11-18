using System.Net;
using System.Net.Http.Json;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Internova.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Internova.Tests.Integration;

public class CompanyApprovalIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ICompanyProfileRepository> _companyRepoMock = new();
    private readonly Mock<IInternshipRepository> _internshipRepoMock = new();

    public CompanyApprovalIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "IntegrationTestSigningKey_AtLeast32CharsLong!");
            builder.UseSetting("Jwt:Issuer", "Internova");
            builder.UseSetting("Jwt:Audience", "InternovaUsers");

            builder.ConfigureTestServices(services =>
            {
                var companyDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICompanyProfileRepository));
                if (companyDescriptor is not null) services.Remove(companyDescriptor);

                var internshipDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IInternshipRepository));
                if (internshipDescriptor is not null) services.Remove(internshipDescriptor);

                services.AddScoped(_ => _companyRepoMock.Object);
                services.AddScoped(_ => _internshipRepoMock.Object);

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

        var response = await client.PostAsJsonAsync("/api/internships", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInternship_ActiveCompany_ReturnsCreated()
    {
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

        var response = await client.PostAsJsonAsync("/api/internships", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanUpdateCompanyStatus()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "1");

        _companyRepoMock.Setup(r => r.UpdateStatusAsync(10, CompanyStatus.Active))
            .ReturnsAsync(true);

        var response = await client.PatchAsJsonAsync(
            "/api/admin/companies/10/status",
            new { Status = CompanyStatus.Active });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
