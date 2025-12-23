using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CodingWithCalvin.GitRanger.Editor.GutterMargin
{
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
        [Import]
        internal ITextDocumentFactoryService? TextDocumentFactoryService { get; set; }

        /// <summary>
        /// Creates the margin for the given text view.
        /// </summary>
        /// <param name="wpfTextViewHost">The text view host.</param>
        /// <param name="marginContainer">The margin container.</param>
        /// <returns>The blame margin, or null if creation fails.</returns>
        public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return new BlameMargin(wpfTextViewHost.TextView, TextDocumentFactoryService);
        }
    }
}
