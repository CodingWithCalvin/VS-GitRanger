using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Media;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service for Visual Studio theme detection and color adaptation.
    /// </summary>
    [Export(typeof(IThemeService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ThemeService : IThemeService
    {
        private static readonly Color[] VibrantAuthorColors = new[]
        {
            Color.FromRgb(0x00, 0x96, 0x88), // Teal
            Color.FromRgb(0xE9, 0x1E, 0x63), // Pink
            Color.FromRgb(0x9C, 0x27, 0xB0), // Purple
            Color.FromRgb(0x67, 0x3A, 0xB7), // Deep Purple
            Color.FromRgb(0x3F, 0x51, 0xB5), // Indigo
            Color.FromRgb(0x21, 0x96, 0xF3), // Blue
            Color.FromRgb(0x00, 0xBC, 0xD4), // Cyan
            Color.FromRgb(0x4C, 0xAF, 0x50), // Green
            Color.FromRgb(0x8B, 0xC3, 0x4A), // Light Green
            Color.FromRgb(0xFF, 0x98, 0x00), // Orange
            Color.FromRgb(0xFF, 0x57, 0x22), // Deep Orange
            Color.FromRgb(0x79, 0x55, 0x48), // Brown
        };

        private readonly Dictionary<string, Color> _authorColorCache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        private int _nextColorIndex = 0;

        /// <summary>
        /// Gets whether the current theme is a dark theme.
        /// </summary>
        public bool IsDarkTheme { get; private set; }

        /// <summary>
        /// Gets the current editor background color.
        /// </summary>
        public Color EditorBackground { get; private set; }

        /// <summary>
        /// Gets the current editor foreground color.
        /// </summary>
        public Color EditorForeground { get; private set; }

        /// <summary>
        /// Fired when the VS theme changes.
        /// </summary>
        public event EventHandler? ThemeChanged;

        /// <summary>
        /// Initializes the theme service and subscribes to theme changes.
        /// </summary>
        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Detect current theme
            UpdateThemeColors();

            // Subscribe to theme changes
            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateThemeColors();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateThemeColors()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the environment background color to determine if dark/light theme
            var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

            // Calculate luminance to determine if dark theme
            var luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;
            IsDarkTheme = luminance < 0.5;

            EditorBackground = Color.FromRgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);

            var foregroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
            EditorForeground = Color.FromRgb(foregroundColor.R, foregroundColor.G, foregroundColor.B);
        }

        /// <summary>
        /// Gets a consistent color for an author (email-based).
        /// </summary>
        /// <param name="authorEmail">The author's email address.</param>
        /// <returns>A vibrant color for the author.</returns>
        public Color GetAuthorColor(string authorEmail)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return VibrantAuthorColors[0];

            if (_authorColorCache.TryGetValue(authorEmail, out var cachedColor))
                return cachedColor;

            // Assign next color in rotation
            var color = VibrantAuthorColors[_nextColorIndex % VibrantAuthorColors.Length];
            _nextColorIndex++;

            _authorColorCache[authorEmail] = color;
            return color;
        }

        /// <summary>
        /// Gets a heat map color based on commit age.
        /// Recent commits are green, old commits are red.
        /// </summary>
        /// <param name="ageDays">Age of the commit in days.</param>
        /// <param name="maxAgeDays">Maximum age to consider (default 365 days).</param>
        /// <returns>A color from green (new) to red (old).</returns>
        public Color GetAgeHeatMapColor(int ageDays, int maxAgeDays = 365)
        {
            // Clamp age to range
            var normalizedAge = Math.Min(1.0, Math.Max(0.0, (double)ageDays / maxAgeDays));

            // Interpolate from green (recent) to red (old)
            // Green: RGB(76, 175, 80) - #4CAF50
            // Yellow: RGB(255, 235, 59) - #FFEB3B (middle)
            // Red: RGB(244, 67, 54) - #F44336

            byte r, g, b;

            if (normalizedAge < 0.5)
            {
                // Green to Yellow
                var t = normalizedAge * 2;
                r = (byte)(76 + (255 - 76) * t);
                g = (byte)(175 + (235 - 175) * t);
                b = (byte)(80 + (59 - 80) * t);
            }
            else
            {
                // Yellow to Red
                var t = (normalizedAge - 0.5) * 2;
                r = (byte)(255 + (244 - 255) * t);
                g = (byte)(235 + (67 - 235) * t);
                b = (byte)(59 + (54 - 59) * t);
            }

            return Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Gets a subtle background color for blame annotations.
        /// </summary>
        public Color GetBlameBackgroundColor()
        {
            if (IsDarkTheme)
            {
                // Subtle dark background
                return Color.FromArgb(40, 255, 255, 255);
            }
            else
            {
                // Subtle light background
                return Color.FromArgb(30, 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets the text color for blame annotations.
        /// </summary>
        public Color GetBlameTextColor()
        {
            if (IsDarkTheme)
            {
                return Color.FromRgb(180, 180, 180);
            }
            else
            {
                return Color.FromRgb(100, 100, 100);
            }
        }

        /// <summary>
        /// Gets a color adjusted for the current theme.
        /// Makes colors slightly dimmer in dark theme for better visibility.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <returns>The adjusted color.</returns>
        public Color AdjustForTheme(Color color)
        {
            if (!IsDarkTheme)
            {
                // Darken colors slightly for light theme
                return Color.FromRgb(
                    (byte)(color.R * 0.8),
                    (byte)(color.G * 0.8),
                    (byte)(color.B * 0.8));
            }

            return color;
        }

        /// <summary>
        /// Clears the author color cache.
        /// </summary>
        public void ClearColorCache()
        {
            _authorColorCache.Clear();
            _nextColorIndex = 0;
        }
    }
}
