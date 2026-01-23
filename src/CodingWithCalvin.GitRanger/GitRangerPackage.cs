using System;
using System.Runtime.InteropServices;
using System.Threading;
using CodingWithCalvin.GitRanger.Commands;
using CodingWithCalvin.GitRanger.Options;
using CodingWithCalvin.GitRanger.Services;
using CodingWithCalvin.Otel4Vsix;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.GitRanger;

/// <summary>
/// Git Ranger - A visually exciting Git management extension for Visual Studio.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(VSCommandTableVsct.guidGitRangerPackageString)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.FolderOpened_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideOptionPage(typeof(GeneralOptionsPage), "Git Ranger", "General", 0, 0, true)]
public sealed class GitRangerPackage : ToolkitPackage
{
    private IStatusBarService? _statusBarService;

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited.
    /// </summary>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Initialize telemetry
        var builder = VsixTelemetry.Configure()
            .WithServiceName(VsixInfo.DisplayName)
            .WithServiceVersion(VsixInfo.Version)
            .WithVisualStudioAttributes(this)
            .WithEnvironmentAttributes();

#if !DEBUG
        builder
            .WithOtlpHttp("https://api.honeycomb.io")
            .WithHeader("x-honeycomb-team", HoneycombConfig.ApiKey);
#endif

        builder.Initialize();

        // Get services that need explicit initialization
        var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
        if (componentModel != null)
        {
            // ThemeService needs async initialization for theme detection
            var themeService = componentModel.GetService<IThemeService>();
            if (themeService != null)
            {
                await themeService.InitializeAsync();
            }

            // Keep reference to StatusBarService for disposal
            _statusBarService = componentModel.GetService<IStatusBarService>();
        }

        // Register commands
        await BlameCommands.InitializeAsync(this);

        // Log successful initialization
        VsixTelemetry.LogInformation("Git Ranger initialized successfully");
        await VS.StatusBar.ShowMessageAsync("Git Ranger initialized successfully");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusBarService?.Dispose();
            VsixTelemetry.Shutdown();
        }

        base.Dispose(disposing);
    }
}
