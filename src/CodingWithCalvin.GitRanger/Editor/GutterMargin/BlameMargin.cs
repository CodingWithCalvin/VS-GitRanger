using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CodingWithCalvin.GitRanger.Core.Models;
using CodingWithCalvin.GitRanger.Options;
using CodingWithCalvin.GitRanger.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace CodingWithCalvin.GitRanger.Editor.GutterMargin;

/// <summary>
/// Margin that displays blame information in the gutter.
/// </summary>
internal sealed class BlameMargin : Canvas, IWpfTextViewMargin
{
    /// <summary>
    /// The name of this margin.
    /// </summary>
    public const string MarginName = "GitRangerBlameMargin";

    private readonly IWpfTextView _view;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly IGitService _gitService;
    private readonly IBlameService _blameService;
    private readonly IThemeService _themeService;
    private readonly IOutputPaneService _outputPane;
    private readonly Popup _tooltipPopup;
    private IReadOnlyList<BlameLineInfo> _blameData = Array.Empty<BlameLineInfo>();
    private string? _currentFilePath;
    private bool _isLoading;
    private bool _isDisposed;
    private int _currentTooltipLine = -1;

    /// <summary>
    /// Creates a new blame margin for the given text view.
    /// </summary>
    public BlameMargin(
        IWpfTextView view,
        ITextDocumentFactoryService textDocumentFactoryService,
        IGitService gitService,
        IBlameService blameService,
        IThemeService themeService,
        IOutputPaneService outputPane)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _textDocumentFactoryService = textDocumentFactoryService;
        _gitService = gitService;
        _blameService = blameService;
        _themeService = themeService;
        _outputPane = outputPane;

        _outputPane.WriteVerbose("BlameMargin created");

        var options = GeneralOptions.Instance;
        Width = options?.GutterWidth ?? 40;
        ClipToBounds = true;
        Background = Brushes.Transparent;

