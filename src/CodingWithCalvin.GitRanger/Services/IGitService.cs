using System.Collections.Generic;
using CodingWithCalvin.GitRanger.Core.Models;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service interface for Git repository operations.
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// Gets the current repository path, if available.
        /// </summary>
        string? CurrentRepositoryPath { get; }

        /// <summary>
        /// Gets whether we're currently in a Git repository.
        /// </summary>
        bool IsInRepository { get; }

        /// <summary>
        /// Gets the current branch name.
        /// </summary>
        string? CurrentBranchName { get; }

        /// <summary>
        /// Discovers and opens a Git repository for the given file path.
        /// </summary>
        /// <param name="filePath">A file path within the repository.</param>
        /// <returns>True if a repository was found, false otherwise.</returns>
        bool TryOpenRepository(string filePath);

        /// <summary>
        /// Gets blame information for a file.
        /// </summary>
        /// <param name="filePath">The file path to blame.</param>
        /// <returns>A collection of blame line information.</returns>
        IReadOnlyList<BlameLineInfo> GetBlame(string filePath);

        /// <summary>
        /// Gets blame information for a specific line.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The 1-based line number.</param>
        /// <returns>Blame info for the line, or null if not available.</returns>
        BlameLineInfo? GetBlameForLine(string filePath, int lineNumber);

        /// <summary>
        /// Gets the file history (commits that modified this file).
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>A collection of commits.</returns>
        IReadOnlyList<CommitInfo> GetFileHistory(string filePath);

        /// <summary>
        /// Gets all commits in the repository for the graph view.
        /// </summary>
        /// <param name="maxCount">Maximum number of commits to retrieve.</param>
        /// <returns>A collection of commits with branch/merge info.</returns>
        IReadOnlyList<GraphCommitInfo> GetCommitGraph(int maxCount = 1000);

        /// <summary>
        /// Gets all branches in the repository.
        /// </summary>
        IReadOnlyList<BranchInfo> GetBranches();

        /// <summary>
        /// Disposes the current repository.
        /// </summary>
        void Dispose();
    }
}
