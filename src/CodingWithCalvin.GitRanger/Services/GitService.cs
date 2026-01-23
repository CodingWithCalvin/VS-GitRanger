using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    [Export(typeof(IGitService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class GitService : IGitService
    {
        private readonly IOutputPaneService _outputPane;
        private Repository? _currentRepository;
        private string? _currentRepositoryPath;

        [ImportingConstructor]
        public GitService(IOutputPaneService outputPane)
        {
            _outputPane = outputPane ?? throw new ArgumentNullException(nameof(outputPane));

            // Disable owner validation to avoid "not owned by current user" errors
            // This is safe for a VS extension since the user explicitly opened these files
            try
            {
                GlobalSettings.SetOwnerValidation(false);
                _outputPane.WriteInfo("GitService created (owner validation disabled)");
            }
            catch (Exception ex)
            {
                _outputPane.WriteError("GitService: could not disable owner validation: {0}", ex.Message);
            }
        }

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
            _outputPane.WriteVerbose("GitService.TryOpenRepository: {0}", filePath);

            if (string.IsNullOrEmpty(filePath))
            {
                _outputPane.WriteVerbose("  - FilePath is empty");
                return false;
            }

            string? repoPath = null;
            try
            {
                repoPath = Repository.Discover(filePath);
                if (string.IsNullOrEmpty(repoPath))
                {
                    _outputPane.WriteVerbose("  - No repository found");
                    return false;
                }

                // Only reopen if it's a different repository
                if (_currentRepositoryPath != repoPath)
                {
                    _outputPane.WriteInfo("Opening repository: {0}", repoPath);
                    _currentRepository?.Dispose();
                    _currentRepository = new Repository(repoPath);
                    _currentRepositoryPath = repoPath;
                }
                else
                {
                    _outputPane.WriteVerbose("  - Using existing repository");
                }

                return true;
            }
            catch (Exception ex)
            {
                _outputPane.WriteError("GitService.TryOpenRepository failed: {0}", ex.Message);

                // Provide helpful message for common Git safe.directory issue
                if (ex.Message.Contains("not owned by current user"))
                {
                    var safePath = repoPath?.TrimEnd('/', '\\') ?? Path.GetDirectoryName(filePath) ?? filePath;
                    _outputPane.WriteError("*** Git Safe Directory Issue ***");
                    _outputPane.WriteError("To fix, run: git config --global --add safe.directory \"{0}\"", safePath);
                    _outputPane.WriteError("Or to trust all: git config --global --add safe.directory '*'");
                }

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
            _outputPane.WriteVerbose("GitService.GetBlame: {0}", filePath);

            if (_currentRepository == null || string.IsNullOrEmpty(filePath))
            {
                _outputPane.WriteVerbose("  - Repository null or filepath empty");
                return Array.Empty<BlameLineInfo>();
            }

            try
            {
                // Get the relative path within the repository
                var repoRoot = _currentRepository.Info.WorkingDirectory;
                var relativePath = GetRelativePath(repoRoot, filePath);

                _outputPane.WriteVerbose("  - RelativePath: {0}", relativePath);

                if (string.IsNullOrEmpty(relativePath))
                {
                    _outputPane.WriteVerbose("  - RelativePath is empty");
                    return Array.Empty<BlameLineInfo>();
                }

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

                _outputPane.WriteVerbose("  - Got {0} blame lines", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _outputPane.WriteError("GitService.GetBlame failed: {0}", ex.Message);
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
