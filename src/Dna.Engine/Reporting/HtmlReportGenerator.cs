using Dna.Core.Interfaces;
using Dna.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dna.Engine.Reporting;

public class HtmlReportGenerator : IReportGenerator
{
    public string Generate(string solutionName, List<ProjectAnalysis> projects, Dictionary<string, List<DetectedRisk>> risksMap)
    {
        var allRisks = risksMap.SelectMany(x => x.Value).ToList();

        double totalHours = allRisks.Sum(r => r.Level switch
        {
            Severity.High or Severity.Critical => 8.0,
            Severity.Medium => 4.0,
            _ => 2.0
        });

        double estimatedCost = totalHours * 60;

        int penalty = allRisks.Sum(r => r.Level switch
        {
            Severity.High or Severity.Critical => 15,
            Severity.Medium => 5,
            _ => 1
        });
        int healthScore = Math.Max(0, 100 - penalty);

        string scoreColor = healthScore switch
        {
            > 80 => "#27ae60",
            > 50 => "#f1c40f",
            _ => "#e74c3c"
        };

        var sb = new StringBuilder();

        sb.AppendLine($@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Risk Assessment: {solutionName}</title>
    <style>
        body {{ font-family: 'Segoe UI', Roboto, Helvetica, sans-serif; background-color: #f0f2f5; color: #333; margin: 0; padding: 40px; }}
        .container {{ max-width: 1100px; margin: 0 auto; background: white; padding: 50px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); border-radius: 12px; }}
        
        /* Header & Dashboard */
        .header {{ border-bottom: 2px solid #ecf0f1; padding-bottom: 20px; margin-bottom: 30px; display: flex; justify-content: space-between; align-items: center; }}
        h1 {{ color: #2c3e50; margin: 0; font-size: 24px; }}
        .date {{ color: #7f8c8d; font-size: 14px; }}

        /* KPI Cards */
        .dashboard {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 40px; }}
        .kpi-card {{ background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: center; border: 1px solid #e9ecef; }}
        .kpi-value {{ display: block; font-size: 32px; font-weight: bold; color: #2c3e50; margin-bottom: 5px; }}
        .kpi-label {{ color: #7f8c8d; font-size: 13px; text-transform: uppercase; letter-spacing: 1px; }}
        
        /* Health Bar Visual */
        .health-section {{ margin-bottom: 40px; }}
        .health-bar-bg {{ background: #ecf0f1; height: 30px; border-radius: 15px; overflow: hidden; }}
        .health-bar-fill {{ height: 100%; text-align: center; color: white; line-height: 30px; font-weight: bold; width: 0; transition: width 1s; }}
        
        /* Projects List */
        .project-card {{ background: white; border: 1px solid #e0e0e0; border-radius: 8px; margin-bottom: 25px; overflow: hidden; transition: transform 0.2s; }}
        .project-card:hover {{ box-shadow: 0 5px 15px rgba(0,0,0,0.05); transform: translateY(-2px); }}
        
        .project-header {{ background: #34495e; color: white; padding: 15px 20px; display: flex; justify-content: space-between; align-items: center; }}
        .project-title {{ font-size: 18px; font-weight: 600; }}
        .badge {{ background: rgba(255,255,255,0.2); padding: 4px 10px; border-radius: 4px; font-size: 12px; margin-left: 10px; }}
        
        .project-body {{ padding: 20px; }}
        
        /* Risk Items */
        .risk-item {{ display: flex; align-items: flex-start; margin-bottom: 12px; padding: 12px; border-radius: 6px; }}
        .risk-high {{ background-color: #fdecea; border-left: 5px solid #e74c3c; }}
        .risk-med  {{ background-color: #fef9e7; border-left: 5px solid #f1c40f; }}
        .risk-low  {{ background-color: #e8f8f5; border-left: 5px solid #27ae60; }}
        
        .risk-icon {{ margin-right: 15px; font-size: 18px; }}
        .risk-content strong {{ display: block; margin-bottom: 4px; color: #2c3e50; }}
        .risk-content p {{ margin: 0; color: #555; font-size: 14px; }}
        .recommendation {{ display: block; margin-top: 6px; font-size: 13px; color: #666; font-style: italic; }}

        .footer {{ text-align: center; margin-top: 60px; color: #bdc3c7; font-size: 12px; border-top: 1px solid #eee; padding-top: 20px; }}
        .clean-state {{ color: #27ae60; padding: 20px; text-align: center; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div>
                <h1>🛡️ Legacy Audit Report</h1>
                <div class='date'>Target: {solutionName} | Generated: {DateTime.Now:yyyy-MM-dd HH:mm}</div>
            </div>
            <div style='text-align: right;'>
                <span style='font-size: 40px; font-weight: bold; color: {scoreColor};'>{healthScore}%</span>
                <div style='font-size: 12px; color: #95a5a6;'>HEALTH SCORE</div>
            </div>
        </div>

        <div class='dashboard'>
            <div class='kpi-card'>
                <span class='kpi-value'>{projects.Count}</span>
                <span class='kpi-label'>Projects</span>
            </div>
            <div class='kpi-card'>
                <span class='kpi-value' style='color: #e74c3c'>{allRisks.Count}</span>
                <span class='kpi-label'>Risks Detected</span>
            </div>
            <div class='kpi-card'>
                <span class='kpi-value'>{totalHours}h</span>
                <span class='kpi-label'>Est. Refactoring Time</span>
            </div>
            <div class='kpi-card'>
                <span class='kpi-value'>${estimatedCost:N0}</span>
                <span class='kpi-label'>Est. Tech Debt Cost</span>
            </div>
        </div>

        <div class='health-section'>
            <div style='display:flex; justify-content:space-between; margin-bottom:5px; font-size:12px; font-weight:bold; color:#7f8c8d;'>
                <span>SYSTEM HEALTH</span>
                <span>{healthScore}/100</span>
            </div>
            <div class='health-bar-bg'>
                <div class='health-bar-fill' style='width: {healthScore}%; background-color: {scoreColor};'>
                </div>
            </div>
        </div>

        {GenerateProjectDetails(projects, risksMap)}

        <div class='footer'>
            Generated by <strong>DotNet Legacy Risk Analyzer (DORA)</strong> • Offline Analysis • No Data Uploaded
        </div>
    </div>
</body>
</html>");

        return sb.ToString();
    }

    private string GenerateProjectDetails(List<ProjectAnalysis> projects, Dictionary<string, List<DetectedRisk>> risksMap)
    {
        var sb = new StringBuilder();

        foreach (var proj in projects)
        {
            var projectRisks = risksMap.ContainsKey(proj.Path) ? risksMap[proj.Path] : new List<DetectedRisk>();

            sb.AppendLine("<div class='project-card'>");

            sb.AppendLine("<div class='project-header'>");
            sb.AppendLine($"<div class='project-title'>{proj.ProjectName}</div>");
            sb.AppendLine("<div>");
            sb.AppendLine($"<span class='badge' style='background:{(proj.TargetFramework.StartsWith("v4") ? "#e74c3c" : "#27ae60")}'>{proj.TargetFramework}</span>");
            sb.AppendLine($"<span class='badge'>{(proj.IsSdkStyle ? "SDK Style" : "Legacy Format")}</span>");
            sb.AppendLine($"<span class='badge'>{proj.Dependencies.Count} Libs</span>");
            sb.AppendLine("</div></div>");

            sb.AppendLine("<div class='project-body'>");

            if (projectRisks.Any())
            {
                foreach (var risk in projectRisks)
                {
                    string cssClass = risk.Level switch
                    {
                        Severity.High or Severity.Critical => "risk-high",
                        Severity.Medium => "risk-med",
                        _ => "risk-low"
                    };

                    string icon = risk.Level switch
                    {
                        Severity.High or Severity.Critical => "🚫",
                        Severity.Medium => "⚠️",
                        _ => "ℹ️"
                    };

                    sb.AppendLine($"<div class='risk-item {cssClass}'>");
                    sb.AppendLine($"<div class='risk-icon'>{icon}</div>");
                    sb.AppendLine("<div class='risk-content'>");
                    sb.AppendLine($"<strong>{risk.Description}</strong>");
                    sb.AppendLine($"<span class='recommendation'>💡 Suggestion: {risk.Recommendation}</span>");
                    sb.AppendLine("</div></div>");
                }
            }
            else
            {
                sb.AppendLine("<div class='clean-state'>✅ No critical risks detected based on standard rules.</div>");
            }

            sb.AppendLine("</div></div>");
        }

        return sb.ToString();
    }
}