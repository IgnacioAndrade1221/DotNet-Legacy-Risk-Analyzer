namespace Dna.Core.Models;

public class AnalysisSettings
{
    public double HoursHigh { get; set; }
    public double HoursMedium { get; set; }
    public double HoursLow { get; set; }
    public double HourlyRate { get; set; }
    public string CurrencySymbol { get; set; } = "$";
}