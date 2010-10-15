﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using NBehave.VS2010.Plugin.GherkinFileEditor;
using NUnit.Framework;
using Rhino.Mocks;

namespace NBehave.VS2010.Plugin.Specs
{
    [TestFixture]
    public class GherkinFileClassifierSpec
    {
        private GherkinFileClassifier _gherkinFileClassifier;
        private ITextSnapshot _snapshot;
        private ITextBuffer _buffer;

        [SetUp]
        public void Setup()
        {
            TestInitialise("Features/gherkin.feature");
        }

        private void TestInitialise(string gherkinFileLocation)
        {
            var registry = MockRepository.GenerateMock<IClassificationTypeRegistryService>();
            registry.Stub(service => service.GetClassificationType(null))
                .IgnoreArguments()
                .WhenCalled(invocation =>
                {
                    invocation.ReturnValue = new MockClassificationType
                                {
                                    Classification = (string)invocation.Arguments.First()
                                };
                });

            _gherkinFileClassifier = new GherkinFileClassifier
                                         {
                                             ClassificationRegistry = new GherkinFileEditorClassifications
                                                                          {
                                                                              ClassificationRegistry = registry
                                                                          },
                                             GherkinFileEditorParserFactory = new GherkinFileEditorParserFactory
                                                                                  {
                                                                                      GherkinFileEditorParser = new GherkinFileEditorParser()
                                                                                  }
                                         };

            _buffer = MockRepository.GenerateMock<ITextBuffer>();
            _buffer.Stub(textBuffer => textBuffer.Properties).Return(new PropertyCollection());
            _snapshot = MockRepository.GenerateMock<ITextSnapshot>();

            var gherkinFile = new StreamReader(gherkinFileLocation).ReadToEnd();

            _buffer.Stub(buffer => buffer.CurrentSnapshot).Return(new MockTextSnapshot(gherkinFile));

            _gherkinFileClassifier.InitialiseWithBuffer(_buffer);
        }

        [Test]
        public void ShouldClassifyFeatureKeyword()
        {
            IEnumerable<string> spans = GetSpans("gherkin.keyword");

            CollectionAssert.AreEqual(spans, new[]{ "Feature", "Feature", "Feature"});
        }

        [Test]
        public void ShouldClassifyFeatureTitle()
        {
            IEnumerable<string> spans = GetSpans("gherkin.featuretitle");

            CollectionAssert.AreEqual(spans, new[] { " S1" + Environment.NewLine, " S2" + Environment.NewLine, " S3" + Environment.NewLine });
        }

        [Test]
        public void ShouldClassifyFeatureDescription()
        {
            IEnumerable<string> spans = GetSpans("gherkin.description").ToArray();

            CollectionAssert.AreEqual(spans, new[]
                                                 {
                                                    "  As a X1" + Environment.NewLine +
                                                    "  I want Y1" + Environment.NewLine +
                                                    "  So that Z1"  + Environment.NewLine,
                                                    
                                                    "  As a X2" + Environment.NewLine +
                                                    "  I want Y2" + Environment.NewLine +
                                                    "  So that Z2"  + Environment.NewLine,
                                                    
                                                    "  As a X3" + Environment.NewLine +
                                                    "  I want Y3" + Environment.NewLine +
                                                    "  So that Z3"  + Environment.NewLine,
                                                });
        }

        private IEnumerable<string> GetSpans(string gherkinKeyword)
        {
            return _gherkinFileClassifier
                .GetClassificationSpans(new SnapshotSpan())
                .Where(span => span.ClassificationType.IsOfType(gherkinKeyword))
                .Select(classificationSpan => classificationSpan.Span.GetText());
        }
    }
}
