using System.Threading.Tasks;

namespace CodingWithCalvin.GitRanger.Services;

/// <summary>
/// Service interface for writing to the Git Ranger output pane.
/// </summary>
public interface IOutputPaneService
{
    /// <summary>
    /// Writes an error message to the output pane.
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// Writes a formatted error message to the output pane.
    /// </summary>
    void WriteError(string format, params object[] args);

    /// <summary>
    /// Writes an info message to the output pane.
    /// </summary>
    void WriteInfo(string message);

    /// <summary>
    /// Writes a formatted info message to the output pane.
    /// </summary>
    void WriteInfo(string format, params object[] args);

    /// <summary>
    /// Writes a verbose/debug message to the output pane.
    /// </summary>
    void WriteVerbose(string message);

    /// <summary>
    /// Writes a formatted verbose/debug message to the output pane.
    /// </summary>
    void WriteVerbose(string format, params object[] args);

    /// <summary>
    /// Activates and shows the output pane.
    /// </summary>
    Task ActivateAsync();
}
