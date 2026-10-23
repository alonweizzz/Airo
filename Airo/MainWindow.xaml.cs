using System.Windows;
namespace Airo;
public partial class MainWindow : Window
{
 public MainWindow() { InitializeComponent(); }
 private void Instructions_Click(object sender, RoutedEventArgs e) => MessageBox.Show("When an AirPlay receiver has been integrated:\n1. Connect iPhone and PC to the same local network.\n2. Open Control Center on iPhone.\n3. Tap Screen Mirroring.\n4. Choose Airo.\n\nThis preview does not yet advertise an AirPlay receiver.", "Airo setup", MessageBoxButton.OK, MessageBoxImage.Information);
 private void About_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Airo Beta 0.1 — Windows UI prototype.\nAirPlay screen mirroring is planned, not yet implemented.", "About Airo");
}