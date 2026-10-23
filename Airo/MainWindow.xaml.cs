using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Airo;
public partial class MainWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, bool repaint);
    private const int GWL_STYLE = -16;
    private const long WS_CHILD = 0x40000000, WS_POPUP = unchecked((int)0x80000000), WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000;

    private Process? _receiver;
    private string? _receiverPath;
    private IntPtr _embeddedWindow;
    private readonly Forms.Panel _videoPanel = new() { BackColor = System.Drawing.Color.Black };
    private int _launchId;

    public MainWindow()
    {
        InitializeComponent();
        VideoHost.Child = _videoPanel;
        _videoPanel.Resize += (_, _) => ResizeVideo();
        LoadBrandLogo();
        _receiverPath = LoadReceiverPath();
        Closed += (_, _) => StopReceiver();
    }
    private static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Airo", "receiver-path.txt");
    private static string? LoadReceiverPath()
    {
        try { return File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath).Trim() : null; }
        catch { return null; }
    }
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
        catch { }
    }
    private void UpdateStatus(string status, Brush color)
    {
        ReceiverStatus.Text = status;
        ReceiverDot.Fill = color;
    }
    private void ResizeVideo()
    {
        if (_embeddedWindow != IntPtr.Zero)
            MoveWindow(_embeddedWindow, 0, 0, _videoPanel.ClientSize.Width, _videoPanel.ClientSize.Height, true);
    }
    private void Home_Click(object sender, RoutedEventArgs e) { }
    private async void Devices_Click(object sender, RoutedEventArgs e)
    {
        if (_receiver is { HasExited: false })
        {
            if (MessageBox.Show("Stop the receiver?", "Airo", MessageBoxButton.YesNo) == MessageBoxResult.Yes) StopReceiver();
            return;
        }
        if (string.IsNullOrWhiteSpace(_receiverPath) || !File.Exists(_receiverPath))
        {
            SelectReceiver();
            if (string.IsNullOrWhiteSpace(_receiverPath) || !File.Exists(_receiverPath)) return;
        }
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _receiverPath,
                    WorkingDirectory = Path.GetDirectoryName(_receiverPath)!,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            _receiver = process;
            var launch = ++_launchId;
            process.Exited += (_, _) => Dispatcher.BeginInvoke(() =>
            {
                if (launch != _launchId) return;
                _embeddedWindow = IntPtr.Zero;
                VideoHost.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
                UpdateStatus("Receiver stopped", Brushes.Orange);
                _receiver = null;
                process.Dispose();
            });
            if (!process.Start()) throw new InvalidOperationException("Receiver did not start.");
            UpdateStatus("Waiting for receiver window", Brushes.LightSkyBlue);
            await AttachWhenReady(process, launch);
        }
        catch (Exception ex)
        {
            StopReceiver();
            UpdateStatus("Receiver launch failed", Brushes.OrangeRed);
            MessageBox.Show(ex.Message, "Airo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async Task AttachWhenReady(Process process, int launch)
    {
        // Some UxPlay builds create their video window only after the iPhone connects.
        for (int i = 0; i < 120 && launch == _launchId; i++)
        {
            await Task.Delay(1000);
            if (process.HasExited || launch != _launchId) return;
            process.Refresh();
            var hwnd = process.MainWindowHandle;
            if (hwnd == IntPtr.Zero) continue;
            try
            {
                VideoHost.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                _videoPanel.CreateControl();
                var style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
                var childStyle = (style | WS_CHILD) & ~(WS_POPUP | WS_CAPTION | WS_THICKFRAME);
                SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(childStyle));
                SetParent(hwnd, _videoPanel.Handle);
                _embeddedWindow = hwnd;
                ResizeVideo();
                UpdateStatus("Receiver window embedded", Brushes.LightGreen);
                return;
            }
            catch (Exception ex)
            {
                VideoHost.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
                UpdateStatus("Embedding failed", Brushes.OrangeRed);
                MessageBox.Show("Receiver is running, but embedding failed: " + ex.Message, "Airo");
                return;
            }
        }
        if (launch == _launchId && !process.HasExited)
            UpdateStatus("Receiver running (window not found)", Brushes.Orange);
    }
    private void StopReceiver()
    {
        ++_launchId;
        _embeddedWindow = IntPtr.Zero;
        VideoHost.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
        var process = _receiver;
        _receiver = null;
        try { if (process is { HasExited: false }) process.Kill(entireProcessTree: true); }
        catch { }
        // Process.Exited disposes the process asynchronously.
        UpdateStatus("Receiver offline", Brushes.Orange);
    }
    private void SelectReceiver()
    {
        var dialog = new OpenFileDialog { Title = "Choose your installed UxPlay EXE", Filter = "Applications (*.exe)|*.exe", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        _receiverPath = dialog.FileName;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, _receiverPath);
        UpdateStatus("Receiver configured", Brushes.LightSkyBlue);
    }
    private void Capture_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Screenshots and recording are planned for a later beta.", "Capture");
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Change the saved UxPlay executable?", "Receiver settings", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            SelectReceiver();
    }
    private void About_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Airo Beta 0.2\n\nExperimental UxPlay window embedding. Not yet verified on all Windows builds. Audio, capture and reliable connection detection are not integrated.", "About Airo");
}