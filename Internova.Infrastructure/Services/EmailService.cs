using System.Threading.Tasks;
using Internova.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Services;

/// <summary>
/// Mock implementation of the Email Service for development.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        // Mock sending email - Log only for verification
        _logger.LogInformation("MOCK EMAIL SENT to: {Email}, Subject: {Subject}, Body: {Body}", to, subject, body);
        return Task.FromResult(true);
    }
}
