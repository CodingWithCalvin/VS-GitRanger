using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace CodingWithCalvin.GitRanger.Editor.BlameAdornment
{
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
        private readonly ITextDocumentFactoryService? _textDocumentFactoryService;
        private readonly Dictionary<int, UIElement> _adornments = new Dictionary<int, UIElement>();
        private IReadOnlyList<BlameLineInfo> _blameData = Array.Empty<BlameLineInfo>();
        private string? _currentFilePath;
        private bool _isLoading;

        /// <summary>
        /// Creates a new blame adornment for the given text view.
        /// </summary>
        public BlameAdornment(IWpfTextView view, ITextDocumentFactoryService? textDocumentFactoryService)
        {
            Debug.WriteLine("[GitRanger] BlameAdornment constructor called");

            _view = view ?? throw new ArgumentNullException(nameof(view));
            _textDocumentFactoryService = textDocumentFactoryService;
            _layer = view.GetAdornmentLayer(LayerName);

            Debug.WriteLine($"[GitRanger] BlameAdornment - TextDocumentFactoryService: {(_textDocumentFactoryService == null ? "NULL" : "OK")}");
            Debug.WriteLine($"[GitRanger] BlameAdornment - AdornmentLayer: {(_layer == null ? "NULL" : "OK")}");

            // Ensure services are initialized (in case package hasn't loaded yet)
            GitRangerPackage.EnsureServicesInitialized();
            Debug.WriteLine("[GitRanger] BlameAdornment - Services initialized");

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

            // Initial load
            LoadBlameDataAsync();
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnViewClosed;

            if (GitRangerPackage.BlameService != null)
            {
                GitRangerPackage.BlameService.BlameLoaded -= OnBlameLoaded;
            }

            GeneralOptions.Saved -= OnOptionsSaved;
        }

        private void OnOptionsSaved(GeneralOptions options)
        {
            // Refresh adornments when options change
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ClearAdornments();
                UpdateAdornments();
            });
        }

        private void OnBlameLoaded(object? sender, BlameLoadedEventArgs e)
        {
            Debug.WriteLine($"[GitRanger] OnBlameLoaded - Event FilePath: {e.FilePath}, CurrentFilePath: {_currentFilePath}, LineCount: {e.Lines.Count}");

            if (string.Equals(e.FilePath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("[GitRanger] OnBlameLoaded - File paths match, updating adornments");
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _blameData = e.Lines;
                    _isLoading = false;
                    Debug.WriteLine($"[GitRanger] OnBlameLoaded - Blame data set, {_blameData.Count} lines");
                    ClearAdornments();
                    UpdateAdornments();
                });
            }
        }

        private void LoadBlameDataAsync()
        {
            var filePath = GetFilePath();
            Debug.WriteLine($"[GitRanger] LoadBlameDataAsync - FilePath: {filePath ?? "NULL"}");

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine("[GitRanger] LoadBlameDataAsync - FilePath is null or empty, returning");
                return;
            }

            if (string.Equals(filePath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("[GitRanger] LoadBlameDataAsync - Same file path, already loaded");
                return;
            }

            _currentFilePath = filePath;
            _isLoading = true;

            // Try to open repository
            var gitService = GitRangerPackage.GitService;
            if (gitService == null)
            {
                Debug.WriteLine("[GitRanger] LoadBlameDataAsync - GitService is NULL");
                _isLoading = false;
                return;
            }

            var repoOpened = gitService.TryOpenRepository(filePath);
            Debug.WriteLine($"[GitRanger] LoadBlameDataAsync - TryOpenRepository: {repoOpened}, RepoPath: {gitService.CurrentRepositoryPath ?? "NULL"}");

            if (!repoOpened)
            {
                Debug.WriteLine("[GitRanger] LoadBlameDataAsync - Failed to open repository");
                _isLoading = false;
                return;
            }

            // Load blame in background
            var blameService = GitRangerPackage.BlameService;
            if (blameService != null)
            {
                Debug.WriteLine("[GitRanger] LoadBlameDataAsync - Loading blame in background");
                blameService.LoadBlameInBackground(filePath);
            }
            else
            {
                Debug.WriteLine("[GitRanger] LoadBlameDataAsync - BlameService is NULL");
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
            // Check if file path changed
            var currentPath = GetFilePath();
            if (!string.Equals(currentPath, _currentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                _currentFilePath = null;
                _blameData = Array.Empty<BlameLineInfo>();
                ClearAdornments();
                LoadBlameDataAsync();
                return;
            }

            // Update only for lines that changed or became visible
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
            Debug.WriteLine("[GitRanger] UpdateAdornments called");

            var options = GeneralOptions.Instance;
            Debug.WriteLine($"[GitRanger] UpdateAdornments - Options: {(options == null ? "NULL" : "OK")}, EnableInlineBlame: {options?.EnableInlineBlame}");

            if (options == null || !options.EnableInlineBlame)
            {
                Debug.WriteLine("[GitRanger] UpdateAdornments - Options null or inline blame disabled");
                ClearAdornments();
                return;
            }

            Debug.WriteLine($"[GitRanger] UpdateAdornments - IsLoading: {_isLoading}, BlameDataCount: {_blameData.Count}");
            if (_isLoading || _blameData.Count == 0)
                return;

            // Get visible lines
            var viewportTop = _view.ViewportTop;
            var viewportBottom = _view.ViewportBottom;

            foreach (var line in _view.TextViewLines)
            {
                // Skip lines outside viewport
                if (line.Bottom < viewportTop || line.Top > viewportBottom)
                    continue;

                var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;

                // Find blame data for this line
                var blameInfo = _blameData.FirstOrDefault(b => b.LineNumber == lineNumber);
                if (blameInfo == null)
                    continue;

                // Create or update adornment
                CreateAdornmentForLine(line, blameInfo, options);
            }
        }

        private void CreateAdornmentForLine(ITextViewLine line, BlameLineInfo blameInfo, GeneralOptions options)
        {
            var lineNumber = _view.TextSnapshot.GetLineNumberFromPosition(line.Start.Position) + 1;

            // Remove existing adornment for this line
            if (_adornments.TryGetValue(lineNumber, out var existing))
            {
                _layer.RemoveAdornment(existing);
                _adornments.Remove(lineNumber);
            }

            // Build the blame text
            var blameText = BuildBlameText(blameInfo, options);
            if (string.IsNullOrEmpty(blameText))
                return;

            // Get theme colors
            var themeService = GitRangerPackage.ThemeService;
            var textColor = themeService?.GetBlameTextColor() ?? Colors.Gray;
            var authorColor = themeService?.GetAuthorColor(blameInfo.AuthorEmail) ?? Colors.Gray;

            // Create the visual
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

            // Apply color based on mode
            switch (options.BlameColorMode)
            {
                case ColorMode.Author:
                    textBlock.Foreground = new SolidColorBrush(
                        themeService?.AdjustForTheme(authorColor) ?? authorColor);
                    break;

                case ColorMode.Age:
                    var ageColor = themeService?.GetAgeHeatMapColor(blameInfo.AgeDays, options.MaxAgeDays) ?? Colors.Gray;
                    textBlock.Foreground = new SolidColorBrush(ageColor);
                    break;
            }

            // Position at end of line
            Canvas.SetLeft(textBlock, line.TextRight + 20);
            Canvas.SetTop(textBlock, line.TextTop);

            // Add to layer
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

            if (author.Length <= maxLength)
                return author;

            return author.Substring(0, maxLength - 1) + "…";
        }

        private static string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            // Take first line only
            var firstLine = message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? message;

            if (firstLine.Length <= maxLength)
                return firstLine;

            return firstLine.Substring(0, maxLength - 1) + "…";
        }

        private static object CreateTooltip(BlameLineInfo blameInfo)
        {
            var tooltip = new StackPanel { Margin = new Thickness(4) };

            // Commit SHA
            tooltip.Children.Add(new TextBlock
            {
                Text = $"Commit: {blameInfo.ShortSha}",
                FontWeight = FontWeights.Bold
            });

            // Author
            tooltip.Children.Add(new TextBlock
            {
                Text = $"Author: {blameInfo.Author} <{blameInfo.AuthorEmail}>",
                Margin = new Thickness(0, 4, 0, 0)
            });

            // Date
            tooltip.Children.Add(new TextBlock
            {
                Text = $"Date: {blameInfo.AuthorDate:yyyy-MM-dd HH:mm:ss} ({blameInfo.RelativeTime})",
                Margin = new Thickness(0, 2, 0, 0)
            });

            // Message
            tooltip.Children.Add(new TextBlock
            {
                Text = blameInfo.FullCommitMessage,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
                Margin = new Thickness(0, 8, 0, 0)
            });

            // Instructions
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
}
