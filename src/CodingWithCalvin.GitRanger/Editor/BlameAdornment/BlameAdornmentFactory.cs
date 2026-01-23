using System.ComponentModel.Composition;
using CodingWithCalvin.GitRanger.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CodingWithCalvin.GitRanger.Editor.BlameAdornment;

/// <summary>
/// Factory for creating blame adornment instances for text views.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class BlameAdornmentFactory : IWpfTextViewCreationListener
{
    /// <summary>
    /// The adornment layer definition for blame annotations.
    /// </summary>
    [Export(typeof(AdornmentLayerDefinition))]
    [Name(BlameAdornment.LayerName)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? EditorAdornmentLayer = null;

    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly IGitService _gitService;
    private readonly IBlameService _blameService;
    private readonly IThemeService _themeService;
    private readonly IOutputPaneService _outputPane;

    [ImportingConstructor]
    public BlameAdornmentFactory(
        ITextDocumentFactoryService textDocumentFactoryService,
        IGitService gitService,
        IBlameService blameService,
        IThemeService themeService,
        IOutputPaneService outputPane)
    {
        _textDocumentFactoryService = textDocumentFactoryService;
        _gitService = gitService;
        _blameService = blameService;
        _themeService = themeService;
        _outputPane = outputPane;

        _outputPane.WriteInfo("BlameAdornmentFactory created");
    }

    /// <summary>
    /// Called when a text view is created.
    /// </summary>
    /// <param name="textView">The text view.</param>
    public void TextViewCreated(IWpfTextView textView)
    {
        _outputPane.WriteVerbose("BlameAdornmentFactory.TextViewCreated called");

        textView.Properties.GetOrCreateSingletonProperty(() =>
        {
            return new BlameAdornment(textView, _textDocumentFactoryService, _gitService, _blameService, _themeService, _outputPane);
        });
    }
}
