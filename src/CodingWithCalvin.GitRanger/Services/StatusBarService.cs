using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Core.Models;
using CodingWithCalvin.GitRanger.Options;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service for displaying blame information in the Visual Studio status bar.
    /// Updates in real-time as the cursor moves between lines.
    /// </summary>
    [Export(typeof(IStatusBarService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StatusBarService : IStatusBarService
    {
        private readonly IBlameService _blameService;
        private readonly IOutputPaneService _outputPane;
        private IWpfTextView? _currentView;
        private string? _currentFilePath;
        private int _lastLineNumber = -1;
        private bool _isDisposed;

        [ImportingConstructor]
        public StatusBarService(IBlameService blameService, IOutputPaneService outputPane)
        {
            _blameService = blameService ?? throw new ArgumentNullException(nameof(blameService));
            _outputPane = outputPane ?? throw new ArgumentNullException(nameof(outputPane));

            _outputPane.WriteInfo("StatusBarService created");

            // Subscribe to document/window events
            VS.Events.WindowEvents.ActiveFrameChanged += OnActiveFrameChanged;
            GeneralOptions.Saved += OnOptionsSaved;

            // Initialize with current document
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await AttachToActiveDocumentAsync();
        }

        private void OnActiveFrameChanged(ActiveFrameChangeEventArgs args)
        {
            _ = HandleActiveFrameChangedAsync();
        }

        private async Task HandleActiveFrameChangedAsync()
        {
            await AttachToActiveDocumentAsync();
        }

        private async Task AttachToActiveDocumentAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView is IWpfTextView wpfView && wpfView != _currentView)
            {
                DetachFromCurrentView();

                _currentView = wpfView;
                _currentFilePath = docView.FilePath;
                _lastLineNumber = -1;

                _currentView.Caret.PositionChanged += OnCaretPositionChanged;
                _currentView.Closed += OnViewClosed;

                // Trigger initial update
                UpdateStatusBarForCurrentPosition();
            }
        }

        private void DetachFromCurrentView()
        {
            if (_currentView != null)
            {
                _currentView.Caret.PositionChanged -= OnCaretPositionChanged;
                _currentView.Closed -= OnViewClosed;
                _currentView = null;
                _currentFilePath = null;
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            DetachFromCurrentView();
            _ = ClearStatusBarAsync();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdateStatusBarForCurrentPosition();
        }

        private void UpdateStatusBarForCurrentPosition()
        {
            if (_currentView == null) return;

            var lineNumber = _currentView.TextSnapshot
                .GetLineNumberFromPosition(_currentView.Caret.Position.BufferPosition.Position) + 1;

            if (lineNumber != _lastLineNumber)
            {
                _lastLineNumber = lineNumber;
                _ = UpdateStatusBarAsync();
            }
        }

        private void OnOptionsSaved(GeneralOptions options)
        {
            // Refresh when options change
            _lastLineNumber = -1;
            UpdateStatusBarForCurrentPosition();
        }

        private async Task UpdateStatusBarAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var options = await GeneralOptions.GetLiveInstanceAsync();
            if (options == null || !options.EnableStatusBarBlame)
            {
                await ClearStatusBarAsync();
                return;
            }

            if (string.IsNullOrEmpty(_currentFilePath) || _lastLineNumber <= 0)
            {
                await ClearStatusBarAsync();
                return;
            }

            var blameInfo = _blameService.GetBlameForLine(_currentFilePath, _lastLineNumber);
            if (blameInfo == null)
            {
                await ClearStatusBarAsync();
                return;
            }

            var message = FormatBlameMessage(blameInfo, options);
            await VS.StatusBar.ShowMessageAsync(message);
        }

        private static string FormatBlameMessage(BlameLineInfo info, GeneralOptions options)
        {
            var format = options.StatusBarFormat;
            var date = options.StatusBarRelativeDate ? info.RelativeTime : info.AuthorDate.ToString("g");
            var message = info.CommitMessage?.Split('\n').FirstOrDefault() ?? "";

            var result = format
                .Replace("{author}", info.Author ?? "Unknown")
                .Replace("{date}", date)
                .Replace("{message}", message)
                .Replace("{sha}", info.ShortSha ?? "");

            if (options.StatusBarMaxLength > 0 && result.Length > options.StatusBarMaxLength)
            {
                result = result.Substring(0, options.StatusBarMaxLength - 3) + "...";
            }

            return $"Git Ranger: {result}";
        }

        private static async Task ClearStatusBarAsync()
        {
            await VS.StatusBar.ClearAsync();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            DetachFromCurrentView();
            VS.Events.WindowEvents.ActiveFrameChanged -= OnActiveFrameChanged;
            GeneralOptions.Saved -= OnOptionsSaved;
        }
    }
}
