using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FOTOapparatus.Core.Models;
using FOTOapparatus.Core.Interfaces;

namespace FOTOapparatus.UI;

internal partial class DiscordSettingsWindow : Window
{
    private readonly IWindowService _windowDiscoveryService;
    private readonly DiscordSettings _settings;

    public DiscordSettingsWindow(DiscordSettings settings, IWindowService windowDiscoveryService)
    {
        _windowDiscoveryService = windowDiscoveryService;
        _settings = settings.Clone();
        InitializeComponent();
    }



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
        if (WindowComboBox.SelectedItem is not WindowInfo selectedWindow)
        {
            return;
        }

        Close(new DiscordSettings
        {
            StayForeground = StayForegroundCheckBox.IsChecked == true,
            UseHotkey = UseHotkeyCheckBox.IsChecked == true,
            HotkeyNumber = Convert.ToInt32(HotkeyNumberInput.Value ?? 1m),
            WindowTitle = selectedWindow.Title,
            WindowClassName = selectedWindow.ClassName,
            DelayAfterHotkey = Convert.ToInt32(DelayInput.Value ?? 2m),
        });
    }

    private async Task LoadWindowsAsync()
    {
        var currentWindow = WindowComboBox.SelectedItem as WindowInfo;
        var currentTitle = currentWindow?.Title ?? _settings.WindowTitle;
        var currentClassName = currentWindow?.ClassName ?? _settings.WindowClassName;
        var windows = await _windowDiscoveryService.GetWindowsAsync();
        WindowComboBox.ItemsSource = windows;
        WindowComboBox.SelectedItem = _windowDiscoveryService.FindBestMatch(
            windows,
            currentTitle ?? string.Empty,
            currentClassName);
    }

    private void UpdateHotkeyControls()
    {
        var enabled = UseHotkeyCheckBox.IsChecked == true;
        HotkeyNumberInput.IsEnabled = enabled;
        DelayInput.IsEnabled = enabled;
    }
}
