using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CodingWithCalvin.GitRanger.Core.Models;
using LibGit2Sharp;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service for Git repository operations.
    /// </summary>
    public class GitService
    {
        private Repository? _currentRepository;
        private string? _currentRepositoryPath;

        /// <summary>
        /// Gets the current repository path, if available.
        /// </summary>
        public string? CurrentRepositoryPath => _currentRepositoryPath;

        /// <summary>
        /// Gets whether we're currently in a Git repository.
        /// </summary>
        public bool IsInRepository => _currentRepository != null;

        /// <summary>
        /// Gets the current branch name.
        /// </summary>
        public string? CurrentBranchName => _currentRepository?.Head?.FriendlyName;

        /// <summary>
        /// Discovers and opens a Git repository for the given file path.
        /// </summary>
        /// <param name="filePath">A file path within the repository.</param>
        /// <returns>True if a repository was found, false otherwise.</returns>
        public bool TryOpenRepository(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                var repoPath = Repository.Discover(filePath);
                if (string.IsNullOrEmpty(repoPath))
                    return false;

                // Only reopen if it's a different repository
                if (_currentRepositoryPath != repoPath)
                {
                    _currentRepository?.Dispose();
                    _currentRepository = new Repository(repoPath);
                    _currentRepositoryPath = repoPath;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets blame information for a file.
        /// </summary>
        /// <param name="filePath">The file path to blame.</param>
        /// <returns>A collection of blame line information.</returns>
        public IReadOnlyList<BlameLineInfo> GetBlame(string filePath)
        {
            Debug.WriteLine($"[GitRanger] GitService.GetBlame - FilePath: {filePath}");
            Debug.WriteLine($"[GitRanger] GitService.GetBlame - CurrentRepository: {(_currentRepository == null ? "NULL" : "OK")}");

            if (_currentRepository == null || string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine("[GitRanger] GitService.GetBlame - Repository null or filepath empty, returning empty");
                return Array.Empty<BlameLineInfo>();
            }

            try
            {
                // Get the relative path within the repository
                var repoRoot = _currentRepository.Info.WorkingDirectory;
                var relativePath = GetRelativePath(repoRoot, filePath);

                Debug.WriteLine($"[GitRanger] GitService.GetBlame - RepoRoot: {repoRoot}");
                Debug.WriteLine($"[GitRanger] GitService.GetBlame - RelativePath: {relativePath}");

                if (string.IsNullOrEmpty(relativePath))
                {
                    Debug.WriteLine("[GitRanger] GitService.GetBlame - RelativePath is empty, returning empty");
                    return Array.Empty<BlameLineInfo>();
                }

                Debug.WriteLine($"[GitRanger] GitService.GetBlame - Calling Repository.Blame for {relativePath}");
                var blameHunks = _currentRepository.Blame(relativePath);
                var results = new List<BlameLineInfo>();
                var lineNumber = 1;

                foreach (var hunk in blameHunks)
                {
                    for (var i = 0; i < hunk.LineCount; i++)
                    {
                        results.Add(new BlameLineInfo
                        {
                            LineNumber = lineNumber++,
                            CommitSha = hunk.FinalCommit.Sha,
                            ShortSha = hunk.FinalCommit.Sha.Substring(0, 7),
                            Author = hunk.FinalCommit.Author.Name,
                            AuthorEmail = hunk.FinalCommit.Author.Email,
                            AuthorDate = hunk.FinalCommit.Author.When.DateTime,
                            CommitMessage = hunk.FinalCommit.MessageShort,
                            FullCommitMessage = hunk.FinalCommit.Message
                        });
                    }
                }

                Debug.WriteLine($"[GitRanger] GitService.GetBlame - Success! Got {results.Count} blame lines");
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GitRanger] GitService.GetBlame - ERROR: {ex.Message}");
                Debug.WriteLine($"[GitRanger] GitService.GetBlame - StackTrace: {ex.StackTrace}");
                // Return empty on error (file not tracked, etc.)
                return Array.Empty<BlameLineInfo>();
            }
        }

        /// <summary>
        /// Gets blame information for a specific line.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The 1-based line number.</param>
        /// <returns>Blame info for the line, or null if not available.</returns>
        public BlameLineInfo? GetBlameForLine(string filePath, int lineNumber)
        {
            return GetBlame(filePath).FirstOrDefault(b => b.LineNumber == lineNumber);
        }

        /// <summary>
        /// Gets the file history (commits that modified this file).
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>A collection of commits.</returns>
        public IReadOnlyList<CommitInfo> GetFileHistory(string filePath)
        {
            if (_currentRepository == null || string.IsNullOrEmpty(filePath))
                return Array.Empty<CommitInfo>();

            try
            {
                var repoRoot = _currentRepository.Info.WorkingDirectory;
                var relativePath = GetRelativePath(repoRoot, filePath);

                if (string.IsNullOrEmpty(relativePath))
                    return Array.Empty<CommitInfo>();

                var filter = new CommitFilter
                {
                    SortBy = CommitSortStrategies.Time
                };

                var results = new List<CommitInfo>();

                foreach (var commit in _currentRepository.Commits.QueryBy(relativePath, filter).Select(e => e.Commit))
                {
                    results.Add(new CommitInfo
                    {
                        Sha = commit.Sha,
                        ShortSha = commit.Sha.Substring(0, 7),
                        Author = commit.Author.Name,
                        AuthorEmail = commit.Author.Email,
                        AuthorDate = commit.Author.When.DateTime,
                        Committer = commit.Committer.Name,
                        CommitterEmail = commit.Committer.Email,
                        CommitterDate = commit.Committer.When.DateTime,
                        MessageShort = commit.MessageShort,
                        Message = commit.Message,
                        ParentCount = commit.Parents.Count()
                    });
                }

                return results;
            }
            catch (Exception)
            {
                return Array.Empty<CommitInfo>();
            }
        }

        /// <summary>
        /// Gets all commits in the repository for the graph view.
        /// </summary>
        /// <param name="maxCount">Maximum number of commits to retrieve.</param>
        /// <returns>A collection of commits with branch/merge info.</returns>
        public IReadOnlyList<GraphCommitInfo> GetCommitGraph(int maxCount = 1000)
        {
            if (_currentRepository == null)
                return Array.Empty<GraphCommitInfo>();

            try
            {
                var results = new List<GraphCommitInfo>();
                var count = 0;

                foreach (var commit in _currentRepository.Commits)
                {
                    if (count++ >= maxCount)
                        break;

                    results.Add(new GraphCommitInfo
                    {
                        Sha = commit.Sha,
                        ShortSha = commit.Sha.Substring(0, 7),
                        Author = commit.Author.Name,
                        AuthorEmail = commit.Author.Email,
                        AuthorDate = commit.Author.When.DateTime,
                        MessageShort = commit.MessageShort,
                        Message = commit.Message,
                        ParentShas = commit.Parents.Select(p => p.Sha).ToArray(),
                        ParentCount = commit.Parents.Count()
                    });
                }

                return results;
            }
            catch (Exception)
            {
                return Array.Empty<GraphCommitInfo>();
            }
        }

        /// <summary>
        /// Gets all branches in the repository.
        /// </summary>
        public IReadOnlyList<BranchInfo> GetBranches()
        {
            if (_currentRepository == null)
                return Array.Empty<BranchInfo>();

            var results = new List<BranchInfo>();

            foreach (var branch in _currentRepository.Branches)
            {
                results.Add(new BranchInfo
                {
                    Name = branch.FriendlyName,
                    IsRemote = branch.IsRemote,
                    IsHead = branch.IsCurrentRepositoryHead,
                    TipSha = branch.Tip?.Sha ?? string.Empty,
                    TrackingBranchName = branch.TrackedBranch?.FriendlyName
                });
            }

            return results;
        }

        /// <summary>
        /// Disposes the current repository.
        /// </summary>
        public void Dispose()
        {
            _currentRepository?.Dispose();
            _currentRepository = null;
            _currentRepositoryPath = null;
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
                return string.Empty;

            // Normalize paths
            basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            fullPath = Path.GetFullPath(fullPath);

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            var relativePath = fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Git uses forward slashes
            return relativePath.Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
