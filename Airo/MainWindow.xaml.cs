using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Airo;
public partial class MainWindow : Window
{
    private Process? _receiver;
    private string? _receiverPath;
    public MainWindow()
    {
        InitializeComponent();
        LoadBrandLogo();
        _receiverPath = LoadReceiverPath();
        Closed += (_, _) => StopReceiver();
    }
    private static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Airo", "receiver-path.txt");
    private static string? LoadReceiverPath() => File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath).Trim() : null;
    private void LoadBrandLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "AiroLogo.png");
        if (!File.Exists(path)) return;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            BrandLogo.Source = bitmap;
            BrandLogo.Visibility = Visibility.Visible;
            LogoFallback.Visibility = Visibility.Collapsed;
        }
        catch (Exception) { }
    }
    private void UpdateStatus(string status, Brush color)
    {
        ReceiverStatus.Text = status;
        ReceiverDot.Fill = color;
    }
    private void Home_Click(object sender, RoutedEventArgs e) { }
    private void Devices_Click(object sender, RoutedEventArgs e)
    {
        if (_receiver is { HasExited: false })
        {
            if (MessageBox.Show("Stop the receiver process?", "Airo", MessageBoxButton.YesNo) == MessageBoxResult.Yes) StopReceiver();
            return;
        }
        if (string.IsNullOrWhiteSpace(_receiverPath) || !File.Exists(_receiverPath))
        {
            MessageBox.Show("Select a compatible Windows AirPlay receiver executable first. Airo does not bundle a receiver yet. Only select software you trust.", "Receiver setup");
            SelectReceiver();
            if (string.IsNullOrWhiteSpace(_receiverPath)) return;
        }
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = _receiverPath,
                WorkingDirectory = Path.GetDirectoryName(_receiverPath)!,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            _receiver = new Process { StartInfo = start, EnableRaisingEvents = true };
            _receiver.Exited += (_, _) => Dispatcher.BeginInvoke(() =>
            {
                _receiver?.Dispose();
                _receiver = null;
                UpdateStatus("Receiver stopped", Brushes.Orange);
            });
            if (!_receiver.Start()) throw new InvalidOperationException("Receiver process could not start.");
            UpdateStatus("Receiver process running", Brushes.LightGreen);
            MessageBox.Show("Receiver process started. This does not confirm AirPlay discovery or an iPhone connection. Check the receiver window and your iPhone's Screen Mirroring menu.", "Airo");
        }
        catch (Exception ex)
        {
            UpdateStatus("Receiver launch failed", Brushes.OrangeRed);
            MessageBox.Show(ex.Message, "Unable to start receiver", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void StopReceiver()
    {
        try { if (_receiver is { HasExited: false }) _receiver.Kill(entireProcessTree: true); }
        catch (Exception) { }
        _receiver?.Dispose();
        _receiver = null;
        UpdateStatus("Receiver offline", Brushes.Orange);
    }
    private void SelectReceiver()
    {
        var dialog = new OpenFileDialog { Title = "Choose AirPlay receiver executable", Filter = "Applications (*.exe)|*.exe", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        _receiverPath = dialog.FileName;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, _receiverPath);
        UpdateStatus("Receiver configured", Brushes.LightSkyBlue);
    }
    private void Capture_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Screenshots and recording will follow after video rendering is integrated.", "Capture — coming soon");
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Choose or change the external AirPlay receiver executable?", "Receiver settings", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            SelectReceiver();
    }
    private void About_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Airo Beta 0.1\n\nExperimental external receiver launcher. AirPlay video/audio integration into the Airo window is not implemented.", "About Airo");
}