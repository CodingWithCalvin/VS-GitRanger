using System.ComponentModel;
using Community.VisualStudio.Toolkit;

namespace CodingWithCalvin.GitRanger.Options
{
    /// <summary>
    /// General options for Git Ranger extension.
    /// </summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        // Blame Settings
        [Category("Blame")]
        [DisplayName("Enable Inline Blame")]
        [Description("Show blame information at the end of each line in the editor.")]
        [DefaultValue(true)]
        public bool EnableInlineBlame { get; set; } = true;

        [Category("Blame")]
        [DisplayName("Enable Blame Gutter")]
        [Description("Show blame information in the editor margin/gutter.")]
        [DefaultValue(true)]
        public bool EnableBlameGutter { get; set; } = true;

        [Category("Blame")]
        [DisplayName("Show Author Name")]
        [Description("Display the author's name in inline blame annotations.")]
        [DefaultValue(true)]
        public bool ShowAuthorName { get; set; } = true;

        [Category("Blame")]
        [DisplayName("Show Commit Date")]
        [Description("Display the commit date in inline blame annotations.")]
        [DefaultValue(true)]
        public bool ShowCommitDate { get; set; } = true;

        [Category("Blame")]
        [DisplayName("Show Commit Message")]
        [Description("Display the commit message in inline blame annotations.")]
        [DefaultValue(true)]
        public bool ShowCommitMessage { get; set; } = true;

        [Category("Blame")]
        [DisplayName("Date Format")]
        [Description("Format for displaying dates. Use 'relative' for relative times (e.g., '2 days ago') or a custom format string.")]
        [DefaultValue("relative")]
        public string DateFormat { get; set; } = "relative";

        // Colors
        [Category("Colors")]
        [DisplayName("Color Mode")]
        [Description("How to colorize blame annotations: 'author' for author-based colors, 'age' for age-based heat map, 'none' for no colors.")]
        [DefaultValue(ColorMode.Author)]
        public ColorMode BlameColorMode { get; set; } = ColorMode.Author;

        [Category("Colors")]
        [DisplayName("Max Age for Heat Map (days)")]
        [Description("Maximum age in days for the age heat map. Commits older than this will show as the oldest color.")]
        [DefaultValue(365)]
        public int MaxAgeDays { get; set; } = 365;

        // Performance
        [Category("Performance")]
        [DisplayName("Cache Duration (minutes)")]
        [Description("How long to cache blame data in minutes. Set to 0 to disable caching.")]
        [DefaultValue(5)]
        public int CacheDurationMinutes { get; set; } = 5;

        [Category("Performance")]
        [DisplayName("Enable Lazy Loading")]
        [Description("Only load blame data for visible lines in the editor viewport.")]
        [DefaultValue(true)]
        public bool EnableLazyLoading { get; set; } = true;

        // Display
        [Category("Display")]
        [DisplayName("Inline Blame Opacity")]
        [Description("Opacity of inline blame annotations (0.0 to 1.0).")]
        [DefaultValue(0.7)]
        public double InlineBlameOpacity { get; set; } = 0.7;

        [Category("Display")]
        [DisplayName("Compact Mode")]
        [Description("Use a more compact display format for blame annotations.")]
        [DefaultValue(false)]
        public bool CompactMode { get; set; } = false;

        [Category("Display")]
        [DisplayName("Show On Hover Only")]
        [Description("Only show detailed blame information on mouse hover.")]
        [DefaultValue(false)]
        public bool ShowOnHoverOnly { get; set; } = false;

        // Gutter Settings
        [Category("Gutter")]
        [DisplayName("Gutter Width")]
        [Description("Width of the blame gutter in pixels.")]
        [DefaultValue(40)]
        public int GutterWidth { get; set; } = 40;

        [Category("Gutter")]
        [DisplayName("Show Age Bars")]
        [Description("Show age indicator bars in the gutter.")]
        [DefaultValue(true)]
        public bool ShowAgeBars { get; set; } = true;

        // Status Bar
        [Category("Status Bar")]
        [DisplayName("Enable Status Bar Blame")]
        [Description("Show blame information in the status bar for the current line.")]
        [DefaultValue(false)]
        public bool EnableStatusBarBlame { get; set; } = false;

        [Category("Status Bar")]
        [DisplayName("Format")]
        [Description("Format template using placeholders: {author}, {date}, {message}, {sha}")]
        [DefaultValue("{author}, {date} \u2022 {message}")]
        public string StatusBarFormat { get; set; } = "{author}, {date} \u2022 {message}";

        [Category("Status Bar")]
        [DisplayName("Use Relative Dates")]
        [Description("Show relative dates (e.g., '2 days ago') instead of absolute dates.")]
        [DefaultValue(true)]
        public bool StatusBarRelativeDate { get; set; } = true;

        [Category("Status Bar")]
        [DisplayName("Maximum Length")]
        [Description("Maximum characters to display before truncating. Set to 0 for no limit.")]
        [DefaultValue(100)]
        public int StatusBarMaxLength { get; set; } = 100;

        // Diagnostics
        [Category("Diagnostics")]
        [DisplayName("Log Level")]
        [Description("Controls output pane verbosity. None=disabled, Error=failures only, Info=key events, Verbose=detailed tracing.")]
        [DefaultValue(LogLevel.Error)]
        public LogLevel LogLevel { get; set; } = LogLevel.Error;
    }

    /// <summary>
    /// Color mode for blame annotations.
    /// </summary>
    public enum ColorMode
    {
        /// <summary>
        /// No color coding.
        /// </summary>
        None,

        /// <summary>
        /// Color by author.
        /// </summary>
        Author,

        /// <summary>
        /// Color by commit age (heat map).
        /// </summary>
        Age
    }

    /// <summary>
    /// Log level for output pane messages.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// No logging.
        /// </summary>
        None,

        /// <summary>
        /// Only errors and failures.
        /// </summary>
        Error,

        /// <summary>
        /// Key lifecycle events and errors.
        /// </summary>
        Info,

        /// <summary>
        /// Detailed tracing for debugging.
        /// </summary>
        Verbose
    }
}
