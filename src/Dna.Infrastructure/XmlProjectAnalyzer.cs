using Dna.Core.Interfaces;
using Dna.Core.Models;
using Microsoft.Build.Construction;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Dna.Infrastructure;

public class XmlProjectAnalyzer : IProjectAnalyzer
{
    public ProjectAnalysis Analyze(string projectPath)
    {
        var result = new ProjectAnalysis
        {
            Path = projectPath,
            ProjectName = Path.GetFileNameWithoutExtension(projectPath)
        };

        try
        {
            var projectRoot = ProjectRootElement.Open(projectPath);

            result.IsSdkStyle = !string.IsNullOrEmpty(projectRoot.Sdk);

            result.TargetFramework = FindFramework(projectRoot);

            foreach (var item in projectRoot.Items.Where(i => i.ItemType == "PackageReference"))
            {
                result.Dependencies.Add(new Dependency
                {
                    Name = item.Include,
                    Version = item.Metadata.FirstOrDefault(m => m.Name == "Version")?.Value ?? "Unknown",
                    Source = "NuGet (csproj)"
                });
            }

            string projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;
            string packagesConfigPath = Path.Combine(projectDir, "packages.config");

            if (File.Exists(packagesConfigPath))
            {
                try
                {
                    var doc = XDocument.Load(packagesConfigPath);
                    foreach (var pkg in doc.Descendants("package"))
                    {
                        result.Dependencies.Add(new Dependency
                        {
                            Name = pkg.Attribute("id")?.Value ?? "Unknown",
                            Version = pkg.Attribute("version")?.Value ?? "Unknown",
                            Source = "NuGet (packages.config)"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error reading packages.config: {ex.Message}");
                }
            }

            foreach (var item in projectRoot.Items.Where(i => i.ItemType == "Reference"))
            {
                var name = item.Include.Split(',')[0];
                if (!name.StartsWith("System") && !name.StartsWith("Microsoft.CSharp"))
                {
                    result.Dependencies.Add(new Dependency
                    {
                        Name = name,
                        Version = "Assembly Reference",
                        Source = "Assembly"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"⚠️ Error analyzing {projectPath}: {ex.Message}");
            result.TargetFramework = "Error";
        }

        return result;
    }

    private string FindFramework(ProjectRootElement root)
    {
        var props = root.Properties;

        var tf = props.FirstOrDefault(p => p.Name == "TargetFramework")?.Value;
        if (!string.IsNullOrEmpty(tf)) return tf;

        var tfVersion = props.FirstOrDefault(p => p.Name == "TargetFrameworkVersion")?.Value;
        if (!string.IsNullOrEmpty(tfVersion)) return tfVersion;

        return "Unknown";
    }
}