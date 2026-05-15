using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.UI;

public partial class SelectionOverlayWindow : Window
{
    private readonly Bitmap? _previewBitmap;
    private readonly PixelSize _sourcePixelSize;
    private readonly RectSettings? _initialSelection;
    private Point _startPoint;
    private Point _currentPoint;
    private bool _isSelecting;
    private Rect _imageDisplayRect;
    private RectSettings? _selectedArea;

    public SelectionOverlayWindow()
        => InitializeComponent();

    public SelectionOverlayWindow(string screenshotPath, RectSettings? initialSelection = null)
        : this()
    {
        using var screenshotStream = File.OpenRead(screenshotPath);
        _previewBitmap = new Bitmap(screenshotStream);
        _sourcePixelSize = _previewBitmap.PixelSize;
        _initialSelection = initialSelection?.IsValid == true ? initialSelection.Clone() : null;
        PreviewImage.Source = _previewBitmap;
    }



    private void Window_OnOpened(object? sender, EventArgs e)
    {
        Activate();
        Focus();
        UpdateImageDisplayRect();
        ApplyInitialSelectionIfAvailable();
    }

    private void OverlayCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_imageDisplayRect.Width <= 0 || _imageDisplayRect.Height <= 0)
        {
            return;
        }

        var rawPosition = e.GetPosition(OverlayCanvas);
        if (!_imageDisplayRect.Contains(rawPosition))
        {
            return;
        }

        var position = ClampToImageBounds(rawPosition);
        _isSelecting = true;
        e.Pointer.Capture(OverlayCanvas);
        _startPoint = position;
        _currentPoint = _startPoint;
        _selectedArea = null;
        AcceptButton.IsEnabled = false;
        StatusText.Text = "Kijelölés folyamatban...";
        UpdateSelectionVisuals();
    }

    private void OverlayCanvas_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        _currentPoint = ClampToImageBounds(e.GetPosition(OverlayCanvas));
        UpdateSelectionVisuals();
    }

    private void OverlayCanvas_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        e.Pointer.Capture(null);
        _currentPoint = ClampToImageBounds(e.GetPosition(OverlayCanvas));
        _isSelecting = false;
        var displaySelection = NormalizeRect(_startPoint, _currentPoint);

        if (displaySelection.Width < 2 || displaySelection.Height < 2)
        {
            ClearSelection();
            return;
        }

        _selectedArea = ConvertDisplayRectToSourceRect(displaySelection);
        UpdateSelectionVisuals();
        AcceptButton.IsEnabled = _selectedArea.IsValid;
        StatusText.Text = _selectedArea.IsValid
            ? $"Kijelölve: {_selectedArea.Width} x {_selectedArea.Height}px"
            : "A kijelölés túl kicsi.";
    }

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null);
        }

        if (e.Key == Key.Enter && _selectedArea?.IsValid == true)
        {
            Close(_selectedArea.Clone());
        }
    }

    private void UpdateSelectionVisuals()
    {
        Rect selection;
        if (_isSelecting)
        {
            selection = NormalizeRect(_startPoint, _currentPoint);
        }
        else if (_selectedArea?.IsValid == true)
        {
            selection = ConvertSourceRectToDisplayRect(_selectedArea);
        }
        else
        {
            SelectionBorder.IsVisible = false;
            SelectionInfoBadge.IsVisible = false;
            return;
        }

        SelectionBorder.IsVisible = true;
        SelectionBorder.Width = selection.Width;
        SelectionBorder.Height = selection.Height;
        Canvas.SetLeft(SelectionBorder, selection.X);
        Canvas.SetTop(SelectionBorder, selection.Y);

        var sourceRect = _isSelecting
            ? ConvertDisplayRectToSourceRect(selection)
            : _selectedArea;
        DimensionText.Text = sourceRect?.IsValid == true
            ? $"{sourceRect.Width} x {sourceRect.Height}"
            : $"{(int)selection.Width} x {(int)selection.Height}";
        SelectionInfoBadge.IsVisible = true;
        Canvas.SetLeft(SelectionInfoBadge, CalculateBadgeLeft(selection));
        Canvas.SetTop(SelectionInfoBadge, CalculateBadgeTop(selection));
    }

    private static Rect NormalizeRect(Point startPoint, Point endPoint)
    {
        var left = Math.Min(startPoint.X, endPoint.X);
        var top = Math.Min(startPoint.Y, endPoint.Y);
        var right = Math.Max(startPoint.X, endPoint.X);
        var bottom = Math.Max(startPoint.Y, endPoint.Y);
        return new Rect(left, top, right - left, bottom - top);
    }

    private void PreviewContainer_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateImageDisplayRect();
        UpdateSelectionVisuals();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        => Close(null);

    private void AcceptButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedArea?.IsValid == true)
        {
            Close(_selectedArea.Clone());
        }
    }

    private void Window_OnClosed(object? sender, EventArgs e)
        => _previewBitmap?.Dispose();

    private void ApplyInitialSelectionIfAvailable()
    {
        if (_initialSelection?.IsValid != true)
        {
            return;
        }

        _selectedArea = _initialSelection.Clone();
        AcceptButton.IsEnabled = true;
        StatusText.Text = $"Aktuális kijelölés: {_selectedArea.Width} x {_selectedArea.Height}px";
        UpdateSelectionVisuals();
    }

    private void ClearSelection()
    {
        _selectedArea = null;
        AcceptButton.IsEnabled = false;
        StatusText.Text = "A kijelölés túl kicsi. Húzd ki újra a kívánt területet.";
        SelectionBorder.IsVisible = false;
        SelectionInfoBadge.IsVisible = false;
    }

    private void UpdateImageDisplayRect()
    {
        var availableWidth = PreviewContainer.Bounds.Width;
        var availableHeight = PreviewContainer.Bounds.Height;
        if (availableWidth <= 0 || availableHeight <= 0 || _sourcePixelSize.Width <= 0 || _sourcePixelSize.Height <= 0)
        {
            _imageDisplayRect = default;
            return;
        }

        var scale = Math.Min(availableWidth / _sourcePixelSize.Width, availableHeight / _sourcePixelSize.Height);
        var displayWidth = _sourcePixelSize.Width * scale;
        var displayHeight = _sourcePixelSize.Height * scale;
        var displayX = (availableWidth - displayWidth) / 2d;
        var displayY = (availableHeight - displayHeight) / 2d;
        _imageDisplayRect = new Rect(displayX, displayY, displayWidth, displayHeight);
    }

    private Point ClampToImageBounds(Point position)
    {
        if (_imageDisplayRect.Width <= 0 || _imageDisplayRect.Height <= 0)
        {
            return position;
        }

        return new Point(
            Math.Clamp(position.X, _imageDisplayRect.X, _imageDisplayRect.Right),
            Math.Clamp(position.Y, _imageDisplayRect.Y, _imageDisplayRect.Bottom));
    }

    private RectSettings ConvertDisplayRectToSourceRect(Rect displayRect)
    {
        var scaleX = _sourcePixelSize.Width / _imageDisplayRect.Width;
        var scaleY = _sourcePixelSize.Height / _imageDisplayRect.Height;

        var x = (int)Math.Round((displayRect.X - _imageDisplayRect.X) * scaleX);
        var y = (int)Math.Round((displayRect.Y - _imageDisplayRect.Y) * scaleY);
        var width = (int)Math.Round(displayRect.Width * scaleX);
        var height = (int)Math.Round(displayRect.Height * scaleY);

        x = Math.Clamp(x, 0, Math.Max(0, _sourcePixelSize.Width - 1));
        y = Math.Clamp(y, 0, Math.Max(0, _sourcePixelSize.Height - 1));
        width = Math.Clamp(width, 1, _sourcePixelSize.Width - x);
        height = Math.Clamp(height, 1, _sourcePixelSize.Height - y);

        return new RectSettings
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };
    }

    private Rect ConvertSourceRectToDisplayRect(RectSettings sourceRect)
    {
        var scaleX = _imageDisplayRect.Width / _sourcePixelSize.Width;
        var scaleY = _imageDisplayRect.Height / _sourcePixelSize.Height;

        return new Rect(
            _imageDisplayRect.X + (sourceRect.X * scaleX),
            _imageDisplayRect.Y + (sourceRect.Y * scaleY),
            sourceRect.Width * scaleX,
            sourceRect.Height * scaleY);
    }

    private double CalculateBadgeLeft(Rect selection)
    {
        const double preferredOffset = 10;
        const double badgeWidth = 120;
        var preferredLeft = selection.Right + preferredOffset;
        var maximumLeft = Math.Max(_imageDisplayRect.X, _imageDisplayRect.Right - badgeWidth);
        return Math.Clamp(preferredLeft, _imageDisplayRect.X, maximumLeft);
    }

    private double CalculateBadgeTop(Rect selection)
    {
        const double preferredOffset = 10;
        const double badgeHeight = 30;
        var preferredTop = selection.Y - badgeHeight - preferredOffset;
        if (preferredTop >= _imageDisplayRect.Y)
        {
            return preferredTop;
        }

        var fallbackTop = selection.Bottom + preferredOffset;
        var maximumTop = Math.Max(_imageDisplayRect.Y, _imageDisplayRect.Bottom - badgeHeight);
        return Math.Clamp(fallbackTop, _imageDisplayRect.Y, maximumTop);
    }
}
