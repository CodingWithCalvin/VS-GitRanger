using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.GitRanger.Commands
{
    /// <summary>
    /// Commands related to file/line history functionality.
    /// </summary>
    internal static class HistoryCommands
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return;

            // Show File History command
            var showFileHistoryId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidShowFileHistory);
            var showFileHistoryCommand = new OleMenuCommand(OnShowFileHistory, showFileHistoryId);
            showFileHistoryCommand.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(showFileHistoryCommand);

            // Show Line History command
            var showLineHistoryId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidShowLineHistory);
            var showLineHistoryCommand = new OleMenuCommand(OnShowLineHistory, showLineHistoryId);
            showLineHistoryCommand.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(showLineHistoryCommand);

            // Compare with Previous command
            var compareWithPreviousId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidCompareWithPrevious);
            var compareWithPreviousCommand = new OleMenuCommand(OnCompareWithPrevious, compareWithPreviousId);
            compareWithPreviousCommand.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(compareWithPreviousCommand);
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                var isInRepo = GitRangerPackage.GitService?.IsInRepository ?? false;
                command.Enabled = isInRepo;
                command.Visible = true;
            }
        }

        private static async void OnShowFileHistory(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var docView = await VS.Documents.GetActiveDocumentViewAsync();
                if (docView == null)
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: No active document");
                    return;
                }

                var filePath = docView.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: Could not determine file path");
                    return;
                }

                // TODO: Open File History Tool Window
                // For now, show a status message
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: File History for {System.IO.Path.GetFileName(filePath)} - Coming soon!");

                // Placeholder for opening tool window:
                // await FileHistoryToolWindow.ShowAsync(filePath);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Error showing file history - {ex.Message}");
            }
        }

        private static async void OnShowLineHistory(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var docView = await VS.Documents.GetActiveDocumentViewAsync();
                if (docView?.TextView == null)
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: No active document");
                    return;
                }

                var filePath = docView.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: Could not determine file path");
                    return;
                }

                var caretPosition = docView.TextView.Caret.Position.BufferPosition;
                var lineNumber = docView.TextView.TextSnapshot.GetLineNumberFromPosition(caretPosition.Position) + 1;

                // TODO: Open Line History Tool Window
                // For now, show a status message
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Line {lineNumber} History - Coming soon!");
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Error showing line history - {ex.Message}");
            }
        }

        private static async void OnCompareWithPrevious(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var docView = await VS.Documents.GetActiveDocumentViewAsync();
                if (docView == null)
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: No active document");
                    return;
                }

                var filePath = docView.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: Could not determine file path");
                    return;
                }

                // TODO: Implement diff with previous version
                // For now, show a status message
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Compare with Previous - Coming soon!");

                // Placeholder for diff functionality:
                // var gitService = GitRangerPackage.GitService;
                // if (gitService != null)
                // {
                //     var history = gitService.GetFileHistory(filePath).Take(2).ToList();
                //     if (history.Count >= 2)
                //     {
                //         await VS.Diff.OpenAsync(previousVersion, currentVersion);
                //     }
                // }
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Error comparing with previous - {ex.Message}");
            }
        }
    }
}
