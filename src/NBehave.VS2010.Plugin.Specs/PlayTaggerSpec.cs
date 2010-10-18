﻿using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using NBehave.VS2010.Plugin.GherkinFileEditor;
using NBehave.VS2010.Plugin.GherkinFileEditor.Glyphs;
using NUnit.Framework;
using Rhino.Mocks;

namespace NBehave.VS2010.Plugin.Specs
{
    [TestFixture]
    public class PlayTaggerSpec
    {
        private ITextBuffer _buffer;
        private ITagger<PlayTag> _playTagger;

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

            _buffer = MockRepository.GenerateMock<ITextBuffer>();
            _buffer.Stub(textBuffer => textBuffer.Properties).Return(new PropertyCollection());

            var gherkinFile = new StreamReader(gherkinFileLocation).ReadToEnd();
            _buffer.Stub(buffer => buffer.CurrentSnapshot).Return(new MockTextSnapshot(gherkinFile));

            var gherkinFileEditorParser = new GherkinFileEditorParser();
            gherkinFileEditorParser.InitialiseWithBuffer(_buffer);
            _buffer.Properties.AddProperty(typeof(GherkinFileEditorParser), gherkinFileEditorParser);

            _playTagger = new PlayTagger(_buffer, null);

            gherkinFileEditorParser.FirstParse();
        }

        [Test]
        public void ShouldClassifyScenarioWithExamples()
        {
            TestInitialise("Features/gherkin.feature");

            var tag = _playTagger.GetTags(null).Where(span => span.Span.GetText().StartsWith("  Scenario: SC1")).First();

            Assert.That(tag.Span.GetText(), Is.EqualTo("  Scenario: SC1"                       + Environment.NewLine +
                                                       "    Given numbers [left] and [right]"  + Environment.NewLine +
                                                       "    When I add the numbers"            + Environment.NewLine +
                                                       "    Then the sum is [sum]"             + Environment.NewLine +
                                                       "    "                                  + Environment.NewLine +
                                                       "    Examples:"                         + Environment.NewLine +
                                                       "    |left|right|sum|"                  + Environment.NewLine +
                                                       "    |1   | 2   |3  |"                  + Environment.NewLine +
                                                       "    |3   | 4   |7  |"));
        }

        [Test]
        public void ShouldClassifyScenarioWithInlineTable()
        {
            TestInitialise("Features/gherkin.feature");

            var tag = _playTagger.GetTags(null).Where(span => span.Span.GetText().StartsWith("  Scenario: inline table")).First();

            Assert.That(tag.Span.GetText(), Is.EqualTo("  Scenario: inline table"                 + Environment.NewLine +
                                                       "    Given the following people exists:"   + Environment.NewLine +
                                                       "      |Name          |Country|"           + Environment.NewLine +
                                                       "      |Morgan Persson|Sweden |"           + Environment.NewLine +
                                                       "      |Jimmy Nilsson |Sweden |"           + Environment.NewLine +
                                                       "      |Jimmy bogard  |USA    |"           + Environment.NewLine +
                                                       "    When I search for people in sweden"   + Environment.NewLine +
                                                       "    Then I should get:"                   + Environment.NewLine +
                                                       "      |Name          |"                   + Environment.NewLine +
                                                       "      |Morgan Persson|"                   + Environment.NewLine +
                                                       "      |Jimmy Nilsson |"));
        }

        [Test]
        public void ShouldClassifyScenario()
        {
            TestInitialise("Features/gherkin.feature");

            var tag = _playTagger.GetTags(null).Where(span => span.Span.GetText().StartsWith("  Scenario: SC2")).First();

            Assert.That(tag.Span.GetText(), Is.EqualTo("  Scenario: SC2"                           + Environment.NewLine +
                                                       "    Given something"                       + Environment.NewLine +
                                                       "    When some event occurs"                + Environment.NewLine +
                                                       "    Then there is some outcome"            + Environment.NewLine));
        }

        [Test]
        public void ShouldClassifyScenarioWithMultipleInlineTables()
        {
            TestInitialise("../../../NBehave.Examples/Tables/Table.feature");

            var tag = _playTagger.GetTags(null).Where(span => span.Span.GetText().StartsWith("Scenario: a table")).First();

            Assert.That(tag.Span.GetText(), Is.EqualTo("Scenario: a table"                        + Environment.NewLine +
                                                       ""                                         + Environment.NewLine +
                                                       "Given a list of people:"                  + Environment.NewLine +
                                                       "|name          |country|"                 + Environment.NewLine +
                                                       "|Morgan Persson|Sweden |"                 + Environment.NewLine +
                                                       "|Jimmy Nilsson |Sweden |"                 + Environment.NewLine +
                                                       "|Jimmy Bogard  |USA    |"                 + Environment.NewLine +
                                                       ""                                         + Environment.NewLine +
                                                       "When I search for people from Sweden"     + Environment.NewLine +
                                                       ""                                         + Environment.NewLine +
                                                       "Then I should find:"                      + Environment.NewLine +
                                                       "|Name          |Country|"                 + Environment.NewLine +
                                                       "|Morgan Persson|Sweden |"                 + Environment.NewLine +
                                                       "|Jimmy Nilsson |Sweden |"                 ));
        }
    }
}
