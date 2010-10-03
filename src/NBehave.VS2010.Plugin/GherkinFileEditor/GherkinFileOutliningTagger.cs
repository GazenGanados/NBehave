﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace NBehave.VS2010.Plugin.GherkinFileEditor
{
    #region Provider definition
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("gherkin")]
    internal sealed class OutliningTaggerProvider : EditorExtensionProviderBase, ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            GherkinFileEditorParser parser = GetParser(buffer);

            return (ITagger<T>)buffer.Properties.GetOrCreateSingletonProperty(() =>
                new GherkinFileOutliningTagger(parser));
        }
    }
    #endregion

    internal class GherkinFileOutliningTagger : ITagger<IOutliningRegionTag>
    {
        private readonly GherkinFileEditorParser parser;

        public GherkinFileOutliningTagger(GherkinFileEditorParser parser)
        {
            this.parser = parser;

            parser.TagsChanged += (sender, args) =>
            {
                if (TagsChanged != null)
                    TagsChanged(this, args);
            };
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return parser.GetTags(spans);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
