using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace NBehave.Narrator.Framework.Specifications
{
    [TestFixture]
    public class TextToTokenStringsParserSpec
    {
        private TextToTokenStringsParser _tokenStringsParser;
        private ActionStepAlias _actionStepAlias;

        [SetUp]
        public void Setup()
        {
            _actionStepAlias = new ActionStepAlias();
            _tokenStringsParser = new TextToTokenStringsParser(_actionStepAlias);
        }

        [Test]
        public void ShouldFindThreeTokens()
        {
            const string scenario = "Given my name is Morgan\n" +
                                    "When I'm greeted\n" +
                                    "Then I should be greeted with �Hello, Morgan!�";

            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings.Count, Is.EqualTo(3));
        }

        [Test]
        public void ShouldParseTokenFromText()
        {
            const string scenario = "Given my name is Morgan\n" +
                                    "When I'm greeted\n" +
                                    "Then I should be greeted with �Hello, Morgan!�";

            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[0], Is.EqualTo("Given my name is Morgan"));
        }

        [Test]
        public void ShouldParseFirstTokenFromTextWhenTokenHasNewLine()
        {
            string scenario = "Given my" + Environment.NewLine +
                              "name is Morgan" + Environment.NewLine +
                              "When I'm greeted" + Environment.NewLine +
                              "Then I should be greeted with �Hello, Morgan!�";
            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[0], Is.EqualTo("Given my" + Environment.NewLine + "name is Morgan"));
        }

        [Test]
        public void ShouldParseLastTokenFromTextWhenTokenHasNewLine()
        {
            const string scenario = "Given my\n" +
                                    "name is Morgan\n" +
                                    "When I'm greeted\n" +
                                    "Then I should be greeted with �Hello, Morgan!�";
            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[2],
                        Is.EqualTo("Then I should be greeted with �Hello, Morgan!�"));
        }

        [Test]
        public void ShouldParseTokenFromTextUsingAlias()
        {
            const string scenario = "Given my name is Morgan\n" +
                                    "When I'm greeted\n" +
                                    "And I should be greeted with �Hello, Morgan!�";

            _actionStepAlias.AddDefaultAlias(new[] { "And" }, "Given");
            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[2], Is.EqualTo("And I should be greeted with �Hello, Morgan!�"));
        }

        [Test]
        public void Should_parse_TokenString_on_multiple_lines()
        {
            const string scenario = "Given my name is\nMorgan\nPersson\n" +
                                 "When I'm greeted\n" +
                                 "Then I should be greeted with �Hello, Morgan Persson!�";

            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings.Count, Is.EqualTo(3));
            Assert.That(_tokenStringsParser.TokenStrings[0], Is.EqualTo("Given my name is" + Environment.NewLine + "Morgan" + Environment.NewLine + "Persson"));
        }

        [Test]
        public void Should_parse_story()
        {
            const string scenario = "Story: title";
            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[0], Is.EqualTo("Story: title"));
        }

        [Test]
        public void Should_parse_narrative_part_As_a()
        {
            const string scenario = "Story: title\nAs a developer\nI want full narrative support\nSo that I can write complete stories in a text file";
            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[1], Is.EqualTo("As a developer"));
        }

        [Test]
        public void Keyword_should_be_first_word_on_a_line()
        {
            const string scenario = "Story: story title\nScenario: foobar";
            _tokenStringsParser.ParseScenario(scenario);

            Assert.That(_tokenStringsParser.TokenStrings[0], Is.EqualTo("Story: story title"));
        }
    }
}