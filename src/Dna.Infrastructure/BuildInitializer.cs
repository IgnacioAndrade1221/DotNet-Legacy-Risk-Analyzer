using Microsoft.Build.Locator;
using System;
using System.IO;
using System.Linq;

namespace Dna.Infrastructure;

public static class BuildInitializer
{
    public static void Initialize()
    {
        if (MSBuildLocator.IsRegistered) return;

        Console.WriteLine("🔍 Debug: Searching for MSBuild instances...");

        // 1. Intento Estándar (Query oficial)
        var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
        Console.WriteLine($"🔍 Debug: Locator found {instances.Count} instances.");

        var bestInstance = instances.OrderByDescending(x => x.Version).FirstOrDefault();

        if (bestInstance != null)
        {
            MSBuildLocator.RegisterInstance(bestInstance);
            Console.WriteLine($"✅ Registered MSBuild: {bestInstance.Name} ({bestInstance.Version})");
            return;
        }

        // 2. Intento de Rescate Manual (Si el paso 1 falló)
        Console.WriteLine("⚠️ No instances found via Locator. Attempting manual SDK discovery...");

        string manualPath = TryManualSdkDiscovery();

        if (!string.IsNullOrEmpty(manualPath))
        {
            Console.WriteLine($"🔍 Manual SDK found at: {manualPath}");
            MSBuildLocator.RegisterMSBuildPath(manualPath);
            Console.WriteLine("✅ Registered MSBuild via Manual Discovery.");
        }
        else
        {
            // 3. Último recurso (Fallará si no encontró nada arriba, pero hay que intentarlo)
            Console.WriteLine("⚠️ Manual discovery failed. Trying defaults...");
            MSBuildLocator.RegisterDefaults();
        }
    }

    private static string TryManualSdkDiscovery()
    {
        // Rutas comunes donde vive el SDK de .NET en Windows
        string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "sdk");

        if (!Directory.Exists(basePath))
        {
            // Intenta en Program Files (x86) por si acaso
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet", "sdk");
            if (!Directory.Exists(basePath)) return null;
        }

        // Buscamos las carpetas de versiones (ej: 8.0.100, 9.0.305)
        var directories = Directory.GetDirectories(basePath)
                                   .Select(d => new DirectoryInfo(d))
                                   .Where(d => char.IsDigit(d.Name[0])) // Solo carpetas que parecen versiones
                                   .OrderByDescending(d => d.Name)      // Tomamos la más nueva
                                   .ToList();

        foreach (var dir in directories)
        {
            // Validamos que tenga el archivo dll crítico
            if (File.Exists(Path.Combine(dir.FullName, "Microsoft.Build.dll")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}