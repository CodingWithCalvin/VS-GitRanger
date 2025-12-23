using System;

namespace CodingWithCalvin.GitRanger.Core.Models
{
    /// <summary>
    /// Represents information about a Git commit.
    /// </summary>
    public class CommitInfo
    {
        /// <summary>
        /// The full commit SHA.
        /// </summary>
        public string Sha { get; set; } = string.Empty;

        /// <summary>
        /// The short (7 character) commit SHA.
        /// </summary>
        public string ShortSha { get; set; } = string.Empty;

        /// <summary>
        /// The author's name.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// The author's email.
        /// </summary>
        public string AuthorEmail { get; set; } = string.Empty;

        /// <summary>
        /// The date/time when authored.
        /// </summary>
        public DateTime AuthorDate { get; set; }

        /// <summary>
        /// The committer's name.
        /// </summary>
        public string Committer { get; set; } = string.Empty;

        /// <summary>
        /// The committer's email.
        /// </summary>
        public string CommitterEmail { get; set; } = string.Empty;

        /// <summary>
        /// The date/time when committed.
        /// </summary>
        public DateTime CommitterDate { get; set; }

        /// <summary>
        /// The short commit message (first line).
        /// </summary>
        public string MessageShort { get; set; } = string.Empty;

        /// <summary>
        /// The full commit message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Number of parent commits.
        /// </summary>
        public int ParentCount { get; set; }

        /// <summary>
        /// Gets whether this is a merge commit.
        /// </summary>
        public bool IsMergeCommit => ParentCount > 1;
    }

    /// <summary>
    /// Extended commit info for graph visualization.
    /// </summary>
    public class GraphCommitInfo : CommitInfo
    {
        /// <summary>
        /// The SHA hashes of parent commits.
        /// </summary>
        public string[] ParentShas { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The graph column position (set during layout).
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// The graph row position (set during layout).
        /// </summary>
        public int Row { get; set; }
    }

    /// <summary>
    /// Represents information about a Git branch.
    /// </summary>
    public class BranchInfo
    {
        /// <summary>
        /// The branch name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a remote tracking branch.
        /// </summary>
        public bool IsRemote { get; set; }

        /// <summary>
        /// Whether this is the current HEAD branch.
        /// </summary>
        public bool IsHead { get; set; }

        /// <summary>
        /// The SHA of the branch tip commit.
        /// </summary>
        public string TipSha { get; set; } = string.Empty;

        /// <summary>
        /// The name of the tracked upstream branch, if any.
        /// </summary>
        public string? TrackingBranchName { get; set; }
    }
}