        _tooltipPopup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Mouse,
            StaysOpen = true,
            IsHitTestVisible = false,
            PopupAnimation = PopupAnimation.None
        };

        _view.LayoutChanged += OnLayoutChanged;
        _view.Closed += OnViewClosed;
        _blameService.BlameLoaded += OnBlameLoaded;
        GeneralOptions.Saved += OnOptionsSaved;

        PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;

        LoadBlameData();
    }

    #region IWpfTextViewMargin Members

    public FrameworkElement VisualElement => this;

    public double MarginSize => Width;

    public bool Enabled
    {
        get
        {
            var options = GeneralOptions.Instance;
            return options?.EnableBlameGutter ?? true;
        }
    }

    public ITextViewMargin? GetTextViewMargin(string marginName)
    {
        return marginName == MarginName ? this : null;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _tooltipPopup.IsOpen = false;

        _view.LayoutChanged -= OnLayoutChanged;
        _view.Closed -= OnViewClosed;
        _blameService.BlameLoaded -= OnBlameLoaded;
        GeneralOptions.Saved -= OnOptionsSaved;
    }

    #endregion

    private void OnViewClosed(object sender, EventArgs e) => Dispose();

    private void OnOptionsSaved(GeneralOptions options)
    {
        _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Width = options.GutterWidth;
            InvalidateVisual();
        });
    }

    private void OnBlameLoaded(object? sender, BlameLoadedEventArgs e)
    {
        if (string.Equals(e.FilePath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _blameData = e.Lines;
                _isLoading = false;
                InvalidateVisual();
            });
        }
    }

    private void LoadBlameData()
    {
        var filePath = GetFilePath();
        if (string.IsNullOrEmpty(filePath))
            return;

        if (string.Equals(filePath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
            return;

        _currentFilePath = filePath;
        _isLoading = true;

        if (!_gitService.TryOpenRepository(filePath))
        {
            _isLoading = false;
            return;
        }

        _blameService.LoadBlameInBackground(filePath);
    }

    private string? GetFilePath()
    {
        if (_textDocumentFactoryService.TryGetTextDocument(_view.TextDataModel.DocumentBuffer, out var textDocument))
        {
            return textDocument.FilePath;
        }

        if (_view.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument doc))
        {
            return doc.FilePath;
        }

        return null;
    }

    private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
    {
        var currentPath = GetFilePath();
        if (!string.Equals(currentPath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
        {
            _currentFilePath = null;
            _blameData = Array.Empty<BlameLineInfo>();
            LoadBlameData();
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var options = GeneralOptions.Instance;
        if (options == null || !options.EnableBlameGutter)
            return;

        if (_isLoading || _blameData.Count == 0)
            return;

        var backgroundColor = _themeService.GetBlameBackgroundColor();

        drawingContext.DrawRectangle(
            new SolidColorBrush(backgroundColor),
            null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        foreach (var line in _view.TextViewLines)
        {
            if (line.VisibilityState == VisibilityState.Unattached)
                continue;

            var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;
            var blameInfo = _blameData.FirstOrDefault(b => b.LineNumber == lineNumber);
            if (blameInfo == null)
                continue;

            DrawLineIndicator(drawingContext, line, blameInfo, options);
        }
    }

    private void DrawLineIndicator(
        DrawingContext drawingContext,
        ITextViewLine line,
        BlameLineInfo blameInfo,
        GeneralOptions options)
    {
        var y = line.TextTop - _view.ViewportTop;
        var height = line.TextHeight;

        Color color;
        switch (options.BlameColorMode)
        {
            case ColorMode.Author:
                color = _themeService.GetAuthorColor(blameInfo.AuthorEmail);
                color = _themeService.AdjustForTheme(color);
                break;

            case ColorMode.Age:
                color = _themeService.GetAgeHeatMapColor(blameInfo.AgeDays, options.MaxAgeDays);
                break;

            default:
                color = Colors.Gray;
                break;
        }

        if (options.ShowAgeBars)
        {
            var maxWidth = ActualWidth - 4;
            var ageRatio = Math.Min(1.0, (double)blameInfo.AgeDays / options.MaxAgeDays);
            var barWidth = maxWidth * (1.0 - ageRatio);

            var brush = new SolidColorBrush(color);
            brush.Freeze();

            drawingContext.DrawRectangle(
                brush,
                null,
                new Rect(2, y + 2, barWidth, height - 4));
        }
        else
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            drawingContext.DrawRectangle(
                brush,
                null,
                new Rect(2, y + 2, 4, height - 4));
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentTooltipLine > 0)
        {
            var blameInfo = _blameData.FirstOrDefault(b => b.LineNumber == _currentTooltipLine);
            if (blameInfo != null)
            {
                System.Windows.Clipboard.SetText(blameInfo.CommitSha);
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await Community.VisualStudio.Toolkit.VS.StatusBar.ShowMessageAsync(
                        $"Git Ranger: Copied commit SHA {blameInfo.ShortSha} to clipboard");
                });
                e.Handled = true;
            }
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        var blameInfo = GetBlameInfoAtPosition(position);

        if (blameInfo != null)
        {
            Cursor = Cursors.Hand;

            if (_currentTooltipLine != blameInfo.LineNumber)
            {
                _currentTooltipLine = blameInfo.LineNumber;
                _tooltipPopup.Child = CreateTooltip(blameInfo);
                _tooltipPopup.IsOpen = true;
            }
        }
        else
        {
            Cursor = Cursors.Arrow;
            if (_currentTooltipLine != -1)
            {
                _currentTooltipLine = -1;
                _tooltipPopup.IsOpen = false;
            }
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        Cursor = Cursors.Arrow;
        _currentTooltipLine = -1;
        _tooltipPopup.IsOpen = false;
    }

    private BlameLineInfo? GetBlameInfoAtPosition(Point position)
    {
        var viewY = position.Y + _view.ViewportTop;

        foreach (var line in _view.TextViewLines)
        {
            if (line.VisibilityState == VisibilityState.Unattached)
                continue;

            if (viewY >= line.TextTop && viewY <= line.TextBottom)
            {
                var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;
                return _blameData.FirstOrDefault(b => b.LineNumber == lineNumber);
            }
        }

        return null;
    }

    private static UIElement CreateTooltip(BlameLineInfo blameInfo)
    {
        var content = new StackPanel { Margin = new Thickness(8) };

        content.Children.Add(new TextBlock
        {
            Text = $"Commit: {blameInfo.ShortSha}",
            FontWeight = FontWeights.Bold
        });

        content.Children.Add(new TextBlock
        {
            Text = $"Author: {blameInfo.Author}",
            Margin = new Thickness(0, 4, 0, 0)
        });

        content.Children.Add(new TextBlock
        {
            Text = $"Date: {blameInfo.RelativeTime}",
            Margin = new Thickness(0, 2, 0, 0)
        });

        content.Children.Add(new TextBlock
        {
            Text = blameInfo.CommitMessage,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300,
            Margin = new Thickness(0, 8, 0, 0)
        });

        content.Children.Add(new TextBlock
        {
            Text = "Click to copy commit SHA",
            FontStyle = FontStyles.Italic,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 8, 0, 0)
        });

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Child = content
        };

        foreach (var child in content.Children)
        {
            if (child is TextBlock textBlock && textBlock.Foreground != Brushes.Gray)
            {
                textBlock.Foreground = Brushes.White;
            }
        }

        return border;
    }
}
