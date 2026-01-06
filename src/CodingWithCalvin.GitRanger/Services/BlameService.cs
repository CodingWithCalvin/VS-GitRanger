using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Core.Models;
using CodingWithCalvin.Otel4Vsix;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service for managing blame data with caching and background loading.
    /// </summary>
    public class BlameService
    {
        private readonly GitService _gitService;
        private readonly ThemeService _themeService;
        private readonly ConcurrentDictionary<string, BlameCache> _cache = new ConcurrentDictionary<string, BlameCache>(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Fired when blame data is loaded for a file.
        /// </summary>
        public event EventHandler<BlameLoadedEventArgs>? BlameLoaded;

        public BlameService(GitService gitService, ThemeService themeService)
        {
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        }

        /// <summary>
        /// Gets blame information for a file, using cache if available.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Blame line information, or empty if not available.</returns>
        public IReadOnlyList<BlameLineInfo> GetBlame(string filePath)
        {
            using var activity = VsixTelemetry.StartCommandActivity("BlameService.GetBlame");

            activity?.SetTag("file.path", filePath);

            if (string.IsNullOrEmpty(filePath))
                return Array.Empty<BlameLineInfo>();

            // Check cache
            if (_cache.TryGetValue(filePath, out var cached) && !cached.IsExpired)
            {
                activity?.SetTag("cache.hit", true);
                return cached.Lines;
            }

            activity?.SetTag("cache.hit", false);

            // Load synchronously
            var lines = _gitService.GetBlame(filePath).ToList();

            // Update cache
            _cache[filePath] = new BlameCache(lines);

            activity?.SetTag("lines.count", lines.Count);

            return lines;
        }

        /// <summary>
        /// Gets blame information for a file asynchronously.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Blame line information.</returns>
        public Task<IReadOnlyList<BlameLineInfo>> GetBlameAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GetBlame(filePath), cancellationToken);
        }

        /// <summary>
        /// Loads blame data in the background and fires BlameLoaded when complete.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void LoadBlameInBackground(string filePath)
        {
            using var activity = VsixTelemetry.StartCommandActivity("BlameService.LoadBlameInBackground");

            activity?.SetTag("file.path", filePath);

            if (string.IsNullOrEmpty(filePath))
            {
                VsixTelemetry.LogInformation("LoadBlameInBackground - FilePath is empty");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    VsixTelemetry.LogInformation("Loading blame for {FilePath}", filePath);
                    var lines = GetBlame(filePath);
                    VsixTelemetry.LogInformation("Loaded {LineCount} blame lines for {FilePath}", lines.Count, filePath);
                    BlameLoaded?.Invoke(this, new BlameLoadedEventArgs(filePath, lines));
                }
                catch (Exception ex)
                {
                    VsixTelemetry.TrackException(ex, new Dictionary<string, object>
                    {
                        { "operation.name", "LoadBlameInBackground" },
                        { "file.path", filePath }
                    });
                }
            });
        }

        /// <summary>
        /// Gets blame information for a specific line.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The 1-based line number.</param>
        /// <returns>Blame info for the line, or null if not available.</returns>
        public BlameLineInfo? GetBlameForLine(string filePath, int lineNumber)
        {
            var lines = GetBlame(filePath);
            return lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        }

        /// <summary>
        /// Gets blame information for a range of lines.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="startLine">The start line (1-based, inclusive).</param>
        /// <param name="endLine">The end line (1-based, inclusive).</param>
        /// <returns>Blame info for the lines in range.</returns>
        public IReadOnlyList<BlameLineInfo> GetBlameForLines(string filePath, int startLine, int endLine)
        {
            var lines = GetBlame(filePath);
            return lines.Where(l => l.LineNumber >= startLine && l.LineNumber <= endLine).ToList();
        }

        /// <summary>
        /// Invalidates the cache for a specific file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void InvalidateCache(string filePath)
        {
            _cache.TryRemove(filePath, out _);
        }

        /// <summary>
        /// Clears all cached blame data.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Ensures blame is loaded for a file, loading if necessary.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if blame data is available.</returns>
        public bool EnsureBlameLoaded(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Check if already in cache and valid
            if (_cache.TryGetValue(filePath, out var cached) && !cached.IsExpired)
                return cached.Lines.Count > 0;

            // Try to open repository
            if (!_gitService.TryOpenRepository(filePath))
                return false;

            // Load blame data
            var lines = GetBlame(filePath);
            return lines.Count > 0;
        }

        private class BlameCache
        {
            public IReadOnlyList<BlameLineInfo> Lines { get; }
            public DateTime LoadedAt { get; }

            public bool IsExpired => DateTime.Now - LoadedAt > CacheExpiry;

            public BlameCache(IReadOnlyList<BlameLineInfo> lines)
            {
                Lines = lines;
                LoadedAt = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Event arguments for blame loaded events.
    /// </summary>
    public class BlameLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// The file path that was loaded.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The blame line information.
        /// </summary>
        public IReadOnlyList<BlameLineInfo> Lines { get; }

        public BlameLoadedEventArgs(string filePath, IReadOnlyList<BlameLineInfo> lines)
        {
            FilePath = filePath;
            Lines = lines;
        }
    }
}
