using Dna.Core.Interfaces;
using Dna.Core.Models;
using Dna.Engine.Reporting;
using Dna.Engine.Rules;
using Dna.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Windows;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Dna.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            string path = TxtSolutionPath.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Por favor, selecciona una solución primero.", "DORA", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BtnAnalyze.IsEnabled = false;
                ProgressAnalyze.IsIndeterminate = true;
                TxtStatus.Text = "Analizando proyectos...";

                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                IConfiguration config = builder.Build();

                var reportSettings = new AnalysisSettings
                {
                    HoursHigh = double.Parse(config["AnalysisSettings:HoursHigh"] ?? "6"),
                    HoursMedium = double.Parse(config["AnalysisSettings:HoursMedium"] ?? "3"),
                    HoursLow = double.Parse(config["AnalysisSettings:HoursLow"] ?? "1.5"),
                    HourlyRate = double.Parse(config["AnalysisSettings:HourlyRate"] ?? "20000"),
                    CurrencySymbol = config["AnalysisSettings:CurrencySymbol"] ?? "$"
                };

                await Task.Run(() =>
                {
                    // 1. Inicializar MSBuild
                    BuildInitializer.Initialize();

                    // 2. Cargar Solución
                    ISolutionLoader loader = new MsBuildSolutionLoader();
                    var projectsRaw = loader.LoadProjects(path).ToList();

                    // 3. Motores de Análisis
                    IProjectAnalyzer analyzer = new XmlProjectAnalyzer();
                    IRiskEvaluator evaluator = new StandardRiskEvaluator();
                    IReportGenerator reportGen = new HtmlReportGenerator();

                    var analyzedProjects = new List<ProjectAnalysis>();
                    var risksMap = new Dictionary<string, List<DetectedRisk>>();

                    // 4. Ejecutar Análisis
                    foreach (var proj in projectsRaw)
                    {
                        var analysis = analyzer.Analyze(proj.AbsolutePath);
                        analyzedProjects.Add(analysis);
                        risksMap[proj.AbsolutePath] = evaluator.Evaluate(analysis);
                    }

                    string reportHtml = reportGen.Generate(
                        System.IO.Path.GetFileName(path),
                        analyzedProjects,
                        risksMap,
                        reportSettings
                    );

                    string reportPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RiskReport.html");
                    System.IO.File.WriteAllText(reportPath, reportHtml);

                    var p = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo(reportPath) { UseShellExecute = true }
                    };
                    p.Start();
                });

                TxtStatus.Text = "¡Análisis Completado!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error Crítico: {ex.Message}", "DORA Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Error en el análisis.";
            }
            finally
            {
                BtnAnalyze.IsEnabled = true;
                ProgressAnalyze.IsIndeterminate = false;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Solution",
                DefaultExt = ".sln",
                Filter = "Visual Studio Solution (.sln)|*.sln"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                TxtSolutionPath.Text = dialog.FileName;
                TxtStatus.Text = "Solución cargada. Lista para analizar.";
            }
        }
    }
}