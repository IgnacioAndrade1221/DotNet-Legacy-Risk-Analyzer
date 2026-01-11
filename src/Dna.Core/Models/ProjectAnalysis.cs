using System.Collections.Generic;

namespace Dna.Core.Models;

public class ProjectAnalysis
{
    public string ProjectName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = "Unknown";
    public bool IsSdkStyle { get; set; }

    public List<Dependency> Dependencies { get; set; } = new();
}

public class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Source { get; set; } = "NuGet";
}