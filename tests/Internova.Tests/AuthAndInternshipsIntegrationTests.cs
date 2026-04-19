using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Internova.Core.Entities;
using Internova.Core.DTOs;
using Internova.Core.Interfaces;

namespace Internova.Tests;

/// <summary>
/// Integration tests for the Auth flow using WebApplicationFactory with
/// mocked repository — no real database required.
/// Tests the full HTTP pipeline: routing → middleware → controller.
/// </summary>
public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserRepository> _userRepoMock = new();

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Skip the SQL Server startup check — no real DB needed for integration tests
            builder.UseSetting("SkipDbInit", "true");

            builder.ConfigureTestServices(services =>
            {
                // Replace real IUserRepository with mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUserRepository));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped(_ => _userRepoMock.Object);

                // Inject test auth scheme so [Authorize] works
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme    = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });
    }

    // ── REGISTER ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidStudent_Returns201()
    {
        var client = _factory.CreateClient();
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var dto = new RegisterRequest
        {
            FullName = "Integration Student",
            Email    = "int.student@example.com",
            Password = "Password1!",
            Role     = "Student"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        var client   = _factory.CreateClient();
        var existing = new User { Id = 1, Email = "dup@example.com" };
        _userRepoMock.Setup(r => r.GetByEmailAsync("dup@example.com")).ReturnsAsync(existing);

        var dto = new RegisterRequest
        {
            FullName = "Dup User",
            Email    = "dup@example.com",
            Password = "Password1!",
            Role     = "Student"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_AdminRole_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var dto = new RegisterRequest
        {
            FullName = "Rogue Admin",
            Email    = "admin@example.com",
            Password = "Password1!",
            Role     = "Admin"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyBody_Returns400BadRequest()
    {
        var client   = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var client  = _factory.CreateClient();
        var hasher  = new PasswordHasher<User>();
        var user    = new User { Id = 10, Email = "login@example.com", Role = "Student" };
        user.PasswordHash = hasher.HashPassword(user, "TestPass1!");

        _userRepoMock.Setup(r => r.GetByEmailAsync("login@example.com")).ReturnsAsync(user);

        var dto = new LoginRequest { Email = "login@example.com", Password = "TestPass1!" };

        var response = await client.PostAsJsonAsync("/api/auth/login", dto);
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(body?.Token));
        Assert.Equal("Student", body?.Role);
        Assert.Equal(10, body?.UserId);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var hasher = new PasswordHasher<User>();
        var user   = new User { Id = 11, Email = "wrong@example.com", Role = "Company" };
        user.PasswordHash = hasher.HashPassword(user, "RealPass1!");

        _userRepoMock.Setup(r => r.GetByEmailAsync("wrong@example.com")).ReturnsAsync(user);

        var dto = new LoginRequest { Email = "wrong@example.com", Password = "WrongPass!" };

        var response = await client.PostAsJsonAsync("/api/auth/login", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new LoginRequest { Email = "ghost@example.com", Password = "AnyPass!" };

        var response = await client.PostAsJsonAsync("/api/auth/login", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── ME ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_AuthenticatedRequest_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role",   "Student");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "5");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_UnauthenticatedRequest_Returns401()
    {
        // Create a client that uses the real auth (no test headers → no test auth → 401)
        var client   = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/auth/me");

        // Without test headers the TestAuthHandler returns NoResult → JwtBearer → 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

/// <summary>
/// Integration tests for Internships HTTP pipeline.
/// </summary>
public class InternshipsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IInternshipRepository>       _internshipRepoMock = new();
    private readonly Mock<ICompanyProfileRepository>   _companyRepoMock    = new();
    private readonly Mock<IUserRepository>             _userRepoMock       = new();

    public InternshipsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Skip the SQL Server startup check — no real DB needed for integration tests
            builder.UseSetting("SkipDbInit", "true");

            builder.ConfigureTestServices(services =>
            {
                RemoveAndReplace<IInternshipRepository>(services, _internshipRepoMock.Object);
                RemoveAndReplace<ICompanyProfileRepository>(services, _companyRepoMock.Object);
                RemoveAndReplace<IUserRepository>(services, _userRepoMock.Object);

                services.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = "Test";
                    o.DefaultChallengeScheme    = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            });
        });
    }

    private static void RemoveAndReplace<T>(IServiceCollection services, T mock) where T : class
    {
        var d = services.SingleOrDefault(x => x.ServiceType == typeof(T));
        if (d != null) services.Remove(d);
        services.AddScoped(_ => mock);
    }

    // ── PUBLIC GET /api/internships ─────────────────────────────────────────

    [Fact]
    public async Task GetAll_PublicEndpoint_Returns200WithActiveInternships()
    {
        var client = _factory.CreateClient();
        _internshipRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Internship>
        {
            new Internship { Id = 1, Status = "Active", IsPublished = true,  Title = "Job A" },
            new Internship { Id = 2, Status = "Active", IsPublished = false, Title = "Job B" },
            new Internship { Id = 3, Status = "Pending Approval", IsPublished = true, Title = "Job C" }
        });

        var response = await client.GetAsync("/api/internships");
        var list     = await response.Content.ReadFromJsonAsync<List<Internship>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Only Active + IsPublished should be returned
        Assert.Single(list!);
        Assert.Equal("Job A", list![0].Title);
    }

    // ── GET /api/internships/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        var client = _factory.CreateClient();
        _internshipRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Internship { Id = 1, Title = "Dev Role" });

        var response = await client.GetAsync("/api/internships/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        var client = _factory.CreateClient();
        _internshipRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Internship?)null);

        var response = await client.GetAsync("/api/internships/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /api/internships (Company-only) ────────────────────────────────

    [Fact]
    public async Task CreateInternship_UnauthenticatedRequest_Returns401()
    {
        var client   = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/internships", new CreateInternshipDto { Title = "X" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateInternship_StudentRole_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role",   "Student");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "5");

        var response = await client.PostAsJsonAsync("/api/internships", new CreateInternshipDto { Title = "X" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInternship_ActiveCompany_Returns201()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role",   "Company");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "10");

        _companyRepoMock.Setup(r => r.GetByCompanyIdAsync(10))
            .ReturnsAsync(new CompanyProfile { CompanyId = 10, Status = Core.Enums.CompanyStatus.Active });
        _internshipRepoMock.Setup(r => r.AddAsync(It.IsAny<Internship>()))
            .ReturnsAsync(new Internship { Id = 55, Title = "Active Job" });

        var dto = new CreateInternshipDto
        {
            Title       = "Active Job",
            Description = "Desc",
            Duration    = "3m",
            Location    = "Remote",
        };

        var response = await client.PostAsJsonAsync("/api/internships", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateInternship_PendingCompany_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role",   "Company");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "10");

        _companyRepoMock.Setup(r => r.GetByCompanyIdAsync(10))
            .ReturnsAsync(new CompanyProfile { CompanyId = 10, Status = Core.Enums.CompanyStatus.Pending });
        // AddAsync should NOT be called for pending company — but we still need internship to be returned
        _internshipRepoMock.Setup(r => r.AddAsync(It.IsAny<Internship>()))
            .ReturnsAsync(new Internship { Id = 1 });

        var dto = new CreateInternshipDto { Title = "Pending Job", Description = "X" };

        var response = await client.PostAsJsonAsync("/api/internships", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
