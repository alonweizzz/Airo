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
public sealed record ReceiverCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string HexCode => $"0x{unchecked((uint)ExitCode):X8}";
    public bool PackageNotFound => unchecked((uint)ExitCode) == 0x8A150014;
    public bool NoUpdateAvailable => unchecked((uint)ExitCode) == 0x8A15002B;
    public bool AlreadyInstalled => unchecked((uint)ExitCode) == 0x8A15010D;
    public string Diagnostics => $"WinGet exit: {ExitCode} ({HexCode})\nOutput:\n{StandardOutput}\nErrors:\n{StandardError}";
}

public static class ReceiverInstaller
{
    public const string PackageId = "leapbtw.uxplay";

    public static async Task<ReceiverCommandResult> RunAsync(bool update)
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
        return new ReceiverCommandResult(process.ExitCode, await stdout, await stderr);
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
