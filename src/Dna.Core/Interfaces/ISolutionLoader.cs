using Dna.Core.Models;

namespace Dna.Core.Interfaces;

public interface ISolutionLoader
{
    IEnumerable<ProjectItem> LoadProjects(string solutionPath);
}

