using System;
using System.IO;
using Wpf = System.Windows;
using Controls = System.Windows.Controls;
using Media = System.Windows.Media;
using Win32 = Microsoft.Win32;

namespace Airo;

public sealed class SettingsWindow : Wpf.Window
{
    private readonly Controls.TextBox _receiverPath;
    private readonly Controls.CheckBox _autoStart;
    private readonly Controls.CheckBox _startMaximized;
    private readonly Controls.CheckBox _alwaysOnTop;
    private readonly Controls.ComboBox _layout;

    public AiroSettings? SavedSettings { get; private set; }

    private static Media.Brush Brush(string hex) =>
        new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(hex)!);

    private static Controls.TextBlock Label(string text, int size = 14, bool bold = false) =>
        new()
        {
            Text = text,
            Foreground = Brush("#F1F7FF"),
            FontSize = size,
            FontWeight = bold ? Wpf.FontWeights.SemiBold : Wpf.FontWeights.Normal,
            Margin = new Wpf.Thickness(0, 0, 0, 7)
        };

    private static Controls.TextBlock Hint(string text) =>
        new()
        {
            Text = text,
            TextWrapping = Wpf.TextWrapping.Wrap,
            Foreground = Brush("#A9BCD6"),
            FontSize = 12,
            Margin = new Wpf.Thickness(0, 0, 0, 15)
        };

    private static Controls.CheckBox Toggle(string title, bool value) =>
        new()
        {
            Content = title,
            IsChecked = value,
            Foreground = Brush("#E7F4FF"),
            FontSize = 14,
            Margin = new Wpf.Thickness(0, 6, 0, 7)
        };

    private static Controls.Button ActionButton(string title, bool primary = false) =>
        new()
        {
            Content = title,
            Padding = new Wpf.Thickness(20, 10, 20, 10),
            Background = Brush(primary ? "#2469B0" : "#253D60"),
            Foreground = Brush("#FFFFFF"),
            BorderBrush = Brush(primary ? "#4FA5E5" : "#48698E"),
            BorderThickness = new Wpf.Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Wpf.Thickness(6, 0, 0, 0),
            MinWidth = 92
        };

    public SettingsWindow(AiroSettings current)
    {
        Title = "Airo Beta 0.2.1 — Settings";
        Width = 620;
        Height = 640;
        MinWidth = 520;
        MinHeight = 550;
        WindowStartupLocation = Wpf.WindowStartupLocation.CenterOwner;
        Background = Brush("#0B1428");
        Foreground = Brush("#FFFFFF");
        FontFamily = new Media.FontFamily("Segoe UI");

        var root = new Controls.DockPanel { Margin = new Wpf.Thickness(28) };
        Content = root;

        var footer = new Controls.StackPanel
        {
            Orientation = Controls.Orientation.Horizontal,
            HorizontalAlignment = Wpf.HorizontalAlignment.Right,
            Margin = new Wpf.Thickness(0, 18, 0, 0)
        };
        Controls.DockPanel.SetDock(footer, Controls.Dock.Bottom);
        root.Children.Add(footer);

        var cancel = ActionButton("Cancel");
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        var save = ActionButton("Save settings", true);
        save.Click += (_, _) => Save();
        footer.Children.Add(cancel);
        footer.Children.Add(save);

        var scroll = new Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Controls.ScrollBarVisibility.Disabled
        };
        root.Children.Add(scroll);
        var body = new Controls.StackPanel();
        scroll.Content = body;
        body.Children.Add(Label("Settings", 28, true));
        body.Children.Add(Hint("Customize how Airo launches and displays your iPhone screen."));

        body.Children.Add(Label("AirPlay receiver", 17, true));
        body.Children.Add(Hint("Airo uses the Windows receiver you installed. Your existing selection is kept."));
        var receiverRow = new Controls.DockPanel { Margin = new Wpf.Thickness(0, 0, 0, 16) };
        body.Children.Add(receiverRow);
        var browse = ActionButton("Browse...");
        Controls.DockPanel.SetDock(browse, Controls.Dock.Right);
        receiverRow.Children.Add(browse);
        _receiverPath = new Controls.TextBox
        {
            Text = current.ReceiverPath,
            IsReadOnly = true,
            Background = Brush("#15243C"),
            Foreground = Brush("#CFE5FF"),
            BorderBrush = Brush("#45638B"),
            Padding = new Wpf.Thickness(10),
            VerticalContentAlignment = Wpf.VerticalAlignment.Center,
            MinHeight = 38
        };
        receiverRow.Children.Add(_receiverPath);
        browse.Click += (_, _) =>
        {
            var picker = new Win32.OpenFileDialog
            {
                Title = "Select your installed UxPlay receiver",
                Filter = "Windows executables (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (picker.ShowDialog(this) == true) _receiverPath.Text = picker.FileName;
        };

        body.Children.Add(Label("Startup", 17, true));
        _autoStart = Toggle("Start the AirPlay receiver when Airo opens", current.AutoStartReceiver);
        body.Children.Add(_autoStart);
        body.Children.Add(Hint("If enabled, Airo starts your saved receiver automatically. You can still stop it with Devices."));
        _startMaximized = Toggle("Open Airo maximized", current.StartMaximized);
        body.Children.Add(_startMaximized);
        _alwaysOnTop = Toggle("Keep Airo above other windows", current.AlwaysOnTop);
        body.Children.Add(_alwaysOnTop);

        body.Children.Add(Label("Video layout", 17, true));
        body.Children.Add(Hint("Choose the initial shape of the embedded screen. Press F6 to switch at any time."));
        _layout = new Controls.ComboBox
        {
            Background = Brush("#172C4B"),
            Foreground = Brush("#172C4B"),
            Padding = new Wpf.Thickness(9),
            MinHeight = 38,
            Margin = new Wpf.Thickness(0, 0, 0, 12)
        };
        _layout.Items.Add("Portrait (iPhone)");
        _layout.Items.Add("Wide (16:9)");
        _layout.SelectedIndex = current.WideLayout ? 1 : 0;
        body.Children.Add(_layout);
    }

    private void Save()
    {
        if (_autoStart.IsChecked == true && !File.Exists(_receiverPath.Text))
        {
            Wpf.MessageBox.Show(this,
                "Select a valid receiver EXE before enabling automatic startup.",
                "Airo settings", Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Warning);
            return;
        }
        var next = new AiroSettings
        {
            ReceiverPath = _receiverPath.Text.Trim(),
            AutoStartReceiver = _autoStart.IsChecked == true,
            StartMaximized = _startMaximized.IsChecked == true,
            AlwaysOnTop = _alwaysOnTop.IsChecked == true,
            WideLayout = _layout.SelectedIndex == 1
        };
        try { next.Save(); }
        catch (Exception ex)
        {
            Wpf.MessageBox.Show(this, "Could not save settings: " + ex.Message,
                "Airo settings", Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Error);
            return;
        }
        SavedSettings = next;
        DialogResult = true;
        Close();
    }
}
