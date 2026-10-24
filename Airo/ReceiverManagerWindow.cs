using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Wpf = System.Windows;
using Controls = System.Windows.Controls;
using Media = System.Windows.Media;
using Input = System.Windows.Input;
using Win32 = Microsoft.Win32;

namespace Airo;

/// <summary>
/// Receiver discovery and management. Does not execute untrusted downloads.
/// Installation remains user-confirmed via the receiver project's releases page.
/// </summary>
public sealed class ReceiverManagerWindow : Wpf.Window
{
    private const string ReceiverReleases = "https://github.com/leapbtw/uxplay-windows/releases";
    private readonly Controls.TextBlock _status;
    private readonly Controls.TextBlock _version;
    private readonly Controls.TextBlock _path;
    private readonly Controls.TextBlock _details;
    private readonly Controls.Button _useButton;
    private readonly AiroSettings _settings;
    private string? _candidate;

    public string? SelectedReceiverPath { get; private set; }

    private static Media.Brush Paint(string color) =>
        new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(color)!);

    private static Controls.TextBlock Text(string text, int size = 13, string color = "#E8F4FF") =>
        new() { Text = text, FontSize = size, Foreground = Paint(color), TextWrapping = Wpf.TextWrapping.Wrap };

    private static Controls.Button Button(string label, bool primary = false) =>
        new()
        {
            Content = label,
            Padding = new Wpf.Thickness(16, 10, 16, 10),
            MinWidth = 110,
            Background = Paint(primary ? "#287CC5" : "#203652"),
            Foreground = Paint("#F4FAFF"),
            BorderBrush = Paint(primary ? "#5CB6EE" : "#3A5676"),
            BorderThickness = new Wpf.Thickness(1),
            Cursor = Input.Cursors.Hand,
            Margin = new Wpf.Thickness(0, 0, 10, 10)
        };

    private static Controls.Border Card(Controls.StackPanel contents) => new()
    {
        Child = contents,
        CornerRadius = new Wpf.CornerRadius(16),
        Background = Paint("#15263F"),
        BorderBrush = Paint("#2C496C"),
        BorderThickness = new Wpf.Thickness(1),
        Padding = new Wpf.Thickness(20),
        Margin = new Wpf.Thickness(0, 0, 0, 16)
    };

    public ReceiverManagerWindow(AiroSettings settings)
    {
        _settings = settings;
        Title = "Airo Beta 0.3 — Receiver Manager";
        Width = 700;
        Height = 640;
        MinWidth = 580;
        MinHeight = 510;
        WindowStartupLocation = Wpf.WindowStartupLocation.CenterOwner;
        Background = Paint("#0B1428");
        Foreground = Paint("#EFF8FF");
        FontFamily = new Media.FontFamily("Segoe UI");

        var root = new Controls.DockPanel { Margin = new Wpf.Thickness(26) };
        Content = root;

        var footer = new Controls.StackPanel
        {
            Orientation = Controls.Orientation.Horizontal,
            HorizontalAlignment = Wpf.HorizontalAlignment.Right
        };
        Controls.DockPanel.SetDock(footer, Controls.Dock.Bottom);
        root.Children.Add(footer);
        _useButton = Button("Use this receiver", true);
        _useButton.Click += (_, _) => SaveSelection();
        footer.Children.Add(_useButton);
        var close = Button("Close");
        close.Click += (_, _) => Close();
        footer.Children.Add(close);

        var scroll = new Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = Controls.ScrollBarVisibility.Auto
        };
        root.Children.Add(scroll);
        var body = new Controls.StackPanel();
        scroll.Content = body;
        var title = Text("Receiver Manager", 27);
        title.FontWeight = Wpf.FontWeights.SemiBold;
        title.Margin = new Wpf.Thickness(0, 0, 0, 6);
        body.Children.Add(title);
        var subtitle = Text("Find, inspect and configure your AirPlay receiver.", 13, "#A6BCD5");
        subtitle.Margin = new Wpf.Thickness(0, 0, 0, 24);
        body.Children.Add(subtitle);

        var statusCard = new Controls.StackPanel();
        body.Children.Add(Card(statusCard));
        var heading = Text("Receiver status", 17);
        heading.FontWeight = Wpf.FontWeights.SemiBold;
        heading.Margin = new Wpf.Thickness(0, 0, 0, 12);
        statusCard.Children.Add(heading);
        _status = Text("Checking...", 15, "#7ACCF8");
        statusCard.Children.Add(_status);
        _path = Text("", 12, "#A8BED9");
        _path.Margin = new Wpf.Thickness(0, 9, 0, 8);
        statusCard.Children.Add(_path);
        _version = Text("", 12, "#D3E7F9");
        statusCard.Children.Add(_version);
        _details = Text("", 12, "#8EAACB");
        _details.Margin = new Wpf.Thickness(0, 8, 0, 0);
        statusCard.Children.Add(_details);

        var actionsCard = new Controls.StackPanel();
        body.Children.Add(Card(actionsCard));
        var actionsTitle = Text("Manage installation", 17);
        actionsTitle.FontWeight = Wpf.FontWeights.SemiBold;
        actionsTitle.Margin = new Wpf.Thickness(0, 0, 0, 8);
        actionsCard.Children.Add(actionsTitle);
        var guidance = Text("Airo can detect your existing UxPlay installation. To install or update, open the Windows receiver releases, run its installer, then scan again.", 12, "#A8BED9");
        guidance.Margin = new Wpf.Thickness(0, 0, 0, 16);
        actionsCard.Children.Add(guidance);
        var buttons = new Controls.WrapPanel();
        actionsCard.Children.Add(buttons);
        var detect = Button("Scan again", true);
        detect.Click += (_, _) => Detect();
        buttons.Children.Add(detect);
        var browse = Button("Choose EXE");
        browse.Click += (_, _) => Browse();
        buttons.Children.Add(browse);
        var install = Button("Install / update...");
        install.Click += (_, _) => OpenInstallerPage();
        buttons.Children.Add(install);
        var disclaimer = Text("Install/update opens a third-party releases page in your browser; Airo does not download or run installers automatically. Verify the publisher before installing.", 11, "#8EAACB");
        disclaimer.Margin = new Wpf.Thickness(0, 4, 0, 0);
        actionsCard.Children.Add(disclaimer);

        Detect();
    }

    private static string? Discover(string? saved)
    {
        if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved)) return saved;
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };
        string[] relatives =
        {
            Path.Combine("uxplay-windows", "uxplay-windows.exe"),
            Path.Combine("UxPlay", "uxplay.exe"),
            Path.Combine("Programs", "uxplay-windows", "uxplay-windows.exe")
        };
        foreach (var root in roots.Where(r => !string.IsNullOrWhiteSpace(r)))
            foreach (var relative in relatives)
            {
                var path = Path.Combine(root, relative);
                if (File.Exists(path)) return path;
            }
        return null;
    }

    private void Detect()
    {
        _candidate = Discover(_settings.ReceiverPath);
        if (_candidate is null)
        {
            _status.Text = "Not found";
            _status.Foreground = Paint("#FFC47B");
            _path.Text = "No receiver was found in the configured or common installation paths.";
            _version.Text = "";
            _details.Text = "Install a receiver, or choose its EXE if it is in another folder.";
            _useButton.IsEnabled = false;
            return;
        }
        ShowCandidate(_candidate);
    }

    private void ShowCandidate(string path)
    {
        _candidate = path;
        _status.Text = "Receiver executable found";
        _status.Foreground = Paint("#78E0B0");
        _path.Text = path;
        try
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            var version = !string.IsNullOrWhiteSpace(info.ProductVersion)
                ? info.ProductVersion : info.FileVersion;
            _version.Text = string.IsNullOrWhiteSpace(version)
                ? "Version: not provided by the executable"
                : "File version: " + version;
        }
        catch { _version.Text = "Version: unavailable"; }
        _details.Text = "File found. AirPlay connectivity and stream health have not been tested.";
        _useButton.IsEnabled = true;
    }

    private void Browse()
    {
        var picker = new Win32.OpenFileDialog
        {
            Title = "Choose installed UxPlay executable",
            Filter = "Executable files (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (picker.ShowDialog(this) == true) ShowCandidate(picker.FileName);
    }

    private void OpenInstallerPage()
    {
        var result = Wpf.MessageBox.Show(this,
            "This opens a third-party Windows UxPlay releases page. Review the publisher and installer before downloading. Continue?",
            "Install or update receiver", Wpf.MessageBoxButton.YesNo, Wpf.MessageBoxImage.Information);
        if (result != Wpf.MessageBoxResult.Yes) return;
        try
        {
            Process.Start(new ProcessStartInfo(ReceiverReleases) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Wpf.MessageBox.Show(this, "Could not open the browser: " + ex.Message,
                "Airo", Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Error);
        }
    }

    private void SaveSelection()
    {
        if (_candidate is null || !File.Exists(_candidate))
        {
            Detect();
            return;
        }
        try
        {
            _settings.ReceiverPath = _candidate;
            _settings.Save();
            SelectedReceiverPath = _candidate;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            Wpf.MessageBox.Show(this, "Could not save receiver: " + ex.Message,
                "Airo", Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Error);
        }
    }
}
