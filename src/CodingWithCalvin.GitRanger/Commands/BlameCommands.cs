using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Options;
using CodingWithCalvin.GitRanger.Services;
using CodingWithCalvin.Otel4Vsix;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.GitRanger.Commands;

/// <summary>
/// Commands related to blame functionality.
/// </summary>
internal static class BlameCommands
{
    private static IGitService? _gitService;
    private static IBlameService? _blameService;

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        // Get services from MEF
        var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
        if (componentModel != null)
        {
            _gitService = componentModel.GetService<IGitService>();
            _blameService = componentModel.GetService<IBlameService>();
        }

        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService == null)
            return;

        // Toggle Inline Blame command
        var toggleInlineBlameId = new CommandID(VSCommandTableVsct.guidGitRangerPackageCmdSet.Guid, VSCommandTableVsct.guidGitRangerPackageCmdSet.cmdidToggleInlineBlame);
        var toggleInlineBlameCommand = new OleMenuCommand(OnToggleInlineBlame, toggleInlineBlameId);
        toggleInlineBlameCommand.BeforeQueryStatus += OnBeforeQueryStatusToggleInlineBlame;
        commandService.AddCommand(toggleInlineBlameCommand);

        // Toggle Blame Gutter command
        var toggleBlameGutterId = new CommandID(VSCommandTableVsct.guidGitRangerPackageCmdSet.Guid, VSCommandTableVsct.guidGitRangerPackageCmdSet.cmdidToggleBlameGutter);
        var toggleBlameGutterCommand = new OleMenuCommand(OnToggleBlameGutter, toggleBlameGutterId);
        toggleBlameGutterCommand.BeforeQueryStatus += OnBeforeQueryStatusToggleBlameGutter;
        commandService.AddCommand(toggleBlameGutterCommand);

        // Copy Commit SHA command
        var copyCommitShaId = new CommandID(VSCommandTableVsct.guidGitRangerPackageCmdSet.Guid, VSCommandTableVsct.guidGitRangerPackageCmdSet.cmdidCopyCommitSha);
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
                ? "Disable Inline Blame"
                : "Enable Inline Blame";
            command.Enabled = true;
            command.Visible = true;
        }
    }

    private static void OnToggleInlineBlame(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        using var activity = VsixTelemetry.StartCommandActivity("GitRanger.ToggleInlineBlame");

        var options = GeneralOptions.Instance;
        if (options != null)
        {
            options.EnableInlineBlame = !options.EnableInlineBlame;
            options.Save();

            var status = options.EnableInlineBlame ? "enabled" : "disabled";
            activity?.SetTag("inline_blame.enabled", options.EnableInlineBlame);
            VsixTelemetry.LogInformation("Inline blame {Status}", status);
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
                ? "Disable Blame Gutter"
                : "Enable Blame Gutter";
            command.Enabled = true;
            command.Visible = true;
        }
    }

    private static void OnToggleBlameGutter(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        using var activity = VsixTelemetry.StartCommandActivity("GitRanger.ToggleBlameGutter");

        var options = GeneralOptions.Instance;
        if (options != null)
        {
            options.EnableBlameGutter = !options.EnableBlameGutter;
            options.Save();

            var status = options.EnableBlameGutter ? "enabled" : "disabled";
            activity?.SetTag("blame_gutter.enabled", options.EnableBlameGutter);
            VsixTelemetry.LogInformation("Blame gutter {Status}", status);
            VS.StatusBar.ShowMessageAsync($"Git Ranger: Blame gutter {status}").FireAndForget();
        }
    }

    private static void OnBeforeQueryStatusCopyCommitSha(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (sender is OleMenuCommand command)
        {
            var isInRepo = _gitService?.IsInRepository ?? false;
            command.Enabled = isInRepo;
            command.Visible = true;
        }
    }

    private static void OnCopyCommitSha(object sender, EventArgs e)
    {
        _ = OnCopyCommitShaAsync();
    }

    private static async Task OnCopyCommitShaAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        using var activity = VsixTelemetry.StartCommandActivity("GitRanger.CopyCommitSha");

        try
        {
            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
                return;

            var filePath = docView.FilePath;
            if (string.IsNullOrEmpty(filePath))
                return;

            var caretPosition = docView.TextView.Caret.Position.BufferPosition;
            var lineNumber = docView.TextView.TextSnapshot.GetLineNumberFromPosition(caretPosition.Position) + 1;

            activity?.SetTag("line.number", lineNumber);

            var blameInfo = _blameService?.GetBlameForLine(filePath, lineNumber);
            if (blameInfo != null)
            {
                System.Windows.Clipboard.SetText(blameInfo.CommitSha);
                activity?.SetTag("commit.sha", blameInfo.ShortSha);
                VsixTelemetry.LogInformation("Copied commit SHA {CommitSha} to clipboard", blameInfo.ShortSha);
                await VS.StatusBar.ShowMessageAsync($"Git Ranger: Copied commit SHA {blameInfo.ShortSha} to clipboard");
            }
            else
            {
                VsixTelemetry.LogInformation("No blame information available for line {LineNumber}", lineNumber);
                await VS.StatusBar.ShowMessageAsync("Git Ranger: No blame information available for this line");
            }
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            VsixTelemetry.TrackException(ex, new Dictionary<string, object>
            {
                { "operation.name", "CopyCommitSha" }
            });
            await VS.StatusBar.ShowMessageAsync($"Git Ranger: Error copying commit SHA - {ex.Message}");
        }
    }
}
