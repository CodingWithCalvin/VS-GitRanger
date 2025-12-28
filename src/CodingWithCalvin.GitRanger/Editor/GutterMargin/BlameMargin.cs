using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CodingWithCalvin.GitRanger.Core.Models;
using CodingWithCalvin.GitRanger.Options;
using CodingWithCalvin.GitRanger.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace CodingWithCalvin.GitRanger.Editor.GutterMargin
{
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
        private readonly ITextDocumentFactoryService? _textDocumentFactoryService;
        private IReadOnlyList<BlameLineInfo> _blameData = Array.Empty<BlameLineInfo>();
        private string? _currentFilePath;
        private bool _isLoading;
        private bool _isDisposed;
        private int _lastTooltipLine = -1;

        /// <summary>
        /// Creates a new blame margin for the given text view.
        /// </summary>
        public BlameMargin(IWpfTextView view, ITextDocumentFactoryService? textDocumentFactoryService)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _textDocumentFactoryService = textDocumentFactoryService;

            // Set initial size
            var options = GeneralOptions.Instance;
            Width = options?.GutterWidth ?? 40;
            ClipToBounds = true;
            Background = Brushes.Transparent; // Required for mouse events to work

            // Ensure services are initialized (in case package hasn't loaded yet)
            GitRangerPackage.EnsureServicesInitialized();

            // Subscribe to events
            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnViewClosed;

            // Subscribe to blame service events
            if (GitRangerPackage.BlameService != null)
            {
                GitRangerPackage.BlameService.BlameLoaded += OnBlameLoaded;
            }

            // Subscribe to options changes
            GeneralOptions.Saved += OnOptionsSaved;

            // Handle mouse events
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;

            // Initial load
            LoadBlameDataAsync();
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

            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnViewClosed;

            if (GitRangerPackage.BlameService != null)
            {
                GitRangerPackage.BlameService.BlameLoaded -= OnBlameLoaded;
            }

            GeneralOptions.Saved -= OnOptionsSaved;
        }

        #endregion

        private void OnViewClosed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void OnOptionsSaved(GeneralOptions options)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
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
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _blameData = e.Lines;
                    _isLoading = false;
                    InvalidateVisual();
                });
            }
        }

        private void LoadBlameDataAsync()
        {
            var filePath = GetFilePath();
            if (string.IsNullOrEmpty(filePath))
                return;

            if (string.Equals(filePath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
                return;

            _currentFilePath = filePath;
            _isLoading = true;

            var gitService = GitRangerPackage.GitService;
            if (gitService == null)
            {
                _isLoading = false;
                return;
            }

            if (!gitService.TryOpenRepository(filePath))
            {
                _isLoading = false;
                return;
            }

            var blameService = GitRangerPackage.BlameService;
            if (blameService != null)
            {
                blameService.LoadBlameInBackground(filePath);
            }
            else
            {
                _isLoading = false;
            }
        }

        private string? GetFilePath()
        {
            // Try to get the file path using the document factory service
            if (_textDocumentFactoryService != null)
            {
                if (_textDocumentFactoryService.TryGetTextDocument(_view.TextDataModel.DocumentBuffer, out var textDocument))
                {
                    return textDocument.FilePath;
                }
            }

            // Fallback: try to get from buffer properties
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
                LoadBlameDataAsync();
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

            var themeService = GitRangerPackage.ThemeService;
            var backgroundColor = themeService?.GetBlameBackgroundColor() ?? Color.FromArgb(30, 0, 0, 0);

            // Draw background
            drawingContext.DrawRectangle(
                new SolidColorBrush(backgroundColor),
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));

            // Draw for each visible line
            foreach (var line in _view.TextViewLines)
            {
                if (line.VisibilityState == VisibilityState.Unattached)
                    continue;

                var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;
                var blameInfo = _blameData.FirstOrDefault(b => b.LineNumber == lineNumber);
                if (blameInfo == null)
                    continue;

                DrawLineIndicator(drawingContext, line, blameInfo, options, themeService);
            }
        }

        private void DrawLineIndicator(
            DrawingContext drawingContext,
            ITextViewLine line,
            BlameLineInfo blameInfo,
            GeneralOptions options,
            ThemeService? themeService)
        {
            var y = line.TextTop - _view.ViewportTop;
            var height = line.TextHeight;

            // Get color based on mode
            Color color;
            switch (options.BlameColorMode)
            {
                case ColorMode.Author:
                    color = themeService?.GetAuthorColor(blameInfo.AuthorEmail) ?? Colors.Gray;
                    color = themeService?.AdjustForTheme(color) ?? color;
                    break;

                case ColorMode.Age:
                    color = themeService?.GetAgeHeatMapColor(blameInfo.AgeDays, options.MaxAgeDays) ?? Colors.Gray;
                    break;

                default:
                    color = Colors.Gray;
                    break;
            }

            if (options.ShowAgeBars)
            {
                // Draw age bar (width proportional to age)
                var maxWidth = ActualWidth - 4;
                var ageRatio = Math.Min(1.0, (double)blameInfo.AgeDays / options.MaxAgeDays);
                var barWidth = maxWidth * (1.0 - ageRatio); // Newer = longer bar

                var brush = new SolidColorBrush(color);
                brush.Freeze();

                drawingContext.DrawRectangle(
                    brush,
                    null,
                    new Rect(2, y + 2, barWidth, height - 4));
            }
            else
            {
                // Draw simple color indicator
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                drawingContext.DrawRectangle(
                    brush,
                    null,
                    new Rect(2, y + 2, 4, height - 4));
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            var blameInfo = GetBlameInfoAtPosition(position);
            if (blameInfo != null)
            {
                // Show commit details
                System.Windows.Clipboard.SetText(blameInfo.CommitSha);
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await Community.VisualStudio.Toolkit.VS.StatusBar.ShowMessageAsync(
                        $"Git Ranger: Copied commit SHA {blameInfo.ShortSha} to clipboard");
                });
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            var blameInfo = GetBlameInfoAtPosition(position);

            if (blameInfo != null)
            {
                Cursor = Cursors.Hand;
                // Only update tooltip if we moved to a different line
                if (_lastTooltipLine != blameInfo.LineNumber)
                {
                    _lastTooltipLine = blameInfo.LineNumber;
                    ToolTip = CreateTooltip(blameInfo);
                }
            }
            else
            {
                Cursor = Cursors.Arrow;
                if (_lastTooltipLine != -1)
                {
                    _lastTooltipLine = -1;
                    ToolTip = null;
                }
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
            _lastTooltipLine = -1;
            ToolTip = null;
        }

        private BlameLineInfo? GetBlameInfoAtPosition(Point position)
        {
            // Adjust for scroll position
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
                Text = $"Author: {blameInfo.Author}",
                Margin = new Thickness(0, 4, 0, 0)
            });

            tooltip.Children.Add(new TextBlock
            {
                Text = $"Date: {blameInfo.RelativeTime}",
                Margin = new Thickness(0, 2, 0, 0)
            });

            tooltip.Children.Add(new TextBlock
            {
                Text = blameInfo.CommitMessage,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300,
                Margin = new Thickness(0, 8, 0, 0)
            });

            tooltip.Children.Add(new TextBlock
            {
                Text = "Click to copy commit SHA",
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 8, 0, 0)
            });

            return tooltip;
        }
    }
}
