namespace Internova.Core.DTOs;

public class AdminStatsDto
{
    public int Placed { get; set; }
    public int Seeking { get; set; }
    public List<IndustryStatsDto> Industries { get; set; } = new();
}

public class IndustryStatsDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
