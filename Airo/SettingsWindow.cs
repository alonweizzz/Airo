using System;
using System.IO;
using Wpf = System.Windows;
using Controls = System.Windows.Controls;
using Media = System.Windows.Media;
using Shapes = System.Windows.Shapes;
using Input = System.Windows.Input;
using Win32 = Microsoft.Win32;

namespace Airo;

/// <summary>Dark, card-based preferences window. No external UI packages required.</summary>
public sealed class SettingsWindow : Wpf.Window
{
    private readonly Controls.TextBox _receiverPath;
    private readonly Controls.CheckBox _startMaximized;
    private readonly Controls.CheckBox _alwaysOnTop;
    private readonly Controls.Button _portraitButton;
    private readonly Controls.Button _wideButton;
    private bool _wideLayout;
    private readonly Controls.ComboBox _videoProfile;

    public AiroSettings? SavedSettings { get; private set; }

    private static Media.Brush Paint(string hex) =>
        new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(hex)!);

    private static readonly Media.Brush PrimaryText = Paint("#EFF6FF");
    private static readonly Media.Brush MutedText = Paint("#A1B5CF");
    private static readonly Media.Brush Accent = Paint("#72C7F5");

    private static Controls.TextBlock Text(string value, double size = 13, bool strong = false,
        Media.Brush? color = null) => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = strong ? Wpf.FontWeights.SemiBold : Wpf.FontWeights.Normal,
        Foreground = color ?? PrimaryText,
        TextWrapping = Wpf.TextWrapping.Wrap,
        VerticalAlignment = Wpf.VerticalAlignment.Center
    };

    private static Wpf.Thickness Inset(double all) => new(all);

    private static Controls.ControlTemplate RoundedButtonTemplate(double radius)
    {
        var template = new Controls.ControlTemplate(typeof(Controls.Button));
        var border = new Wpf.FrameworkElementFactory(typeof(Controls.Border));
        border.SetValue(Controls.Border.BackgroundProperty,
            new Wpf.TemplateBindingExtension(Controls.Control.BackgroundProperty));
        border.SetValue(Controls.Border.BorderBrushProperty,
            new Wpf.TemplateBindingExtension(Controls.Control.BorderBrushProperty));
        border.SetValue(Controls.Border.BorderThicknessProperty,
            new Wpf.TemplateBindingExtension(Controls.Control.BorderThicknessProperty));
        border.SetValue(Controls.Border.CornerRadiusProperty, new Wpf.CornerRadius(radius));
        var content = new Wpf.FrameworkElementFactory(typeof(Controls.ContentPresenter));
        content.SetValue(Controls.ContentPresenter.HorizontalAlignmentProperty, Wpf.HorizontalAlignment.Center);
        content.SetValue(Controls.ContentPresenter.VerticalAlignmentProperty, Wpf.VerticalAlignment.Center);
        content.SetValue(Controls.ContentPresenter.MarginProperty,
            new Wpf.TemplateBindingExtension(Controls.Control.PaddingProperty));
        border.AppendChild(content);
        template.VisualTree = border;
        return template;
    }

    private static Controls.Button Button(string title, bool primary = false) => new()
    {
        Content = title,
        Height = 40,
        MinWidth = 94,
        Padding = new Wpf.Thickness(18, 0, 18, 0),
        FontSize = 13,
        FontWeight = Wpf.FontWeights.SemiBold,
        Background = Paint(primary ? "#2B80CA" : "#203651"),
        Foreground = PrimaryText,
        BorderBrush = Paint(primary ? "#4DA6E9" : "#385574"),
        BorderThickness = new Wpf.Thickness(1),
        Cursor = Input.Cursors.Hand,
        Template = RoundedButtonTemplate(12)
    };

    private static Controls.Border Card()
    {
        var card = new Controls.Border
        {
            CornerRadius = new Wpf.CornerRadius(18),
            Background = Paint("#14233B"),
            BorderBrush = Paint("#2A4261"),
            BorderThickness = new Wpf.Thickness(1),
            Padding = Inset(20),
            Margin = new Wpf.Thickness(0, 0, 0, 14)
        };
        return card;
    }

    private static Controls.StackPanel Section(Controls.Border card, string symbol, string title, string subtitle)
    {
        var body = new Controls.StackPanel();
        card.Child = body;
        var heading = new Controls.StackPanel
        {
            Orientation = Controls.Orientation.Horizontal,
            Margin = new Wpf.Thickness(0, 0, 0, 5)
        };
        var icon = Text(symbol, 17, true, Accent);
        icon.Width = 34;
        icon.FontFamily = new Media.FontFamily("Segoe MDL2 Assets");
        heading.Children.Add(icon);
        heading.Children.Add(Text(title, 16, true));
        body.Children.Add(heading);
        var description = Text(subtitle, 12, false, MutedText);
        description.Margin = new Wpf.Thickness(34, 0, 0, 18);
        body.Children.Add(description);
        return body;
    }

    private static Controls.Border Divider() => new()
    {
        Height = 1,
        Background = Paint("#29405C"),
        Margin = new Wpf.Thickness(0, 12, 0, 12)
    };

    private static Controls.CheckBox Option(string label, string hint, bool enabled)
    {
        var labelStack = new Controls.StackPanel();
        labelStack.Children.Add(Text(label, 13, true));
        var detail = Text(hint, 11, false, MutedText);
        detail.Margin = new Wpf.Thickness(0, 4, 0, 0);
        labelStack.Children.Add(detail);
        return new Controls.CheckBox
        {
            Content = labelStack,
            IsChecked = enabled,
            Foreground = PrimaryText,
            VerticalContentAlignment = Wpf.VerticalAlignment.Center,
            Cursor = Input.Cursors.Hand,
            Padding = new Wpf.Thickness(2, 4, 0, 4)
        };
    }

    public SettingsWindow(AiroSettings current)
    {
        Title = "Airo Beta 0.3 — Settings";
        Width = 710;
        Height = 725;
        MinWidth = 590;
        MinHeight = 570;
        WindowStartupLocation = Wpf.WindowStartupLocation.CenterOwner;
        Background = Paint("#0B1427");
        Foreground = PrimaryText;
        FontFamily = new Media.FontFamily("Segoe UI");

        var root = new Controls.DockPanel();
        Content = root;

        // Fixed footer stays visible while the settings cards scroll.
        var footer = new Controls.Border
        {
            Background = Paint("#0D1B30"),
            BorderBrush = Paint("#2B4564"),
            BorderThickness = new Wpf.Thickness(0, 1, 0, 0),
            Padding = new Wpf.Thickness(30, 15, 30, 15)
        };
        Controls.DockPanel.SetDock(footer, Controls.Dock.Bottom);
        root.Children.Add(footer);
        var footerRow = new Controls.DockPanel { LastChildFill = false };
        footer.Child = footerRow;
        var version = Text("AIRO  /  BETA 0.3", 11, true, MutedText);
        version.VerticalAlignment = Wpf.VerticalAlignment.Center;
        footerRow.Children.Add(version);
        var actions = new Controls.StackPanel
        {
            Orientation = Controls.Orientation.Horizontal,
            HorizontalAlignment = Wpf.HorizontalAlignment.Right
        };
        Controls.DockPanel.SetDock(actions, Controls.Dock.Right);
        footerRow.Children.Add(actions);
        var cancel = Button("Cancel");
        cancel.Margin = new Wpf.Thickness(0, 0, 9, 0);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        actions.Children.Add(cancel);
        var save = Button("Save changes", true);
        save.Click += (_, _) => Save();
        actions.Children.Add(save);

        var scroll = new Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Controls.ScrollBarVisibility.Disabled,
            Padding = new Wpf.Thickness(30, 26, 30, 18)
        };
        root.Children.Add(scroll);
        var body = new Controls.StackPanel();
        scroll.Content = body;

        var hero = new Controls.StackPanel { Margin = new Wpf.Thickness(0, 0, 0, 24) };
        body.Children.Add(hero);
        var headingRow = new Controls.StackPanel { Orientation = Controls.Orientation.Horizontal };
        hero.Children.Add(headingRow);
        var brand = new Controls.Border
        {
            Width = 46,
            Height = 46,
            CornerRadius = new Wpf.CornerRadius(14),
            Background = Paint("#234C79"),
            BorderBrush = Paint("#487EA8"),
            BorderThickness = new Wpf.Thickness(1),
            Margin = new Wpf.Thickness(0, 0, 14, 0),
            Child = new Controls.TextBlock
            {
                Text = "\uE713",
                FontFamily = new Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 23,
                Foreground = Accent,
                HorizontalAlignment = Wpf.HorizontalAlignment.Center,
                VerticalAlignment = Wpf.VerticalAlignment.Center
            }
        };
        headingRow.Children.Add(brand);
        var headingText = new Controls.StackPanel { VerticalAlignment = Wpf.VerticalAlignment.Center };
        headingText.Children.Add(Text("Preferences", 25, true));
        var tagline = Text("Make Airo work the way you like.", 12, false, MutedText);
        tagline.Margin = new Wpf.Thickness(0, 3, 0, 0);
        headingText.Children.Add(tagline);
        headingRow.Children.Add(headingText);

        // Receiver card.
        var receiverCard = Card();
        body.Children.Add(receiverCard);
        var receiver = Section(receiverCard, "\uE8B7", "AirPlay receiver",
            "Manage the receiver installed on your Windows PC.");
        var receiverRow = new Controls.DockPanel { LastChildFill = true };
        receiver.Children.Add(receiverRow);
        var browse = Button("Browse");
        browse.Margin = new Wpf.Thickness(10, 0, 0, 0);
        Controls.DockPanel.SetDock(browse, Controls.Dock.Right);
        receiverRow.Children.Add(browse);
        _receiverPath = new Controls.TextBox
        {
            Text = current.ReceiverPath,
            IsReadOnly = true,
            ToolTip = current.ReceiverPath,
            Background = Paint("#0D1A2D"),
            Foreground = Paint("#D2E9FF"),
            BorderBrush = Paint("#355578"),
            BorderThickness = new Wpf.Thickness(1),
            Padding = new Wpf.Thickness(12, 10, 12, 10),
            VerticalContentAlignment = Wpf.VerticalAlignment.Center,
            Height = 40,
            FontSize = 12
        };
        receiverRow.Children.Add(_receiverPath);
        var manager = Button("Receiver Manager", true);
        manager.Margin = new Wpf.Thickness(0, 12, 0, 0);
        manager.Click += (_, _) =>
        {
            var currentSelection = AiroSettings.Load();
            currentSelection.ReceiverPath = _receiverPath.Text;
            var window = new ReceiverManagerWindow(currentSelection) { Owner = this };
            if (window.ShowDialog() == true && window.SelectedReceiverPath is not null)
            {
                _receiverPath.Text = window.SelectedReceiverPath;
                _receiverPath.ToolTip = window.SelectedReceiverPath;
            }
        };
        receiver.Children.Add(manager);
        browse.Click += (_, _) =>
        {
            var picker = new Win32.OpenFileDialog
            {
                Title = "Choose your installed UxPlay receiver",
                Filter = "Windows executables (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (picker.ShowDialog(this) == true)
            {
                _receiverPath.Text = picker.FileName;
                _receiverPath.ToolTip = picker.FileName;
            }
        };

        // Startup card.
        var startupCard = Card();
        body.Children.Add(startupCard);
        var startup = Section(startupCard, "\uE7E8", "Startup & window",
            "Choose how Airo appears when you open it.");
        _startMaximized = Option("Open Airo maximized",
            "Start with a larger viewing area.", current.StartMaximized);
        startup.Children.Add(_startMaximized);
        startup.Children.Add(Divider());
        _alwaysOnTop = Option("Keep Airo above other windows",
            "Keep your mirrored screen visible while multitasking.", current.AlwaysOnTop);
        startup.Children.Add(_alwaysOnTop);

        // Layout card with two clear choices rather than the default white combo box.
        var videoCard = Card();
        body.Children.Add(videoCard);
        var video = Section(videoCard, "\uE714", "Video layout",
            "Pick your preferred screen shape. F6 switches modes while mirroring.");
        var choices = new Controls.Grid();
        choices.ColumnDefinitions.Add(new Controls.ColumnDefinition());
        choices.ColumnDefinitions.Add(new Controls.ColumnDefinition());
        video.Children.Add(choices);
        _portraitButton = Button("▯   Portrait  ·  iPhone");
        _portraitButton.Height = 48;
        _portraitButton.Margin = new Wpf.Thickness(0, 0, 6, 0);
        choices.Children.Add(_portraitButton);
        _wideButton = Button("▭   Wide  ·  16:9");
        _wideButton.Height = 48;
        _wideButton.Margin = new Wpf.Thickness(6, 0, 0, 0);
        Controls.Grid.SetColumn(_wideButton, 1);
        choices.Children.Add(_wideButton);
        _portraitButton.Click += (_, _) => { _wideLayout = false; RefreshLayoutButtons(); };
        _wideButton.Click += (_, _) => { _wideLayout = true; RefreshLayoutButtons(); };
        _wideLayout = current.WideLayout;
        RefreshLayoutButtons();

        // Receiver options are opt-in: the tested command line remains the default.
        var performanceCard = Card();
        body.Children.Add(performanceCard);
        var performance = Section(performanceCard, "\uE7F4", "Video performance (experimental)",
            "These settings apply when you next start the receiver. Results depend on your UxPlay build and iPhone.");
        _videoProfile = new Controls.ComboBox
        {
            Height = 42,
            Background = Paint("#142B47"),
            Foreground = Paint("#101A2A"),
            BorderBrush = Paint("#547DA4"),
            BorderThickness = new Wpf.Thickness(1),
            Padding = new Wpf.Thickness(10, 5, 10, 5)
        };
        _videoProfile.Items.Add("Balanced — current stable behavior");
        _videoProfile.Items.Add("Smooth — request up to 60 FPS");
        _videoProfile.Items.Add("Responsive — try lower display latency");
        _videoProfile.SelectedIndex = current.VideoProfile switch
        {
            "Smooth" => 1,
            "Responsive" => 2,
            _ => 0
        };
        performance.Children.Add(_videoProfile);
        var performanceNote = Text("Smooth requests -fps 60 with timestamp synchronization. Responsive uses -vsync no; this may affect audio/video sync. If playback becomes worse, select Balanced.", 11, false, MutedText);
        performanceNote.Margin = new Wpf.Thickness(0, 10, 0, 0);
        performance.Children.Add(performanceNote);
    }

    private void RefreshLayoutButtons()
    {
        _portraitButton.Background = Paint(!_wideLayout ? "#215A91" : "#0F1D32");
        _portraitButton.BorderBrush = Paint(!_wideLayout ? "#6DC3EF" : "#355270");
        _portraitButton.Foreground = Paint(!_wideLayout ? "#FFFFFF" : "#B3C7DF");
        _wideButton.Background = Paint(_wideLayout ? "#215A91" : "#0F1D32");
        _wideButton.BorderBrush = Paint(_wideLayout ? "#6DC3EF" : "#355270");
        _wideButton.Foreground = Paint(_wideLayout ? "#FFFFFF" : "#B3C7DF");
    }

    private void Save()
    {
        var next = new AiroSettings
        {
            ReceiverPath = _receiverPath.Text.Trim(),
            StartMaximized = _startMaximized.IsChecked == true,
            AlwaysOnTop = _alwaysOnTop.IsChecked == true,
            WideLayout = _wideLayout,
            VideoProfile = _videoProfile.SelectedIndex switch
            {
                1 => "Smooth",
                2 => "Responsive",
                _ => "Balanced"
            }
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
