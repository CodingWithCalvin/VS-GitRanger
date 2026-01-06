using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Commands;
using CodingWithCalvin.GitRanger.Options;
using CodingWithCalvin.GitRanger.Services;
using CodingWithCalvin.Otel4Vsix;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.GitRanger
{
    /// <summary>
    /// Git Ranger - A visually exciting Git management extension for Visual Studio.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidGitRangerPackageString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.FolderOpened_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(GeneralOptionsPage), "Git Ranger", "General", 0, 0, true)]
    public sealed class GitRangerPackage : ToolkitPackage
    {
        private static readonly object _initLock = new object();
        private static bool _servicesInitialized = false;

        /// <summary>
        /// The Git service instance for repository operations.
        /// </summary>
        public static GitService? GitService { get; private set; }

        /// <summary>
        /// The Theme service instance for VS theme adaptation.
        /// </summary>
        public static ThemeService? ThemeService { get; private set; }

        /// <summary>
        /// The Blame service instance for blame operations.
        /// </summary>
        public static BlameService? BlameService { get; private set; }

        /// <summary>
        /// Ensures that services are initialized. Can be called from MEF components.
        /// </summary>
        public static void EnsureServicesInitialized()
        {
            if (_servicesInitialized)
                return;

            lock (_initLock)
            {
                if (_servicesInitialized)
                    return;

                // Initialize services synchronously if not already done
                if (ThemeService == null)
                {
                    ThemeService = new ThemeService();
                    // Note: InitializeAsync needs to run on UI thread,
                    // but we can still create the service
                }

                if (GitService == null)
                {
                    GitService = new GitService();
                }

                if (BlameService == null && GitService != null && ThemeService != null)
                {
                    BlameService = new BlameService(GitService, ThemeService);
                }

                _servicesInitialized = true;
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Switch to the main thread for telemetry initialization
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

            // Initialize services
            await InitializeServicesAsync();

            // Register commands
            await RegisterCommandsAsync();

            // Log successful initialization
            VsixTelemetry.LogInformation("Git Ranger initialized successfully");
            await VS.StatusBar.ShowMessageAsync("Git Ranger initialized successfully");
        }

        private async Task InitializeServicesAsync()
        {
            lock (_initLock)
            {
                // Initialize the theme service first (needed for colors)
                if (ThemeService == null)
                {
                    ThemeService = new ThemeService();
                }

                // Initialize the Git service
                if (GitService == null)
                {
                    GitService = new GitService();
                }

                // Initialize the Blame service (depends on Git and Theme services)
                if (BlameService == null)
                {
                    BlameService = new BlameService(GitService, ThemeService);
                }

                _servicesInitialized = true;
            }

            // Initialize theme service async (needs UI thread)
            await ThemeService.InitializeAsync();
        }

        private async Task RegisterCommandsAsync()
        {
            // Register blame commands (toggle inline/gutter, copy SHA)
            await BlameCommands.InitializeAsync(this);
            // Note: HistoryCommands and GraphCommands are not registered yet (coming soon)
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                VsixTelemetry.Shutdown();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Package GUIDs.
    /// </summary>
    public static class PackageGuids
    {
        public const string guidGitRangerPackageString = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890";
        public const string guidGitRangerPackageCmdSetString = "B2C3D4E5-F6A7-8901-BCDE-F12345678902";

        public static readonly Guid guidGitRangerPackage = new Guid(guidGitRangerPackageString);
        public static readonly Guid guidGitRangerPackageCmdSet = new Guid(guidGitRangerPackageCmdSetString);
    }

    /// <summary>
    /// Command IDs.
    /// </summary>
    public static class PackageIds
    {
        public const int cmdidToggleInlineBlame = 0x0100;
        public const int cmdidToggleBlameGutter = 0x0101;
        public const int cmdidCopyCommitSha = 0x0106;
    }
}
