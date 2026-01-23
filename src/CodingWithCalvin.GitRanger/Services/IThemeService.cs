using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CodingWithCalvin.GitRanger.Services
{
    /// <summary>
    /// Service interface for Visual Studio theme detection and color adaptation.
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Gets whether the current theme is a dark theme.
        /// </summary>
        bool IsDarkTheme { get; }

        /// <summary>
        /// Gets the current editor background color.
        /// </summary>
        Color EditorBackground { get; }

        /// <summary>
        /// Gets the current editor foreground color.
        /// </summary>
        Color EditorForeground { get; }

        /// <summary>
        /// Fired when the VS theme changes.
        /// </summary>
        event EventHandler? ThemeChanged;

        /// <summary>
        /// Initializes the theme service and subscribes to theme changes.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets a consistent color for an author (email-based).
        /// </summary>
        /// <param name="authorEmail">The author's email address.</param>
        /// <returns>A vibrant color for the author.</returns>
        Color GetAuthorColor(string authorEmail);

        /// <summary>
        /// Gets a heat map color based on commit age.
        /// Recent commits are green, old commits are red.
        /// </summary>
        /// <param name="ageDays">Age of the commit in days.</param>
        /// <param name="maxAgeDays">Maximum age to consider (default 365 days).</param>
        /// <returns>A color from green (new) to red (old).</returns>
        Color GetAgeHeatMapColor(int ageDays, int maxAgeDays = 365);

        /// <summary>
        /// Gets a subtle background color for blame annotations.
        /// </summary>
        Color GetBlameBackgroundColor();

        /// <summary>
        /// Gets the text color for blame annotations.
        /// </summary>
        Color GetBlameTextColor();

        /// <summary>
        /// Gets a color adjusted for the current theme.
        /// Makes colors slightly dimmer in dark theme for better visibility.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <returns>The adjusted color.</returns>
        Color AdjustForTheme(Color color);

        /// <summary>
        /// Clears the author color cache.
        /// </summary>
        void ClearColorCache();
    }
}
