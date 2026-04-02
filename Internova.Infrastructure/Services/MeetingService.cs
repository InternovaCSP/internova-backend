using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Internova.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Services;

/// <summary>
/// Service to generate Daily.co meeting rooms.
/// </summary>
public class MeetingService : IMeetingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MeetingService> _logger;
    private readonly string _apiKey;

    public MeetingService(IConfiguration configuration, ILogger<MeetingService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        
        // Get API key from config (User should add this to appsettings.json)
        _apiKey = _configuration["DailyCo:ApiKey"] ?? "";
    }

    public async Task<string> GenerateMeetingLinkAsync(string summary, DateTime startTime, int durationMinutes = 60)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Daily.co API Key missing. Returning simulation link.");
                return $"https://internova.daily.co/mock-{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.daily.co/v1/rooms");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            var roomOptions = new
            {
                name = $"room-{Guid.NewGuid().ToString().Substring(0, 8)}",
                privacy = "public",
                properties = new
                {
                    enable_chat = true,
                    start_audio_off = true,
                    start_video_off = false,
                    exp = ((DateTimeOffset)startTime.AddMinutes(durationMinutes + 30)).ToUnixTimeSeconds()
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(roomOptions), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                return doc.RootElement.GetProperty("url").GetString() ?? "https://daily.co/";
            }
            
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Daily.co API Error: {Error}", error);
            return $"https://internova.daily.co/error-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Daily.co room.");
            return $"https://internova.daily.co/fallback-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}
