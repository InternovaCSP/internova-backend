using System.Threading.Tasks;

namespace Internova.Core.Interfaces;

/// <summary>
/// Interface for meeting link generation (External API).
/// </summary>
public interface IMeetingService
{
    /// <summary>
    /// Generates a unique meeting link (e.g., Google Meet).
    /// </summary>
    /// <param name="summary">The title or description for the meeting.</param>
    /// <param name="startTime">The start time of the meeting.</param>
    /// <param name="durationMinutes">How long the meeting will last.</param>
    /// <returns>A string containing the full meeting link.</returns>
    Task<string> GenerateMeetingLinkAsync(string summary, DateTime startTime, int durationMinutes = 60);
}
