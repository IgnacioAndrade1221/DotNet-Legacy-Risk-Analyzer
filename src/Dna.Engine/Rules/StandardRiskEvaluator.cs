using Dna.Core.Interfaces;
using Dna.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Dna.Engine.Rules;

public class StandardRiskEvaluator : IRiskEvaluator
{
    public List<DetectedRisk> Evaluate(ProjectAnalysis analysis)
    {
        var risks = new List<DetectedRisk>();

        if (analysis.TargetFramework.StartsWith("v4"))
        {
            risks.Add(new DetectedRisk
            {
                Code = "NET-001",
                Description = $"Uso de Framework Legacy ({analysis.TargetFramework})",
                Level = Severity.High,
                Recommendation = "Planificar migración a .NET 6/8 LTS mediante Upgrade Assistant."
            });
        }

        if (!analysis.IsSdkStyle)
        {
            risks.Add(new DetectedRisk
            {
                Code = "PRJ-002",
                Description = "Formato de proyecto antiguo (Non-SDK Style)",
                Level = Severity.Medium,
                Recommendation = "Convertir a SDK Style para facilitar gestión de NuGets y DevOps."
            });
        }

        foreach (var dep in analysis.Dependencies)
        {
            if (dep.Name.Equals("System.Data.SqlClient", System.StringComparison.OrdinalIgnoreCase))
            {
                risks.Add(new DetectedRisk
                {
                    Code = "DEP-003",
                    Description = "Driver SQL Legacy detectado (System.Data.SqlClient)",
                    Level = Severity.Medium,
                    Recommendation = "Reemplazar por Microsoft.Data.SqlClient para soporte TLS moderno."
                });
            }

            if (dep.Name.Equals("jQuery", System.StringComparison.OrdinalIgnoreCase))
            {
                risks.Add(new DetectedRisk
                {
                    Code = "WEB-004",
                    Description = "Uso de jQuery (Tecnología Frontend Legacy)",
                    Level = Severity.Low,
                    Recommendation = "Evaluar migración progresiva a React/Angular/Vue si hay mucha lógica en cliente."
                });
            }
        }

        return risks;
    }
}