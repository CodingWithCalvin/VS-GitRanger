using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodingWithCalvin.GitRanger.Core.Models;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service interface for managing blame data with caching and background loading.
    /// </summary>
    public interface IBlameService
    {
        /// <summary>
        /// Fired when blame data is loaded for a file.
        /// </summary>
        event EventHandler<BlameLoadedEventArgs>? BlameLoaded;

        /// <summary>
        /// Gets blame information for a file, using cache if available.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Blame line information, or empty if not available.</returns>
        IReadOnlyList<BlameLineInfo> GetBlame(string filePath);

        /// <summary>
        /// Gets blame information for a file asynchronously.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Blame line information.</returns>
        Task<IReadOnlyList<BlameLineInfo>> GetBlameAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads blame data in the background and fires BlameLoaded when complete.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        void LoadBlameInBackground(string filePath);

        /// <summary>
        /// Gets blame information for a specific line.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The 1-based line number.</param>
        /// <returns>Blame info for the line, or null if not available.</returns>
        BlameLineInfo? GetBlameForLine(string filePath, int lineNumber);

        /// <summary>
        /// Gets blame information for a range of lines.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="startLine">The start line (1-based, inclusive).</param>
        /// <param name="endLine">The end line (1-based, inclusive).</param>
        /// <returns>Blame info for the lines in range.</returns>
        IReadOnlyList<BlameLineInfo> GetBlameForLines(string filePath, int startLine, int endLine);

        /// <summary>
        /// Invalidates the cache for a specific file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        void InvalidateCache(string filePath);

        /// <summary>
        /// Clears all cached blame data.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Ensures blame is loaded for a file, loading if necessary.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if blame data is available.</returns>
        bool EnsureBlameLoaded(string filePath);
    }
}
