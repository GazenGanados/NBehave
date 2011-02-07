﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NBehave.Narrator.Framework.EventListeners;
using NBehave.Narrator.Framework.EventListeners.Xml;
using NBehave.Narrator.Framework.Specifications.Features;
using NUnit.Framework;
using Rhino.Mocks;

namespace NBehave.Narrator.Framework.Specifications
{
    public static class LocalConfigurationExtensions
    {
        public static TextRunner Build(this NBehaveConfiguration configuration)
        {
            return (TextRunner) NBehaveConfigurationExtensions.Build(configuration);
        }
    }
    [TestFixture]
    public class TextRunnerSpec
    {
        protected NBehaveConfiguration CreateRunnerWithBasicConfiguration()
        {
            var writer = new StreamWriter(new MemoryStream());
            var listener = new TextWriterEventListener(writer);
            
            var config = NBehaveConfiguration
                .New
                .SetAssemblies(new[] { "TestPlainTextAssembly.dll" })
                .SetEventListener(listener)
                .SetScenarioFiles(new[] { TestFeatures.ScenariosWithoutFeature });

            return config;
        }

        [TestFixture]
        public class WhenRunningPlainTextScenarios : TextRunnerSpec
        {
            private FeatureResults _result;

            [Test]
            public void ShouldFindGivenActionStepInAssembly()
            {
                var runner = this.CreateRunnerWithBasicConfiguration().Build();
                runner.Run();
                Assert.That(runner.ActionCatalog.ActionExists("my name is Axel"), Is.True);
            }

            [Test]
            public void ShouldFindWhenActionStepInAssembly()
            {
                var runner = this.CreateRunnerWithBasicConfiguration().Build();
                runner.Run();
                Assert.That(runner.ActionCatalog.ActionExists("I'm greeted"), Is.True);
            }

            [Test]
            public void ShouldFindThenActionStepInAssembly()
            {
                var runner = this.CreateRunnerWithBasicConfiguration().Build();
                runner.Run();
                Assert.That(runner.ActionCatalog.ActionExists("I should be greeted with “Hello, Axel!”"), Is.True);
            }
            
            [Test]
            public void ShouldRunScenariosInTextFile()
            {
                _result = this.CreateRunnerWithBasicConfiguration().Run();

                Assert.That(_result.NumberOfPassingScenarios, Is.EqualTo(2));
            }
            
            [Test]
            public void ShouldGetResultOfRunningScenariosInTextFile()
            {
                _result = this.CreateRunnerWithBasicConfiguration().Run();

                Assert.That(_result.NumberOfThemes, Is.EqualTo(0));
                Assert.That(_result.NumberOfStories, Is.EqualTo(1));
                Assert.That(_result.NumberOfScenariosFound, Is.EqualTo(2));
                Assert.That(_result.NumberOfPassingScenarios, Is.EqualTo(2));
            }

            [Test]
            public void ShouldGetCorrectErrormessageFromFailedScenario()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { TestFeatures.FeatureWithFailingStep }).Run();

                Assert.That(_result.NumberOfFailingScenarios, Is.EqualTo(1));
                Assert.That(_result.ScenarioResults[0].Message.StartsWith("Should.Core.Exceptions.EqualException"), Is.True);
            }

            [Test]
            public void ShouldMarkFailingStepAsFailedInOutput()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                this.CreateRunnerWithBasicConfiguration().SetEventListener(listener).SetScenarioFiles(
                    new[] { TestFeatures.FeatureWithFailingStep }).Run();

