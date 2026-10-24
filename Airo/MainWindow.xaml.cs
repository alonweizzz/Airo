using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Airo;
public partial class MainWindow : Window
{
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    private const uint SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010, SWP_FRAMECHANGED = 0x0020, SWP_SHOWWINDOW = 0x0040;
    private const int GWL_STYLE = -16;
    private const long WS_CHILD = 0x40000000, WS_POPUP = unchecked((int)0x80000000), WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000;

    private AiroSettings _settings = AiroSettings.Load();
    private Process? _receiver;
    private string? _receiverPath;
    private IntPtr _embeddedWindow;
    private readonly Forms.Panel _videoPanel = new() { BackColor = System.Drawing.Color.Black };
    private int _launchId;
    private readonly DispatcherTimer _resizeTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private int _lastVideoWidth = -1, _lastVideoHeight = -1;
    private bool _wideLayout;
    // iPhone portrait screens are taller than 9:16; use a closer 9:19.5 viewport.
    private const double PortraitAspect = 9.0 / 19.5;
    private const double LandscapeAspect = 16.0 / 9.0;

    public MainWindow()
    {
        InitializeComponent();
        VideoHost.Child = _videoPanel;
        _videoPanel.Resize += (_, _) => ScheduleVideoResize();
        _resizeTimer.Tick += (_, _) => { _resizeTimer.Stop(); ResizeVideo(); };
        DisplaySurface.SizeChanged += (_, _) => UpdateVideoLayout();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.F6)
            {
                _wideLayout = !_wideLayout;
                UpdateVideoLayout();
                e.Handled = true;
            }
        };
        LoadBrandLogo();
        _receiverPath = _settings.ReceiverPath;
        _wideLayout = _settings.WideLayout;
        Topmost = _settings.AlwaysOnTop;
        Loaded += (_, _) =>
        {
            if (_settings.StartMaximized) WindowState = System.Windows.WindowState.Maximized;
        };
        Closed += (_, _) => StopReceiver();
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
    private void UpdateStatus(string status, System.Windows.Media.Brush color)
    {
        ReceiverStatus.Text = status;
        ReceiverDot.Fill = color;
    }
    private void UpdateVideoLayout()
    {
        // Fit the complete receiver viewport without stretching it or cropping.
        // WPF measures in device-independent pixels; WinForms handles native pixels.
        var width = Math.Max(1, DisplaySurface.ActualWidth - 24);
        var height = Math.Max(1, DisplaySurface.ActualHeight - 24);
        var aspect = _wideLayout ? LandscapeAspect : PortraitAspect;
        var fittedWidth = Math.Min(width, height * aspect);
        var fittedHeight = fittedWidth / aspect;
        if (double.IsNaN(VideoHost.Width) || Math.Abs(VideoHost.Width - fittedWidth) > 0.5)
            VideoHost.Width = fittedWidth;
        if (double.IsNaN(VideoHost.Height) || Math.Abs(VideoHost.Height - fittedHeight) > 0.5)
            VideoHost.Height = fittedHeight;
        ScheduleVideoResize();
    }
    private void ScheduleVideoResize()
    {
        if (_embeddedWindow == IntPtr.Zero || _videoPanel.IsDisposed) return;
        // Coalesce rapid resize events instead of repeatedly resizing the native renderer.
        _resizeTimer.Stop();
        _resizeTimer.Start();
    }
    private void ResizeVideo(bool force = false)
    {
        if (_embeddedWindow == IntPtr.Zero || _videoPanel.IsDisposed) return;
        var width = _videoPanel.ClientSize.Width;
        var height = _videoPanel.ClientSize.Height;
        if (width <= 0 || height <= 0) return;
        if (!force && width == _lastVideoWidth && height == _lastVideoHeight) return;
        // SWP_FRAMECHANGED is needed after reparenting, not on every mouse movement.
        SetWindowPos(_embeddedWindow, IntPtr.Zero, 0, 0, width, height,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW |
            (force ? SWP_FRAMECHANGED : 0));
        _lastVideoWidth = width;
        _lastVideoHeight = height;
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
            var manager = new ReceiverManagerWindow(_settings) { Owner = this };
            if (manager.ShowDialog() != true || string.IsNullOrWhiteSpace(manager.SelectedReceiverPath)) return;
            _receiverPath = manager.SelectedReceiverPath;
            _settings.ReceiverPath = _receiverPath;
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
            process.Exited += (_, _) =>
            {
                try
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (launch == _launchId)
                        {
                            _resizeTimer.Stop();
                            _embeddedWindow = IntPtr.Zero;
                            _lastVideoWidth = _lastVideoHeight = -1;
                            VideoHost.Visibility = Visibility.Collapsed;
                            EmptyState.Visibility = Visibility.Visible;
                            UpdateStatus("Receiver stopped", Brushes.Orange);
                            _receiver = null;
                        }
                        process.Dispose();
                    });
                }
                catch (TaskCanceledException) { process.Dispose(); }
                catch (InvalidOperationException) { process.Dispose(); }
            };
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
        // Fast discovery during initial connection, slower checks while idle.
        // Keep watching after two minutes: the iPhone may connect much later.
        var waiting = Stopwatch.StartNew();
        while (launch == _launchId)
        {
            await Task.Delay(_embeddedWindow != IntPtr.Zero ? 1000 :
                waiting.Elapsed < TimeSpan.FromSeconds(30) ? 250 : 750);
            if (launch != _launchId) return;
            try
            {
                if (process.HasExited) return;
                if (_embeddedWindow != IntPtr.Zero)
                {
                    if (IsWindow(_embeddedWindow)) continue;
                    // Receiver can recreate its video window on reconnect.
                    _embeddedWindow = IntPtr.Zero;
                    _lastVideoWidth = _lastVideoHeight = -1;
                    _resizeTimer.Stop();
                    VideoHost.Visibility = Visibility.Collapsed;
                    EmptyState.Visibility = Visibility.Visible;
                    UpdateStatus("Waiting for receiver video window", Brushes.LightSkyBlue);
                    waiting.Restart();
                }

                process.Refresh();
                var hwnd = process.MainWindowHandle;
                if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) continue;

                _videoPanel.CreateControl();
                var style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
                var childStyle = (style | WS_CHILD) & ~(WS_POPUP | WS_CAPTION | WS_THICKFRAME);
                SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(childStyle));
                var previousParent = SetParent(hwnd, _videoPanel.Handle);
                if (previousParent == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
                    throw new System.ComponentModel.Win32Exception(
                        Marshal.GetLastWin32Error(), "Could not attach receiver window");

                _embeddedWindow = hwnd;
                _lastVideoWidth = _lastVideoHeight = -1;
                VideoHost.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                UpdateVideoLayout();
                // Wait until WPF has measured the now-visible WindowsFormsHost.
                // Immediately resizing a not-yet-arranged host can leave a blank viewport.
                await Dispatcher.InvokeAsync(() =>
                {
                    if (launch != _launchId || _embeddedWindow != hwnd) return;
                    VideoHost.UpdateLayout();
                    _resizeTimer.Stop();
                    ResizeVideo(force: true);
                    ScheduleVideoResize();
                    UpdateStatus("Receiver embedded · F6 rotate layout", Brushes.LightGreen);
                }, DispatcherPriority.Loaded);
            }
            catch (InvalidOperationException) when (launch != _launchId) { return; }
            catch (ObjectDisposedException) { return; }
            catch (Exception ex)
            {
                _embeddedWindow = IntPtr.Zero;
                VideoHost.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
                UpdateStatus("Embedding failed", Brushes.OrangeRed);
                MessageBox.Show("Receiver is running, but embedding failed: " + ex.Message, "Airo");
                return;
            }
        }
    }
    private void StopReceiver()
    {
        ++_launchId;
        _resizeTimer.Stop();
        _lastVideoWidth = _lastVideoHeight = -1;
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
        _settings.ReceiverPath = _receiverPath;
        try { _settings.Save(); }
        catch (Exception ex)
        {
            MessageBox.Show("Receiver selected, but saving its path failed: " + ex.Message, "Airo");
        }
        UpdateStatus("Receiver configured", Brushes.LightSkyBlue);
    }
    private void Capture_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Screenshots and recording are planned for a later beta.", "Capture");
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SavedSettings is null) return;
        _settings = dialog.SavedSettings;
        _receiverPath = _settings.ReceiverPath;
        _wideLayout = _settings.WideLayout;
        Topmost = _settings.AlwaysOnTop;
        WindowState = _settings.StartMaximized
            ? System.Windows.WindowState.Maximized
            : System.Windows.WindowState.Normal;
        UpdateVideoLayout();
    }
    private void About_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Airo Beta 0.3\n\nExperimental UxPlay window embedding. Not yet verified on all Windows builds. Audio, capture and reliable connection detection are not integrated.", "About Airo");
}