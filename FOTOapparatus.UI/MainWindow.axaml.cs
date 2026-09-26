using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FOTOapparatus.Core.Models;
using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.UI.ViewModels;

namespace FOTOapparatus.UI;

internal partial class MainWindow : Window
{
    private readonly IScreenshotService _captureService;
    private readonly IWindowService _windowDiscoveryService;
    private readonly SchedulerService _schedulerService;
    private readonly ISettingsStore _settingsStore;
    private readonly MainWindowViewModel _viewModel;
    private readonly bool _startHidden;
    private bool _startupCompleted;
    private bool _isExiting;
    private bool _autostartToggleInProgress;
    private AppSettings _currentSettings;

    internal bool StartHidden => _startHidden;

    public MainWindow(
        IScreenshotService captureService,
        IWindowService windowDiscoveryService,
        SchedulerService schedulerService,
        ISettingsStore settingsStore,
        AppSettings initialSettings)
    {
        _captureService = captureService;
        _windowDiscoveryService = windowDiscoveryService;
        _schedulerService = schedulerService;
        _settingsStore = settingsStore;
        _currentSettings = initialSettings.Clone();

        var args = Environment.GetCommandLineArgs();
        _startHidden = args.Contains("--hidden");

        InitializeComponent();

        _viewModel = new MainWindowViewModel
        {
            AutostartSupported = true,
        };
        _viewModel.Load(_currentSettings);
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.WindowTitle))
            {
                Title = _viewModel.WindowTitle;
            }
        };

        DataContext = _viewModel;
        Title = _viewModel.WindowTitle;
    }

    public void ShowFromExternalRequest()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
    }

    public async Task ExitApplicationAsync()
    {
        if (!await HandlePendingChangesAsync("kilépés előtt"))
        {
            return;
        }

        _isExiting = true;
        await _schedulerService.StopAsync();
        Close();

        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }



    private async void Window_OnOpened(object? sender, EventArgs e)
        => await InitializeForStartupAsync();

    internal async Task InitializeForStartupAsync()
    {
        if (_startupCompleted)
        {
            return;
        }

        _startupCompleted = true;
        await RefreshWindowsAsync();
        SynchronizeAutostartState();
        _schedulerService.Start();
        _schedulerService.UpdateSettings(_currentSettings);

        _viewModel.StatusMessage = _startHidden && !IsVisible
            ? "Az alkalmazás a háttérben fut."
            : "Alkalmazás betöltve.";
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        await HideToTrayAsync();
    }

    private async Task HideToTrayAsync()
    {
        if (!await HandlePendingChangesAsync("tálcára helyezés előtt"))
        {
            return;
        }

        Hide();
        _viewModel.StatusMessage = "Az alkalmazás a háttérben fut.";
    }

    private async Task<bool> HandlePendingChangesAsync(string contextText)
    {
        if (!_viewModel.IsDirty)
        {
            return true;
        }

        var result = await ShowDialogAsync(
            "Nem mentett változások",
            $"Vannak nem mentett beállítások. Szeretnéd menteni őket {contextText}?",
            new DialogButtonDefinition("Mentés", "save", IsDefault: true),
            new DialogButtonDefinition("Elvetés", "discard"),
            new DialogButtonDefinition("Mégse", "cancel"));

        if (string.Equals(result, "save", StringComparison.OrdinalIgnoreCase))
        {
            return await SaveSettingsAsync();
        }

        if (string.Equals(result, "discard", StringComparison.OrdinalIgnoreCase))
        {
            _viewModel.MarkDiscarded();
            return true;
        }

        return false;
    }

    private async Task<bool> SaveSettingsAsync()
    {
        var settings = _viewModel.BuildSettings();
        if (!await EnsureSavePathIsReadyAsync(settings))
        {
            return false;
        }

        try
        {
            await _settingsStore.SaveAsync(settings);
            _currentSettings = settings.Clone();
            _schedulerService.UpdateSettings(_currentSettings);
            _viewModel.MarkSaved("Beállítások sikeresen elmentve.");
            return true;
        }
        catch (Exception ex)
        {
            await ShowDialogAsync(
                "Mentési hiba",
                $"Nem sikerült menteni a konfigurációt:\n{ex.Message}",
                new DialogButtonDefinition("OK", "ok", IsDefault: true));
            return false;
        }
    }

    private async Task RefreshWindowsAsync()
    {
        var windows = await _windowDiscoveryService.GetWindowsAsync();
        _viewModel.SetAvailableWindows(windows);
        _viewModel.StatusMessage = windows.Count == 0
            ? "Nem sikerült ablaklistát lekérni a rendszerből."
            : "Ablaklista frissítve.";
    }

    private void SynchronizeAutostartState()
    {
        if (!AutostartManager.IsSupported)
        {
            return;
        }

        var preferredState = _currentSettings.AutostartEnabled;
        var actualState = AutostartManager.IsEnabled();
        if (preferredState != actualState)
        {
            if (AutostartManager.SetEnabled(preferredState))
            {
                actualState = preferredState;
            }
        }

        _autostartToggleInProgress = true;
        _viewModel.UpdateAutostartFromSystem(actualState);
        _autostartToggleInProgress = false;
    }

    private async void FolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!StorageProvider.CanPickFolder)
        {
            await ShowDialogAsync(
                "Nem támogatott művelet",
                "A rendszer nem támogatja a mappaválasztó megnyitását.",
                new DialogButtonDefinition("OK", "ok", IsDefault: true));
            return;
        }

        IStorageFolder? startLocation = null;
        if (!string.IsNullOrWhiteSpace(_viewModel.SavePath))
        {
            startLocation = await StorageProvider.TryGetFolderFromPathAsync(_viewModel.SavePath);
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = startLocation,
                Title = "Mentési mappa",
            });

        var selectedPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            _viewModel.SavePath = selectedPath;
            _viewModel.StatusMessage = "Mentési mappa frissítve.";
        }
    }

    private async void RefreshWindowsButton_OnClick(object? sender, RoutedEventArgs e)
        => await RunSafeUiActionAsync(
            RefreshWindowsAsync,
            "Ablaklista hiba",
            "Nem sikerült frissíteni az ablaklistát.");

    private async void DiscordSettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await RunSafeUiActionAsync(
            async () =>
            {
                var dialog = new DiscordSettingsWindow(_viewModel.DiscordSettings.Clone(), _windowDiscoveryService);
                var result = await dialog.ShowDialog<DiscordSettings?>(this);
                if (result is not null)
                {
                    _viewModel.UpdateDiscordSettings(result);
                    _viewModel.StatusMessage = "Discord beállítások frissítve.";
                }
            },
            "Discord beállítási hiba",
            "Nem sikerült megnyitni a Discord beállításokat.");
    }

    private async void SelectAreaButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await RunSafeUiActionAsync(
            async () =>
            {
                _viewModel.StatusMessage = "Képernyő-előnézet készítése...";
                var previewPath = await CaptureSelectionPreviewAsync();
                if (previewPath is null)
                {
                    await ShowDialogAsync(
                        "Területkijelölési hiba",
                        "Nem sikerült képernyő-előnézetet készíteni a kijelöléshez. Ellenőrizd, hogy a `gnome-screenshot` elérhető-e, és a rendszer engedi-e a képernyőképkészítést.",
                        new DialogButtonDefinition("OK", "ok", IsDefault: true));
                    _viewModel.StatusMessage = "Nem sikerült elindítani a területkijelölést.";
                    return;
                }

                RectSettings? result;
                try
                {
                    var selectorWindow = new SelectionOverlayWindow(previewPath, _viewModel.CustomArea);
                    result = await selectorWindow.ShowDialog<RectSettings?>(this);
                }
                finally
                {
                    File.Delete(previewPath);
                }

                if (result is not null)
                {
                    _viewModel.SetCustomArea(result);
                    _viewModel.StatusMessage = "Egyéni terület kiválasztva.";
                }
                else
                {
                    _viewModel.StatusMessage = "Területkijelölés megszakítva.";
                }
            },
            "Területkijelölési hiba",
            "Nem sikerült elindítani a területkijelölést.");
    }

    private void AddScheduleButton_OnClick(object? sender, RoutedEventArgs e)
        => _viewModel.AddSchedule();

    private void RemoveScheduleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ScheduleSettingsViewModel schedule)
        {
            _viewModel.RemoveSchedule(schedule);
        }
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        => await RunSafeUiActionAsync(
            async () => await SaveSettingsAsync(),
            "Mentési hiba",
            "Nem sikerült elmenteni a beállításokat.");

    private async void TestButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await RunSafeUiActionAsync(
            async () =>
            {
                TestButton.IsEnabled = false;
                try
                {
                    var settings = _viewModel.BuildSettings();
                    if (!await EnsureSavePathIsReadyAsync(settings))
                    {
                        return;
                    }

                    if (settings.CaptureType == CaptureTypes.Program && string.IsNullOrWhiteSpace(settings.TargetWindow))
                    {
                        await ShowDialogAsync(
                            "Hiányzó adat",
                            "Válassz ki egy programablakot a teszt futtatása előtt.",
                            new DialogButtonDefinition("OK", "ok", IsDefault: true));
                        return;
                    }

                    if (settings.CaptureType == CaptureTypes.Discord
                        && string.IsNullOrWhiteSpace(settings.DiscordSettings.WindowTitle)
                        && string.IsNullOrWhiteSpace(settings.DiscordSettings.WindowClassName))
                    {
                        await ShowDialogAsync(
                            "Hiányzó adat",
                            "Válaszd ki a Discord ablakot a beállításoknál.",
                            new DialogButtonDefinition("OK", "ok", IsDefault: true));
                        return;
                    }

                    _viewModel.StatusMessage = "Képkészítés folyamatban...";
                    var savedPath = await _captureService.CaptureAsync(settings, "Teszt", forceStayForeground: true);
                    if (savedPath is null)
                    {
                        await ShowDialogAsync(
                            "Képkészítési hiba",
                            "Nem sikerült elkészíteni a tesztképet. Ellenőrizd, hogy Linux alatt elérhető-e a `gnome-screenshot`, illetve hogy X11 környezetben fut-e az alkalmazás.",
                            new DialogButtonDefinition("OK", "ok", IsDefault: true));
                        return;
                    }

                    _viewModel.StatusMessage = $"Tesztkép elkészült: {savedPath}";
                }
                finally
                {
                    TestButton.IsEnabled = true;
                }
            },
            "Képkészítési hiba",
            "Váratlan hiba történt a tesztkép készítése közben.");
    }

    private async void AutostartCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_startupCompleted || _autostartToggleInProgress || !AutostartManager.IsSupported)
        {
            return;
        }

        var desiredState = _viewModel.AutostartEnabled;
        if (AutostartManager.SetEnabled(desiredState))
        {
            _viewModel.StatusMessage = desiredState
                ? "Autostart engedélyezve."
                : "Autostart letiltva.";
            return;
        }

        _autostartToggleInProgress = true;
        _viewModel.UpdateAutostartFromSystem(AutostartManager.IsEnabled());
        _autostartToggleInProgress = false;

        await ShowDialogAsync(
            "Autostart hiba",
            "Nem sikerült módosítani az autostart beállítást a rendszerben.",
            new DialogButtonDefinition("OK", "ok", IsDefault: true));
    }

    private async Task<string?> ShowDialogAsync(
        string title,
        string message,
        params DialogButtonDefinition[] buttons)
    {
        var dialog = new MessageDialogWindow(title, message, buttons);
        return await dialog.ShowDialog<string?>(this);
    }

    private async Task<string?> CaptureSelectionPreviewAsync()
    {
        var wasVisible = IsVisible;
        var previousWindowState = WindowState;

        try
        {
            if (wasVisible)
            {
                Hide();
                await Task.Delay(250);
            }

            return await _captureService.CaptureSelectionPreviewAsync();
        }
        finally
        {
            if (wasVisible)
            {
                Show();
                WindowState = previousWindowState == WindowState.Minimized
                    ? WindowState.Normal
                    : previousWindowState;
                Activate();
            }
        }
    }

    private async Task RunSafeUiActionAsync(
        Func<Task> action,
        string errorTitle,
        string fallbackMessage)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = fallbackMessage;
            await ShowDialogAsync(
                errorTitle,
                $"{fallbackMessage}\n\nRészletek:\n{ex.Message}",
                new DialogButtonDefinition("OK", "ok", IsDefault: true));
        }
    }

    private async Task<bool> EnsureSavePathIsReadyAsync(AppSettings settings)
    {
        var normalizedSavePath = AppSettingsDefaults.NormalizeSavePath(settings.SavePath);
        if (!string.Equals(settings.SavePath, normalizedSavePath, StringComparison.Ordinal))
        {
            settings.SavePath = normalizedSavePath;
            _viewModel.SavePath = normalizedSavePath;
            _viewModel.StatusMessage = "A mentési útvonal Linuxos alapértelmezettre lett javítva.";
        }

        try
        {
            Directory.CreateDirectory(settings.SavePath);
            return true;
        }
        catch (Exception ex)
        {
            await ShowDialogAsync(
                "Mentési mappa hiba",
                $"A mentési mappa nem használható:\n{ex.Message}",
                new DialogButtonDefinition("OK", "ok", IsDefault: true));
            return false;
        }
    }
}
