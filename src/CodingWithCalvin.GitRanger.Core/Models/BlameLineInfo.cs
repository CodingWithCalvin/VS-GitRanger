using System;

namespace CodingWithCalvin.GitRanger.Core.Models
{
    /// <summary>
    /// Represents blame information for a single line.
    /// </summary>
    public class BlameLineInfo
    {
        /// <summary>
        /// The 1-based line number.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The full commit SHA.
        /// </summary>
        public string CommitSha { get; set; } = string.Empty;

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
        /// The date/time of the commit.
        /// </summary>
        public DateTime AuthorDate { get; set; }

        /// <summary>
        /// The short commit message (first line).
        /// </summary>
        public string CommitMessage { get; set; } = string.Empty;

        /// <summary>
        /// The full commit message.
        /// </summary>
        public string FullCommitMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets a friendly relative time string (e.g., "2 days ago").
        /// </summary>
        public string RelativeTime => GetRelativeTime(AuthorDate);

        /// <summary>
        /// Gets the age of this commit in days.
        /// </summary>
        public int AgeDays => (int)(DateTime.Now - AuthorDate).TotalDays;

        private static string GetRelativeTime(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;

            if (span.TotalDays > 365)
            {
                var years = (int)(span.TotalDays / 365);
                return years == 1 ? "1 year ago" : $"{years} years ago";
            }

            if (span.TotalDays > 30)
            {
                var months = (int)(span.TotalDays / 30);
                return months == 1 ? "1 month ago" : $"{months} months ago";
            }

            if (span.TotalDays > 7)
            {
                var weeks = (int)(span.TotalDays / 7);
                return weeks == 1 ? "1 week ago" : $"{weeks} weeks ago";
            }

            if (span.TotalDays >= 1)
            {
                var days = (int)span.TotalDays;
                return days == 1 ? "1 day ago" : $"{days} days ago";
            }

            if (span.TotalHours >= 1)
            {
                var hours = (int)span.TotalHours;
                return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
            }

            if (span.TotalMinutes >= 1)
            {
                var minutes = (int)span.TotalMinutes;
                return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
            }

            return "just now";
        }
    }
}
