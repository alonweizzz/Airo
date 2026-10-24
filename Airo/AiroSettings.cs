using System;
using System.IO;
using System.Text.Json;

namespace Airo;

public sealed class AiroSettings
{
    public string ReceiverPath { get; set; } = "";
    public bool AutoStartReceiver { get; set; }
    public bool StartMaximized { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool WideLayout { get; set; }

    private static string SettingsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Airo");
    private static string SettingsFile => Path.Combine(SettingsFolder, "settings.json");
    private static string LegacyReceiverFile => Path.Combine(SettingsFolder, "receiver-path.txt");

    public static AiroSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
                return JsonSerializer.Deserialize<AiroSettings>(File.ReadAllText(SettingsFile)) ?? new AiroSettings();
        }
        catch (Exception) { /* Invalid settings: fall back to defaults. */ }

        var settings = new AiroSettings();
        try
        {
            // Preserve the receiver selected in Beta 0.2.
            if (File.Exists(LegacyReceiverFile))
                settings.ReceiverPath = File.ReadAllText(LegacyReceiverFile).Trim();
        }
        catch (Exception) { }
        return settings;
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsFolder);
        var temp = SettingsFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, SettingsFile, overwrite: true);
    }
}
