using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Airo;

/// <summary>
/// Uses Microsoft's Windows Package Manager and its verified package manifest.
/// No installers are downloaded or executed directly by Airo.
/// </summary>
public static class ReceiverInstaller
{
    public const string PackageId = "leapbtw.uxplay";

    public static async Task<int> RunAsync(bool update)
    {
        var executable = FindWinget();
        if (executable is null)
            throw new FileNotFoundException("Windows Package Manager (winget) was not found.");

        var verb = update ? "upgrade" : "install";
        var start = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"{verb} --id {PackageId} --exact --source winget --interactive --accept-source-agreements --accept-package-agreements",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(start)
            ?? throw new Win32Exception("Windows Package Manager could not be started.");

        // Drain both streams to prevent the child from blocking on full pipes.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdout, stderr, process.WaitForExitAsync());
        return process.ExitCode;
    }

    public static string? FindWinget()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var known = Path.Combine(local, "Microsoft", "WindowsApps", "winget.exe");
        if (File.Exists(known)) return known;

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var file = Path.Combine(directory.Trim().Trim('"'), "winget.exe");
                if (File.Exists(file)) return file;
            }
            catch (ArgumentException) { }
        }
        return null;
    }
}
