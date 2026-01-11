using Dna.Core.Interfaces;
using Dna.Core.Models;
using Dna.Infrastructure;
using Dna.Engine.Rules;
using Dna.Engine.Reporting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🇯🇵🇨🇦 DotNet Legacy Risk Analyzer - Starting...");

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfiguration config = builder.Build();

        string targetSolution = config["TargetSolution"] ?? (args.Length > 0 ? args[0] : "");

        try { BuildInitializer.Initialize(); }
        catch (Exception ex) { Console.Error.WriteLine($"❌ CRITICAL: {ex.Message}"); return; }

        if (string.IsNullOrWhiteSpace(targetSolution) || !File.Exists(targetSolution))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ERROR: No solution file configured.");
            Console.WriteLine("👉 Please edit 'appsettings.json' and set 'TargetSolution' path.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"🔍 Scanning Solution from Config: {Path.GetFileName(targetSolution)}");


        ISolutionLoader loader = new MsBuildSolutionLoader();
        var projectsRaw = loader.LoadProjects(targetSolution).ToList();

        if (!projectsRaw.Any())
        {
            Console.WriteLine("⚠️  No projects found.");
            return;
        }

        Console.WriteLine($"✅ Found {projectsRaw.Count} projects. Starting Deep Analysis...");
        Console.WriteLine("---------------------------------------------------");

        IProjectAnalyzer analyzer = new XmlProjectAnalyzer();
        IRiskEvaluator evaluator = new StandardRiskEvaluator();
        IReportGenerator reportGen = new HtmlReportGenerator();

        var analyzedProjects = new List<ProjectAnalysis>();
        var risksMap = new Dictionary<string, List<DetectedRisk>>();

        foreach (var projItem in projectsRaw)
        {
            var analysis = analyzer.Analyze(projItem.AbsolutePath);
            analyzedProjects.Add(analysis);

            var risks = evaluator.Evaluate(analysis);
            risksMap[projItem.AbsolutePath] = risks;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📂 PROJECT: {analysis.ProjectName}");
            Console.ResetColor();
            Console.WriteLine($"   Target: {analysis.TargetFramework} | Style: {(analysis.IsSdkStyle ? "SDK" : "Legacy")} | NuGets: {analysis.Dependencies.Count}");

            if (risks.Any())
            {
                foreach (var risk in risks)
                {
                    switch (risk.Level)
                    {
                        case Severity.High:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("   [HIGH] ");
                            break;
                        case Severity.Medium:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("   [MED]  ");
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("   [LOW]  ");
                            break;
                    }
                    Console.ResetColor();
                    Console.WriteLine($"{risk.Description}");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine("📝 Generating Report...");

        string reportHtml = reportGen.Generate(Path.GetFileName(targetSolution), analyzedProjects, risksMap);
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "RiskReport.html");
        File.WriteAllText(reportPath, reportHtml);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ REPORT GENERATED SUCCESSFULLY!");
        Console.WriteLine($"👉 File: {reportPath}");
        var p = new System.Diagnostics.Process();
        p.StartInfo = new System.Diagnostics.ProcessStartInfo(reportPath)
        {
            UseShellExecute = true
        };
        p.Start();
        Console.ResetColor();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}