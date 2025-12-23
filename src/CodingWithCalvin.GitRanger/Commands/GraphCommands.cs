using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.GitRanger.Commands
{
    /// <summary>
    /// Commands related to the Git Graph functionality.
    /// </summary>
    internal static class GraphCommands
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return;

            // Open Git Graph command
            var openGitGraphId = new CommandID(PackageGuids.guidGitRangerPackageCmdSet, PackageIds.cmdidOpenGitGraph);
            var openGitGraphCommand = new OleMenuCommand(OnOpenGitGraph, openGitGraphId);
            openGitGraphCommand.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(openGitGraphCommand);
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

        private static async void OnOpenGitGraph(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Check if we're in a git repository
                var gitService = GitRangerPackage.GitService;
                if (gitService == null || !gitService.IsInRepository)
                {
                    // Try to detect repository from active document
                    var docView = await VS.Documents.GetActiveDocumentViewAsync();
                    if (docView?.FilePath != null)
                    {
                        gitService?.TryOpenRepository(docView.FilePath);
                    }
                }

                if (gitService == null || !gitService.IsInRepository)
                {
                    await VS.StatusBar.ShowMessageAsync("Git Ranger: Not in a Git repository");
                    return;
                }

                // TODO: Open Git Graph Document Window (editor tab)
                // For now, show a status message with branch info
                var branchName = gitService.CurrentBranchName ?? "unknown";
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Opening Git Graph for branch '{branchName}' - Coming soon!");

                // Placeholder for opening document window:
                // await GitGraphDocumentWindow.ShowAsync(gitService.CurrentRepositoryPath);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Error opening Git Graph - {ex.Message}");
            }
        }
    }
}
