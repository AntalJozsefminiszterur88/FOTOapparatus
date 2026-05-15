using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FOTOapparatus.Avalonia.Models;
using FOTOapparatus.Avalonia.Services;

namespace FOTOapparatus.Avalonia.Windows;

internal partial class DiscordSettingsWindow : Window
{
    private readonly WindowDiscoveryService _windowDiscoveryService;
    private readonly DiscordSettings _settings;

    public DiscordSettingsWindow(DiscordSettings settings, WindowDiscoveryService windowDiscoveryService)
    {
        _windowDiscoveryService = windowDiscoveryService;
        _settings = settings.Clone();
        InitializeComponent();
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);

    private async void Window_OnOpened(object? sender, EventArgs e)
    {
        StayForegroundCheckBox.IsChecked = _settings.StayForeground;
        UseHotkeyCheckBox.IsChecked = _settings.UseHotkey;
        HotkeyNumberInput.Value = _settings.HotkeyNumber;
        DelayInput.Value = (decimal)_settings.DelayAfterHotkey;
        UpdateHotkeyControls();
        await LoadWindowsAsync();
    }

    private async void RefreshWindowsButton_OnClick(object? sender, RoutedEventArgs e)
        => await LoadWindowsAsync();

    private void UseHotkeyCheckBox_OnClick(object? sender, RoutedEventArgs e)
        => UpdateHotkeyControls();

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        => Close(null);

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedWindow = WindowComboBox.SelectedItem as WindowInfo;
        if (selectedWindow is null)
        {
            return;
        }

        Close(new DiscordSettings
        {
            StayForeground = StayForegroundCheckBox.IsChecked == true,
            UseHotkey = UseHotkeyCheckBox.IsChecked == true,
            HotkeyNumber = Convert.ToInt32(HotkeyNumberInput.Value ?? 1),
            WindowTitle = selectedWindow.Title,
            DelayAfterHotkey = Convert.ToDouble(DelayInput.Value ?? 2m),
        });
    }

    private async Task LoadWindowsAsync()
    {
        var currentTitle = (WindowComboBox.SelectedItem as WindowInfo)?.Title ?? _settings.WindowTitle;
        var windows = await _windowDiscoveryService.GetWindowsAsync();
        WindowComboBox.ItemsSource = windows;
        WindowComboBox.SelectedItem = _windowDiscoveryService.FindBestMatch(windows, currentTitle ?? string.Empty);
    }

    private void UpdateHotkeyControls()
    {
        var enabled = UseHotkeyCheckBox.IsChecked == true;
        HotkeyNumberInput.IsEnabled = enabled;
        DelayInput.IsEnabled = enabled;
    }
}
