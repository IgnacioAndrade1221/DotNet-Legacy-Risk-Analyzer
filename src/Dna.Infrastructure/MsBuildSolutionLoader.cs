using Dna.Core.Interfaces;
using Dna.Core.Models;
using Microsoft.Build.Construction;

namespace Dna.Infrastructure;

public class MsBuildSolutionLoader : ISolutionLoader
{
    public IEnumerable<ProjectItem> LoadProjects(string solutionPath)
    {
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");

        string absolutePath = Path.GetFullPath(solutionPath);

        var solutionFile = SolutionFile.Parse(absolutePath);

        var projects = new List<ProjectItem>();

        foreach (var proj in solutionFile.ProjectsInOrder)
        {
            if (proj.ProjectType == SolutionProjectType.SolutionFolder)
                continue;

            projects.Add(new ProjectItem
            {
                Name = proj.ProjectName,
                AbsolutePath = proj.AbsolutePath,
                TypeGuid = proj.ProjectType.ToString()
            });
        }

        return projects;
    }
}
