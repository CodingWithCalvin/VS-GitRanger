using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodingWithCalvin.GitRanger.Core.Models;
using CodingWithCalvin.GitRanger.Options;
using CodingWithCalvin.GitRanger.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace CodingWithCalvin.GitRanger.Editor.BlameAdornment;

/// <summary>
/// Adornment that renders blame information at the end of each line.
/// </summary>
internal sealed class BlameAdornment
{
    /// <summary>
    /// The name of the adornment layer.
    /// </summary>
    public const string LayerName = "GitRangerBlameAdornment";

    private readonly IWpfTextView _view;
    private readonly IAdornmentLayer _layer;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly IGitService _gitService;
    private readonly IBlameService _blameService;
    private readonly IThemeService _themeService;
    private readonly IOutputPaneService _outputPane;
    private readonly Dictionary<int, UIElement> _adornments = new();
    private IReadOnlyList<BlameLineInfo> _blameData = Array.Empty<BlameLineInfo>();
    private string? _currentFilePath;
    private bool _isLoading;

    /// <summary>
    /// Creates a new blame adornment for the given text view.
    /// </summary>
    public BlameAdornment(
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
        _layer = view.GetAdornmentLayer(LayerName);

        _outputPane.WriteVerbose("BlameAdornment created");

        // Subscribe to events
        _view.LayoutChanged += OnLayoutChanged;
        _view.Closed += OnViewClosed;
        _blameService.BlameLoaded += OnBlameLoaded;
        GeneralOptions.Saved += OnOptionsSaved;

        // Initial load
        LoadBlameData();
    }

    private void OnViewClosed(object sender, EventArgs e)
    {
        _view.LayoutChanged -= OnLayoutChanged;
        _view.Closed -= OnViewClosed;
        _blameService.BlameLoaded -= OnBlameLoaded;
        GeneralOptions.Saved -= OnOptionsSaved;
    }

    private void OnOptionsSaved(GeneralOptions options)
    {
        _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ClearAdornments();
            UpdateAdornments();
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
                _outputPane.WriteVerbose("BlameAdornment received {0} lines", e.Lines.Count);
                ClearAdornments();
                UpdateAdornments();
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
            ClearAdornments();
            LoadBlameData();
            return;
        }

        if (e.NewOrReformattedLines.Count > 0 || e.VerticalTranslation)
        {
            UpdateAdornments();
        }
    }

    private void ClearAdornments()
    {
        _layer.RemoveAllAdornments();
        _adornments.Clear();
    }

    private void UpdateAdornments()
    {
        var options = GeneralOptions.Instance;
        if (options == null || !options.EnableInlineBlame)
        {
            ClearAdornments();
            return;
        }

        if (_isLoading || _blameData.Count == 0)
            return;

        var viewportTop = _view.ViewportTop;
        var viewportBottom = _view.ViewportBottom;

        foreach (var line in _view.TextViewLines)
        {
            if (line.Bottom < viewportTop || line.Top > viewportBottom)
                continue;

            var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;
            var blameInfo = _blameData.FirstOrDefault(b => b.LineNumber == lineNumber);
            if (blameInfo == null)
                continue;

            CreateAdornmentForLine(line, blameInfo, options);
        }
    }

    private void CreateAdornmentForLine(ITextViewLine line, BlameLineInfo blameInfo, GeneralOptions options)
    {
        var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;

        if (_adornments.TryGetValue(lineNumber, out var existing))
        {
            _layer.RemoveAdornment(existing);
            _adornments.Remove(lineNumber);
        }

        var blameText = BuildBlameText(blameInfo, options);
        if (string.IsNullOrEmpty(blameText))
            return;

        var textColor = _themeService.GetBlameTextColor();
        var authorColor = _themeService.GetAuthorColor(blameInfo.AuthorEmail);

        var textBlock = new TextBlock
        {
            Text = blameText,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Foreground = new SolidColorBrush(textColor),
            Opacity = options.InlineBlameOpacity,
            Margin = new Thickness(20, 0, 0, 0),
            ToolTip = CreateTooltip(blameInfo)
        };

        switch (options.BlameColorMode)
        {
            case ColorMode.Author:
                textBlock.Foreground = new SolidColorBrush(_themeService.AdjustForTheme(authorColor));
                break;

            case ColorMode.Age:
                var ageColor = _themeService.GetAgeHeatMapColor(blameInfo.AgeDays, options.MaxAgeDays);
                textBlock.Foreground = new SolidColorBrush(ageColor);
                break;
        }

        Canvas.SetLeft(textBlock, line.TextRight + 20);
        Canvas.SetTop(textBlock, line.TextTop);

        _layer.AddAdornment(
            AdornmentPositioningBehavior.TextRelative,
            line.Extent,
            null,
            textBlock,
            (tag, element) => _adornments.Remove(lineNumber));

        _adornments[lineNumber] = textBlock;
    }

    private static string BuildBlameText(BlameLineInfo blameInfo, GeneralOptions options)
    {
        var parts = new List<string>();

        if (options.ShowAuthorName)
        {
            parts.Add(TruncateAuthor(blameInfo.Author));
        }

        if (options.ShowCommitDate)
        {
            var dateText = options.DateFormat.ToLowerInvariant() == "relative"
                ? blameInfo.RelativeTime
                : blameInfo.AuthorDate.ToString(options.DateFormat);
            parts.Add(dateText);
        }

        if (options.ShowCommitMessage && !options.CompactMode)
        {
            parts.Add(TruncateMessage(blameInfo.CommitMessage, 50));
        }

        return string.Join(" | ", parts);
    }

    private static string TruncateAuthor(string author, int maxLength = 15)
    {
        if (string.IsNullOrEmpty(author))
            return string.Empty;

        return author.Length <= maxLength ? author : author.Substring(0, maxLength - 1) + "…";
    }

    private static string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        var firstLine = message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? message;
        return firstLine.Length <= maxLength ? firstLine : firstLine.Substring(0, maxLength - 1) + "…";
    }

    private static object CreateTooltip(BlameLineInfo blameInfo)
    {
        var tooltip = new StackPanel { Margin = new Thickness(4) };

        tooltip.Children.Add(new TextBlock
        {
            Text = $"Commit: {blameInfo.ShortSha}",
            FontWeight = FontWeights.Bold
        });

        tooltip.Children.Add(new TextBlock
        {
            Text = $"Author: {blameInfo.Author} <{blameInfo.AuthorEmail}>",
            Margin = new Thickness(0, 4, 0, 0)
        });

        tooltip.Children.Add(new TextBlock
        {
            Text = $"Date: {blameInfo.AuthorDate:yyyy-MM-dd HH:mm:ss} ({blameInfo.RelativeTime})",
            Margin = new Thickness(0, 2, 0, 0)
        });

        tooltip.Children.Add(new TextBlock
        {
            Text = blameInfo.FullCommitMessage,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 400,
            Margin = new Thickness(0, 8, 0, 0)
        });

        tooltip.Children.Add(new TextBlock
        {
            Text = "Right-click for more options",
            FontStyle = FontStyles.Italic,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 8, 0, 0)
        });

        return tooltip;
    }
}
