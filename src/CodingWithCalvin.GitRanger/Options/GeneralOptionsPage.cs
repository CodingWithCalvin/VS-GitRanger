using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.GitRanger.Options
{
    /// <summary>
    /// Options page that appears in Visual Studio's Tools > Options dialog.
    /// </summary>
    [ComVisible(true)]
    [Guid("E7F8A9B0-C1D2-E3F4-A5B6-C7D8E9F0A1B2")]
    public class GeneralOptionsPage : DialogPage
    {
        private GeneralOptions _options = GeneralOptions.Instance;

        // Blame Settings
        [Category("Blame")]
        [DisplayName("Enable Inline Blame")]
        [Description("Show blame information at the end of each line in the editor.")]
        public bool EnableInlineBlame
        {
            get => _options.EnableInlineBlame;
            set => _options.EnableInlineBlame = value;
        }

        [Category("Blame")]
        [DisplayName("Enable Blame Gutter")]
        [Description("Show blame information in the editor margin/gutter.")]
        public bool EnableBlameGutter
        {
            get => _options.EnableBlameGutter;
            set => _options.EnableBlameGutter = value;
        }

        [Category("Blame")]
        [DisplayName("Show Author Name")]
        [Description("Display the author's name in inline blame annotations.")]
        public bool ShowAuthorName
        {
            get => _options.ShowAuthorName;
            set => _options.ShowAuthorName = value;
        }

        [Category("Blame")]
        [DisplayName("Show Commit Date")]
        [Description("Display the commit date in inline blame annotations.")]
        public bool ShowCommitDate
        {
            get => _options.ShowCommitDate;
            set => _options.ShowCommitDate = value;
        }

        [Category("Blame")]
        [DisplayName("Show Commit Message")]
        [Description("Display the commit message in inline blame annotations.")]
        public bool ShowCommitMessage
        {
            get => _options.ShowCommitMessage;
            set => _options.ShowCommitMessage = value;
        }

        [Category("Blame")]
        [DisplayName("Date Format")]
        [Description("Format for displaying dates. Use 'relative' for relative times or a custom format string.")]
        public string DateFormat
        {
            get => _options.DateFormat;
            set => _options.DateFormat = value;
        }

        // Colors
        [Category("Colors")]
        [DisplayName("Color Mode")]
        [Description("How to colorize blame annotations.")]
        public ColorMode BlameColorMode
        {
            get => _options.BlameColorMode;
            set => _options.BlameColorMode = value;
        }

        [Category("Colors")]
        [DisplayName("Max Age for Heat Map (days)")]
        [Description("Maximum age in days for the age heat map.")]
        public int MaxAgeDays
        {
            get => _options.MaxAgeDays;
            set => _options.MaxAgeDays = value;
        }

        // Performance
        [Category("Performance")]
        [DisplayName("Cache Duration (minutes)")]
        [Description("How long to cache blame data in minutes.")]
        public int CacheDurationMinutes
        {
            get => _options.CacheDurationMinutes;
            set => _options.CacheDurationMinutes = value;
        }

        [Category("Performance")]
        [DisplayName("Enable Lazy Loading")]
        [Description("Only load blame data for visible lines.")]
        public bool EnableLazyLoading
        {
            get => _options.EnableLazyLoading;
            set => _options.EnableLazyLoading = value;
        }

        // Display
        [Category("Display")]
        [DisplayName("Inline Blame Opacity")]
        [Description("Opacity of inline blame annotations (0.0 to 1.0).")]
        public double InlineBlameOpacity
        {
            get => _options.InlineBlameOpacity;
            set => _options.InlineBlameOpacity = Math.Max(0.0, Math.Min(1.0, value));
        }

        [Category("Display")]
        [DisplayName("Compact Mode")]
        [Description("Use a more compact display format.")]
        public bool CompactMode
        {
            get => _options.CompactMode;
            set => _options.CompactMode = value;
        }

        // Gutter Settings
        [Category("Gutter")]
        [DisplayName("Gutter Width")]
        [Description("Width of the blame gutter in pixels.")]
        public int GutterWidth
        {
            get => _options.GutterWidth;
            set => _options.GutterWidth = Math.Max(20, Math.Min(100, value));
        }

        [Category("Gutter")]
        [DisplayName("Show Age Bars")]
        [Description("Show age indicator bars in the gutter.")]
        public bool ShowAgeBars
        {
            get => _options.ShowAgeBars;
            set => _options.ShowAgeBars = value;
        }

        // Status Bar Settings
        [Category("Status Bar")]
        [DisplayName("Enable Status Bar Blame")]
        [Description("Show blame information in the status bar for the current line.")]
        public bool EnableStatusBarBlame
        {
            get => _options.EnableStatusBarBlame;
            set => _options.EnableStatusBarBlame = value;
        }

        [Category("Status Bar")]
        [DisplayName("Format")]
        [Description("Format template using placeholders: {author}, {date}, {message}, {sha}")]
        public string StatusBarFormat
        {
            get => _options.StatusBarFormat;
            set => _options.StatusBarFormat = value;
        }

        [Category("Status Bar")]
        [DisplayName("Use Relative Dates")]
        [Description("Show relative dates (e.g., '2 days ago') instead of absolute dates.")]
        public bool StatusBarRelativeDate
        {
            get => _options.StatusBarRelativeDate;
            set => _options.StatusBarRelativeDate = value;
        }

        [Category("Status Bar")]
        [DisplayName("Maximum Length")]
        [Description("Maximum characters to display before truncating. Set to 0 for no limit.")]
        public int StatusBarMaxLength
        {
            get => _options.StatusBarMaxLength;
            set => _options.StatusBarMaxLength = Math.Max(0, value);
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            _options.Save();
        }

        /// <summary>
        /// Loads the settings.
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();
            _options = GeneralOptions.Instance;
        }
    }
}
