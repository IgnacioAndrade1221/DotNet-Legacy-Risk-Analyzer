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
            //NUEVO PARA LEER EL WEB.CONFIG, APP.CONFIG O APPSETTINGS.JSON
            string[] targetConfigFiles = { "web.config", "app.config", "appsettings.json" };
            foreach (var configName in targetConfigFiles)
            {
                string fullConfigPath = Path.Combine(projectDir, configName);
                if (File.Exists(fullConfigPath))
                {
                    result.ConfigurationFiles[configName] = File.ReadAllText(fullConfigPath);
                }
            }
            //NUEVO -- LEE CODIGO FUENTE PARA DETECTAR USOS DE PATRONES LEGACY PESADOS .CS
            if (Directory.Exists(projectDir))
            {
                // Buscamos todos los archivos .cs de forma recursiva
                var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
                foreach (var file in csFiles)
                {
                    // Filtro
                    if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                        file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) ||
                        file.EndsWith(".g.cs"))
                        continue;

                    var lines = File.ReadLines(file);
                    int lineNum = 1;
                    foreach (var line in lines)
                    {
                        // Detectamos patrones de arquitectura legacy pesada
                        if (line.Contains("DataTable") || line.Contains("DataSet") || line.Contains("SqlDataAdapter"))
                        {
                            string fileName = Path.GetFileName(file);
                            if (!result.SourceCodeFindings.ContainsKey(fileName))
                                result.SourceCodeFindings[fileName] = new List<string>();

                            result.SourceCodeFindings[fileName].Add($"L{lineNum}: {line.Trim()}");
                        }
                        lineNum++;
                    }
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