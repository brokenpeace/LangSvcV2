﻿namespace Tvl.VisualStudio.Language.Antlr3
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Utilities;
    using Tvl.VisualStudio.Text.Classification;

    [Export(typeof(IClassifierProvider))]
    [ContentType(AntlrConstants.AntlrContentType)]
    public sealed class AntlrClassifierProvider : LanguageClassifierProvider<AntlrLanguagePackage>
    {
        protected override IClassifier GetClassifierImpl(ITextBuffer textBuffer)
        {
            return new AntlrClassifier(textBuffer, StandardClassificationService, ClassificationTypeRegistryService);
        }
    }
}
