using Dna.Core.Models;
using System.Collections.Generic;

namespace Dna.Core.Interfaces;

public interface IReportGenerator
{
    string Generate(string solutionName, List<ProjectAnalysis> projects, Dictionary<string, List<DetectedRisk>> risks);
}