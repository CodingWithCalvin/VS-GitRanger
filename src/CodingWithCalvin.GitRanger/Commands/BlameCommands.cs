using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Options;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.GitRanger.Commands
{
    /// <summary>
    /// Commands related to blame functionality.
    /// </summary>
    internal static class BlameCommands
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return;

            // Toggle Inline Blame command
            var toggleInlineBlameId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidToggleInlineBlame);
            var toggleInlineBlameCommand = new OleMenuCommand(OnToggleInlineBlame, toggleInlineBlameId);
            toggleInlineBlameCommand.BeforeQueryStatus += OnBeforeQueryStatusToggleInlineBlame;
            commandService.AddCommand(toggleInlineBlameCommand);

            // Toggle Blame Gutter command
            var toggleBlameGutterId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidToggleBlameGutter);
            var toggleBlameGutterCommand = new OleMenuCommand(OnToggleBlameGutter, toggleBlameGutterId);
            toggleBlameGutterCommand.BeforeQueryStatus += OnBeforeQueryStatusToggleBlameGutter;
            commandService.AddCommand(toggleBlameGutterCommand);

            // Copy Commit SHA command
            var copyCommitShaId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidCopyCommitSha);
            var copyCommitShaCommand = new OleMenuCommand(OnCopyCommitSha, copyCommitShaId);
            copyCommitShaCommand.BeforeQueryStatus += OnBeforeQueryStatusCopyCommitSha;
            commandService.AddCommand(copyCommitShaCommand);
        }

        private static void OnBeforeQueryStatusToggleInlineBlame(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                var options = GeneralOptions.Instance;
                var isEnabled = options?.EnableInlineBlame ?? true;
                command.Text = isEnabled
                    ? "Git Ranger: Disable Inline Blame"
                    : "Git Ranger: Enable Inline Blame";
                command.Enabled = true;
                command.Visible = true;
            }
        }

        private static void OnToggleInlineBlame(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var options = GeneralOptions.Instance;
            if (options != null)
            {
                options.EnableInlineBlame = !options.EnableInlineBlame;
                options.Save();

                var status = options.EnableInlineBlame ? "enabled" : "disabled";
                VS.StatusBar.ShowMessageAsync($"Git Ranger: Inline blame {status}").FireAndForget();
            }
        }

        private static void OnBeforeQueryStatusToggleBlameGutter(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                var options = GeneralOptions.Instance;
                var isEnabled = options?.EnableBlameGutter ?? true;
                command.Text = isEnabled
                    ? "Git Ranger: Disable Blame Gutter"
                    : "Git Ranger: Enable Blame Gutter";
                command.Enabled = true;
                command.Visible = true;
            }
        }

        private static void OnToggleBlameGutter(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var options = GeneralOptions.Instance;
            if (options != null)
            {
                options.EnableBlameGutter = !options.EnableBlameGutter;
                options.Save();

                var status = options.EnableBlameGutter ? "enabled" : "disabled";
                VS.StatusBar.ShowMessageAsync($"Git Ranger: Blame gutter {status}").FireAndForget();
            }
        }

        private static void OnBeforeQueryStatusCopyCommitSha(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                // Only enable if we're in a git repository and have blame data
                var isInRepo = GitRangerPackage.GitService?.IsInRepository ?? false;
                command.Enabled = isInRepo;
                command.Visible = true;
            }
        }

        private static async void OnCopyCommitSha(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var docView = await VS.Documents.GetActiveDocumentViewAsync();
                if (docView?.TextView == null)
                    return;

                var filePath = docView.FilePath;
                if (string.IsNullOrEmpty(filePath))
                    return;

                // Get the current line
                var caretPosition = docView.TextView.Caret.Position.BufferPosition;
                var lineNumber = docView.TextView.TextSnapshot.GetLineNumberFromPosition(caretPosition.Position) + 1;

                // Get blame for this line
                var blameInfo = GitRangerPackage.BlameService?.GetBlameForLine(filePath, lineNumber);
                if (blameInfo != null)
                {
                    System.Windows.Clipboard.SetText(blameInfo.CommitSha);
                    await VS.StatusBar.ShowMessageAsync($"Git Ranger: Copied commit SHA {blameInfo.ShortSha} to clipboard");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: No blame information available for this line");
                }
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Error copying commit SHA - {ex.Message}");
            }
        }
    }
}
