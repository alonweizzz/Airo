using System.Windows;
namespace Airo;
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    private void Home_Click(object sender, RoutedEventArgs e) { }
    private void Devices_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("AirPlay receiver integration is coming next.\n\nWhen available, connect your iPhone and PC to the same Wi-Fi network, open Control Center → Screen Mirroring and choose Airo.", "Connect a device", MessageBoxButton.OK, MessageBoxImage.Information);
    private void Capture_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Screenshots and recordings will be enabled after a working screen mirroring connection is implemented.", "Capture — coming soon");
    private void Settings_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Device name, language, appearance and streaming quality settings are planned for a future beta.", "Settings — coming soon");
    private void About_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Airo Beta 0.1\n\nLiquid Glass UI preview for Windows.\nAirPlay mirroring is not implemented yet.", "About Airo");
}