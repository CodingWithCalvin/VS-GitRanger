using System.ComponentModel.Composition;
using CodingWithCalvin.GitRanger.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CodingWithCalvin.GitRanger.Editor.GutterMargin;

/// <summary>
/// Factory for creating blame margin instances.
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(BlameMargin.MarginName)]
[Order(Before = PredefinedMarginNames.LineNumber)]
[MarginContainer(PredefinedMarginNames.LeftSelection)]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class BlameMarginFactory : IWpfTextViewMarginProvider
{
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly IGitService _gitService;
    private readonly IBlameService _blameService;
    private readonly IThemeService _themeService;
    private readonly IOutputPaneService _outputPane;

    [ImportingConstructor]
    public BlameMarginFactory(
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

        _outputPane.WriteInfo("BlameMarginFactory created");
    }

    /// <summary>
    /// Creates the margin for the given text view.
    /// </summary>
    public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        _outputPane.WriteVerbose("BlameMarginFactory.CreateMargin called");

        return new BlameMargin(
            wpfTextViewHost.TextView,
            _textDocumentFactoryService,
            _gitService,
            _blameService,
            _themeService,
            _outputPane);
    }
}
