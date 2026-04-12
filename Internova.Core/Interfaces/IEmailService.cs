using System.Threading.Tasks;

namespace Internova.Core.Interfaces;

/// <summary>
/// Interface for email notification services.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Email body content.</param>
    /// <returns>True if the email was successfully sent.</returns>
    Task<bool> SendEmailAsync(string to, string subject, string body);
}
