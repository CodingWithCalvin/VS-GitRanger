using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Options;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.GitRanger.Services;

/// <summary>
/// Service for writing to the Git Ranger output pane.
/// </summary>
[Export(typeof(IOutputPaneService))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class OutputPaneService : IOutputPaneService
{
    private const string PaneName = "Git Ranger";
    private OutputWindowPane? _pane;
    private readonly object _lock = new();

    public void WriteError(string message)
    {
        WriteAtLevel(LogLevel.Error, message);
    }

    public void WriteError(string format, params object[] args)
    {
        WriteError(string.Format(format, args));
    }

    public void WriteInfo(string message)
    {
        WriteAtLevel(LogLevel.Info, message);
    }

    public void WriteInfo(string format, params object[] args)
    {
        WriteInfo(string.Format(format, args));
    }

    public void WriteVerbose(string message)
    {
        WriteAtLevel(LogLevel.Verbose, message);
    }

    public void WriteVerbose(string format, params object[] args)
    {
        WriteVerbose(string.Format(format, args));
    }

    private void WriteAtLevel(LogLevel messageLevel, string message)
    {
        var configuredLevel = GeneralOptions.Instance?.LogLevel ?? LogLevel.Error;

        // None means no logging at all
        if (configuredLevel == LogLevel.None)
            return;

        // Only write if message level is at or below configured level
        if (messageLevel > configuredLevel)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var levelPrefix = messageLevel switch
        {
            LogLevel.Error => "ERROR",
            LogLevel.Info => "INFO",
            LogLevel.Verbose => "VERBOSE",
            _ => ""
        };
        var formattedMessage = $"[{timestamp}] [{levelPrefix}] {message}";

        _ = WriteLineInternalAsync(formattedMessage);
    }

    private async Task WriteLineInternalAsync(string message)
    {
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var pane = await GetOrCreatePaneAsync();
            if (pane != null)
            {
                await pane.WriteLineAsync(message);
            }
        }
        catch
        {
            // Silently fail - we don't want logging to break the extension
        }
    }

    public async Task ActivateAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var pane = await GetOrCreatePaneAsync();
        if (pane != null)
        {
            await pane.ActivateAsync();
        }
    }

    private async Task<OutputWindowPane?> GetOrCreatePaneAsync()
    {
        if (_pane != null)
            return _pane;

        lock (_lock)
        {
            if (_pane != null)
                return _pane;
        }

        try
        {
            _pane = await VS.Windows.CreateOutputWindowPaneAsync(PaneName);
            return _pane;
        }
        catch
        {
            return null;
        }
    }
}