                StringAssert.Contains("Then I should be greeted with “Hello, Scott!” - FAILED", writer.ToString());
            }

            [Test]
            public void ShouldExecuteMoreThanOneScenarioInTextFile()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { TestFeatures.FeatureWithManyScenarios }).Run();

                Assert.That(_result.NumberOfScenariosFound, Is.EqualTo(2));
                Assert.That(_result.NumberOfPassingScenarios, Is.EqualTo(2));
            }

            [Test]
            public void ShouldRunScenarioInTextFileWithScenarioTitle()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { TestFeatures.ScenariosWithoutFeature }).Run();

                Assert.That(_result.ScenarioResults[0].ScenarioTitle, Is.EqualTo("greeting Morgan"));
                Assert.That(_result.ScenarioResults[0].Result, Is.TypeOf(typeof(Passed)));
            }

            [Test]
            public void ShouldRunTextScenarioWhithNewlinesInGiven()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { TestFeatures.FeatureWithNewLineInGivenClause }).Run();

                Assert.That(_result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Test]
            public void ShouldSetScenarioPendingIfActionGivenInTokenStringDoesntExist()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { TestFeatures.ScenarioWithNoActionSteps }).Run();

                Assert.That(_result.NumberOfPendingScenarios, Is.EqualTo(1));
            }

            [Test]
            public void ShouldListAllPendingActionSteps()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { TestFeatures.ScenarioWithNoActionSteps }).Run();

                StringAssert.Contains("No matching Action found for \"Given something that has no ActionStep\"", _result.ScenarioResults[0].Message);
                StringAssert.Contains("No matching Action found for \"And something else that has no ActionStep\"", _result.ScenarioResults[0].Message);
            }

            [Test]
            public void ShouldUseWildcardAndRunAllScenariosInAllMatchingTextFiles()
            {
                _result = this.CreateRunnerWithBasicConfiguration().SetScenarioFiles(
                    new[] { @"Features\\Feature*.feature" }).Run();

                Assert.That(_result.NumberOfPassingScenarios, Is.EqualTo(4));
            }
        }

        [TestFixture]
        public class WhenRunningWithXmlListener : TextRunnerSpec
        {
            private XmlDocument _xmlOut;
            private FeatureResults _result;

            [SetUp]
            public void SetUp()
            {
                var writer = new XmlTextWriter(new MemoryStream(), Encoding.UTF8);
                var listener = new XmlOutputEventListener(writer);

                _result = this.CreateRunnerWithBasicConfiguration()
                    .SetEventListener(listener)
                    .SetScenarioFiles(new[] { TestFeatures.FeatureWithFailingStep })
                    .Run();
                
                _xmlOut = new XmlDocument();
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                _xmlOut.Load(writer.BaseStream);
            }

            [Test]
            public void ShouldFindOneFailedActionStep()
            {
                var storyNodes = _xmlOut.SelectNodes("//actionStep[@outcome='failed']");
                Assert.That(storyNodes.Count, Is.EqualTo(1));
            }

            [Test]
            public void ShouldFindTwoPassedActionStep()
            {
                var storyNodes = _xmlOut.SelectNodes("//actionStep[@outcome='passed']");
                Assert.That(storyNodes.Count, Is.EqualTo(2));
            }
        }

        [TestFixture]
        public class WhenRunningPlainTextScenariosWithXmlListener : TextRunnerSpec
        {
            private const string StoryTitle = "Greeting system";

            private XmlDocument _xmlOut;
            private FeatureResults _result;

            [SetUp]
            public void SetUp()
            {
                var writer = new XmlTextWriter(new MemoryStream(), Encoding.UTF8);
                var listener = new XmlOutputEventListener(writer);
                _result = this.CreateRunnerWithBasicConfiguration()
                    .SetEventListener(listener)
                    .SetScenarioFiles(new[] { TestFeatures.FeatureWithManyScenarios })
                    .Run();

                _xmlOut = new XmlDocument();
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                _xmlOut.Load(writer.BaseStream);
            }

            [Test]
            public void ShouldFindOneStory()
            {
                var storyNodes = _xmlOut.SelectNodes("//story");
                Assert.That(storyNodes.Count, Is.EqualTo(1));
            }

            [Test]
            public void ShouldSetTitleOfStory()
            {
                var storyNodes = _xmlOut.SelectSingleNode("//story").Attributes["name"];

                Assert.That(storyNodes.Value, Is.EqualTo(StoryTitle));
            }

            [Test]
            public void ShouldRunTwoScenarios()
            {
                var scenarioNodes = _xmlOut.SelectNodes("//scenario");

                Assert.That(scenarioNodes.Count, Is.EqualTo(2));
            }
        }

        [TestFixture]
        public class WhenRunningPlainTextScenariosWithStory : TextRunnerSpec
        {
            private FeatureResults _result;
            private StringWriter _messages;

            [SetUp]
            public void SetUp()
            {
                _messages = new StringWriter();
                var listener = new TextWriterEventListener(_messages);

                _result = this.CreateRunnerWithBasicConfiguration()
                    .SetEventListener(listener)
                    .SetScenarioFiles(new[] { TestFeatures.FeatureNamedStory })
                    .Run();
            }

            [Test]
            public void ShouldSetStoryTitleOnResult()
            {
                Assert.That(_result.ScenarioResults[0].FeatureTitle, Is.EqualTo("Greeting system"));
            }

            [Test]
            public void ShouldSetNarrativeOnResult()
            {
                var messages = _messages.ToString();
                StringAssert.Contains("As a project member", messages);
                StringAssert.Contains("I want", messages);
                StringAssert.Contains("So that", messages);
            }

            [Test]
            public void ShouldSetScenarioTitleOnResult()
            {
                Assert.That(_result.ScenarioResults[0].ScenarioTitle, Is.EqualTo("Greeting someone"));
            }
        }

        [TestFixture]
        public class WhenRunningPlainTextScenariosWithStoryEventsRaised : TextRunnerSpec
        {
            private IEventListener _listener;
            private FeatureResults _result;

            [TestFixtureSetUp]
            public void EstablishContext()
            {
                _listener = MockRepository.GenerateMock<IEventListener>();
                _result = this.CreateRunnerWithBasicConfiguration()
                   .SetEventListener(_listener)
                   .SetScenarioFiles(new[] { TestFeatures.FeatureNamedStory })
                   .Run();
            }

            [Test]
            public void ShouldGetStoryCreatedEventWithTitle()
            {
                _listener.AssertWasCalled(l => l.FeatureCreated("Greeting system"));
            }

            [Test]
            public void ShouldGetStoryNarrative()
            {
                var args = _listener.GetArgumentsForCallsMadeOn(l => l.FeatureNarrative(null), opt => opt.IgnoreArguments());
                var arg = args[0][0] as string;
                StringAssert.Contains("As a", arg);
                StringAssert.Contains("I want", arg);
                StringAssert.Contains("So that", arg);
            }

            [Test]
            public void ShouldGetScenarioCreatedEventWithTitle()
            {
                _listener.AssertWasCalled(l => l.ScenarioCreated("Greeting someone"));
            }
        }
    
        [TestFixture, ActionSteps]
        public class WhenRunningPlainTextScenarioInSwedish : TextRunnerSpec
        {
            private readonly Stack<int> _numbers = new Stack<int>();
            private int _calcResult;
            private static bool _givenWasCalled;
            private static bool _whenWasCalled;
            private static bool _thenWasCalled;
            private FeatureResults _featureResults;

            [Given(@"att jag knappat in $number")]
            public void GivenNumber(int number)
            {
                _numbers.Push(number);
                _givenWasCalled = true;
            }

            [When("jag summerar")]
            public void Sum()
            {
                _calcResult = _numbers.Pop() + _numbers.Pop();
                _whenWasCalled = true;
            }

            [Then("ska resultatet vara $sum")]
            public void Result(int sum)
            {
                Assert.AreEqual(sum, _calcResult);
                _thenWasCalled = true;
            }

            [SetUp]
            public void SetUp()
            {
                _featureResults = this.CreateRunnerWithBasicConfiguration()
                   .SetAssemblies(new[]{ "NBehave.Narrator.Framework.Specifications.dll" })
                   .SetScenarioFiles(new[] { TestFeatures.FeatureInSwedish })
                   .Run();
            }

            [Test]
            public void ShouldRunTextScenarioInStream()
            {
                Assert.That(_featureResults.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Test]
            public void GivenShouldBeCalled()
            {
                Assert.That(_givenWasCalled, Is.True);
            }

            [Test]
            public void WhenShouldBeCalled()
            {
                Assert.That(_whenWasCalled, Is.True);
            }

            [Test]
            public void ThenShouldBeCalled()
            {
                Assert.That(_thenWasCalled, Is.True);
            }
        }
    }
}
