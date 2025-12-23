using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CodingWithCalvin.GitRanger.Editor.BlameAdornment
{
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

        [Import]
        internal ITextDocumentFactoryService? TextDocumentFactoryService { get; set; }

        /// <summary>
        /// Called when a text view is created.
        /// </summary>
        /// <param name="textView">The text view.</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            // Create the adornment for this view
            textView.Properties.GetOrCreateSingletonProperty(() =>
                new BlameAdornment(textView, TextDocumentFactoryService));
        }
    }
}
