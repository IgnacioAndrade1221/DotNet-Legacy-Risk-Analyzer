using Dna.Core.Models;

namespace Dna.Core.Interfaces;

public interface IProjectAnalyzer
{
    ProjectAnalysis Analyze(string projectPath);
}