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

        risks.AddRange(EvaluateArchitectureRisks(analysis));
        risks.AddRange(EvaluateToolingRisks(analysis));
        risks.AddRange(EvaluateDependencyRisks(analysis));
        risks.AddRange(EvaluateSecurityRisks(analysis));
        risks.AddRange(EvaluateCodeQualityRisks(analysis));

        return risks;
    }

    private IEnumerable<DetectedRisk> EvaluateArchitectureRisks(ProjectAnalysis analysis)
    {
        if (analysis.TargetFramework.StartsWith("v4"))
        {
            yield return new DetectedRisk
            {
                Code = "NET--001",
                Category = RiskCategory.Architecture,
                Description = $"Uso de Framework Legacy ({analysis.TargetFramework})",
                Level = Severity.High,
                Recommendation = "Ejecuta 'dotnet-upgrade-assistant upgrade' para iniciar la migración de la base a .NET 8+ LTS."
            };
        }
    }
    private IEnumerable<DetectedRisk> EvaluateToolingRisks(ProjectAnalysis analysis)
    {
        if (!analysis.IsSdkStyle)
        {
            yield return new DetectedRisk
            {
                Code = "PRJ--002",
                Category = RiskCategory.Tooling,
                Description = "Formato de proyecto antiguo (Non-SDK Style)",
                Level = Severity.Medium,
                Recommendation = "Convertir a SDK Style con la herramienta 'try-convert' para facilitar flujos de CI/CD ."
            };
        }
    }
    private IEnumerable<DetectedRisk> EvaluateDependencyRisks(ProjectAnalysis analysis)
    {
        foreach (var dep in analysis.Dependencies)
        {
            if (dep.Name.Equals("System.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
            {
                yield return new DetectedRisk
                {
                    Code = "DEP-003",
                    Category = RiskCategory.Security,
                    Description = "Driver SQL Legacy detectado (System.Data.SqlClient)",
                    Level = Severity.High,
                    Recommendation = "Reemplazar por Microsoft.Data.SqlClient para soporte TLS 1.2+ y Always Encrypted."
                };
            }

            if (dep.Name.Equals("jQuery", StringComparison.OrdinalIgnoreCase))
            {
                yield return new DetectedRisk
                {
                    Code = "WEB-004",
                    Category = RiskCategory.Frontend,
                    Description = "Uso de jQuery (Tecnología Frontend Legacy)",
                    Level = Severity.Low,
                    Recommendation = "Evaluar migración a Angular/React si la UI requiere alta interactividad."
                };
            }
        }
    }
    private IEnumerable<DetectedRisk> EvaluateSecurityRisks(ProjectAnalysis analysis)
    {
        foreach (var config in analysis.ConfigurationFiles)
        {
            string fileName = config.Key;
            string content = config.Value;

            // ── 1. SEC-002: Contraseñas en texto plano vs encriptadas ──────────────
            bool hasPasswordKeyword = content.Contains("Password=", StringComparison.OrdinalIgnoreCase) ||
                                      content.Contains("Pwd=", StringComparison.OrdinalIgnoreCase) ||
                                      content.Contains("\"Password\":", StringComparison.OrdinalIgnoreCase);

            if (hasPasswordKeyword)
            {
                // Indicadores comunes de valores encriptados
                bool looksEncrypted = System.Text.RegularExpressions.Regex.IsMatch(content,
                    @"(Password|Pwd)\s*=\s*[A-Za-z0-9+/]{20,}={0,2}",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
                    content.Contains("configProtectionProvider", StringComparison.OrdinalIgnoreCase);

                if (looksEncrypted)
                {
                    yield return new DetectedRisk
                    {
                        Code = "SEC-002-ENC",
                        Category = RiskCategory.Security,
                        Description = $"Contraseña detectada en {fileName} — parece encriptada (configProtectionProvider presente).",
                        Level = Severity.Low,
                        Recommendation = "Verificar que la encriptación sea DPAPI o Azure Key Vault. " +
                                         "Asegurarse de que la clave de descifrado no esté en el mismo repositorio."
                    };
                }
                else
                {
                    yield return new DetectedRisk
                    {
                        Code = "SEC-002",
                        Category = RiskCategory.Security,
                        Description = $"Posible contraseña expuesta en TEXTO PLANO en {fileName}.",
                        Level = Severity.Critical,
                        Recommendation = "URGENTE: Mover secretos a Variables de Entorno, Azure Key Vault o User Secrets. " +
                                         "NUNCA commitear credenciales."
                    };
                }
            }

            // ── 2. SEC-003: ConnectionStrings sin encriptar ─────────────────────────
            bool hasConnectionStrings = content.Contains("<connectionStrings>", StringComparison.OrdinalIgnoreCase) ||
                                        content.Contains("\"ConnectionStrings\"", StringComparison.OrdinalIgnoreCase);

            if (hasConnectionStrings)
            {
                // Buscar data source / server dentro del bloque
                bool hasServerCredentials = System.Text.RegularExpressions.Regex.IsMatch(content,
                    @"(Data Source|Server|Uid|User Id|user\s*=)\s*=\s*[^;""<]{2,}",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                bool connectionStringEncrypted = content.Contains("configProtectionProvider", StringComparison.OrdinalIgnoreCase);

                if (hasServerCredentials && !connectionStringEncrypted)
                {
                    yield return new DetectedRisk
                    {
                        Code = "SEC-003",
                        Category = RiskCategory.Security,
                        Description = $"<connectionStrings> con credenciales en texto plano detectada en {fileName}.",
                        Level = Severity.Critical,
                        Recommendation = "Encriptar la sección <connectionStrings> con aspnet_regiis o mover a " +
                                         "Azure Key Vault / Variables de Entorno."
                    };
                }
                else if (connectionStringEncrypted)
                {
                    yield return new DetectedRisk
                    {
                        Code = "SEC-003-ENC",
                        Category = RiskCategory.Security,
                        Description = $"<connectionStrings> encriptada detectada en {fileName}.",
                        Level = Severity.Low,
                        Recommendation = "Confirmar que la clave DPAPI o el proveedor de encriptación estén correctamente protegidos."
                    };
                }
            }

            // ── 3. SEC-004: Credenciales comentadas ────────────────────────────────
            // Captura comentarios XML <!-- ... --> y comentarios de código // o /* */
            var commentedSecrets = System.Text.RegularExpressions.Regex.Matches(content,
                @"(<!--[\s\S]*?-->|//[^\n]*|/\*[\s\S]*?\*/)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match comment in commentedSecrets)
            {
                string commentText = comment.Value;
                bool secretInComment =
                    commentText.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                    commentText.Contains("Pwd", StringComparison.OrdinalIgnoreCase) ||
                    commentText.Contains("connectionString", StringComparison.OrdinalIgnoreCase) ||
                    commentText.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
                    commentText.Contains("Secret", StringComparison.OrdinalIgnoreCase);

                if (secretInComment)
                {
                    yield return new DetectedRisk
                    {
                        Code = "SEC-004",
                        Category = RiskCategory.Security,
                        Description = $"Posible credencial encontrada dentro de un COMENTARIO en {fileName}. ",
                        Level = Severity.High,
                        Recommendation = "Eliminar completamente cualquier credencial comentada del código fuente. " +
                                         "Los comentarios también se commitean al repositorio."
                    };
                    break;
                }
            }
        }
    }

    private IEnumerable<DetectedRisk> EvaluateCodeQualityRisks(ProjectAnalysis analysis)
    {
        foreach (var finding in analysis.SourceCodeFindings)
        {
            string fileName = finding.Key;
            int count = finding.Value.Count;

            yield return new DetectedRisk
            {
                Code = "CODE-001",
                Category = RiskCategory.Database,
                Description = $"Arquitectura de datos obsoleta en {fileName}",
                Level = Severity.Medium,
                Recommendation = $"Se encontraron {count} usos de DataTable/DataSet. Migrar a DTOs con tipado fuerte (POCOs) para mejorar el rendimiento y la mantenibilidad."
            };
        }
    }
}