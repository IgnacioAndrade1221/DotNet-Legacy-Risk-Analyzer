namespace Dna.Core.Models;

public class RiskReport
{
    public string ProjectName { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public List<DetectedRisk> Risks { get; set; } = new();
}

public class  DetectedRisk
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Level { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}
