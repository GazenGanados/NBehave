﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace NBehave.VS2010.Plugin.GherkinFileEditor
{

    [Export(typeof(IClassifierProvider))]
    [ContentType("gherkin")]
    internal class GherkinFileClassifierProvider : IClassifierProvider
    {
        [Import]
        private GherkinFileClassifier GherkinFileClassifier { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            GherkinFileClassifier.InitialiseWithBuffer(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => GherkinFileClassifier);
        }

    }

    [Export(typeof(GherkinFileClassifier))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class GherkinFileClassifier : IClassifier, IDisposable
    {
        private GherkinFileEditorParser _parser;
        private List<ClassificationSpan> _spans;
        private IDisposable _disposable;
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public GherkinFileClassifier()
        {
            _spans = new List<ClassificationSpan>();
        }

        [Import]
        internal GherkinFileEditorParserFactory GherkinFileEditorParserFactory { get; set; }

        [Import]
        private GherkinFileEditorClassifications ClassificationRegistry { get; set; }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return _spans;
        }

        public void InitialiseWithBuffer(ITextBuffer buffer)
        {
            _parser = GherkinFileEditorParserFactory.CreateParser(buffer); ;

            IObservable<ClassificationSpan> observable = _parser.ParserEvents
                .Where(@event => @event.EventType == ParserEventType.Feature)
                .Select(parserEvent => new ClassificationSpan(new SnapshotSpan(buffer.CurrentSnapshot, new Span(0, 10)),
                                                              ClassificationRegistry.FeatureTitle));

            _disposable = observable
                            .Do(
                                span =>
                                    {
                                        if (ClassificationChanged != null)
                                        {
                                            ClassificationChanged(this,
                                                                  new ClassificationChangedEventArgs(
                                                                      new SnapshotSpan(buffer.CurrentSnapshot,
                                                                                       new Span(0, 10))));
                                        }
                                    })
                            .Subscribe(_spans.Add);

            _parser.FirstParse();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
