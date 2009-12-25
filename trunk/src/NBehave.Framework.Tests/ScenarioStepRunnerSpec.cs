using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Rhino.Mocks;
using Specification = NUnit.Framework.TestAttribute;


namespace NBehave.Narrator.Framework.Specifications
{
    [TestFixture]
    public class ScenarioStepRunnerSpec
    {
        private ScenarioStepRunner _runner;
        private ActionCatalog _actionCatalog;
        private StringStepRunner _stringStepRunner;

        private ScenarioWithSteps CreateScenarioWithSteps()
        {
            return CreateScenarioWithSteps(MockRepository.GenerateStub<IEventListener>());
        }

        private ScenarioWithSteps CreateScenarioWithSteps(IEventListener listener)
        {
            var scenario = new ScenarioWithSteps(_stringStepRunner, listener);
            return scenario;
        }

        [SetUp]
        public void SetUp()
        {
            _actionCatalog = new ActionCatalog();
            _stringStepRunner = new StringStepRunner(_actionCatalog);
            _runner = new ScenarioStepRunner();
        }

        public class When_running_a_scenario : ScenarioStepRunnerSpec
        {
            [Test]
            public void Should_have_result_for_each_step()
            {
                Action<string> action = name => Assert.AreEqual("Morgan", name);
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"my name is (?<name>\w+)"), action, action.Method, "Given"));

                var scenario = CreateScenarioWithSteps();
                scenario.AddStep("Given my name is Axel");
                scenario.AddStep("And my name is Morgan");
                var scenarioResult = _runner.RunScenarios(new[] { scenario }).First();

                Assert.AreEqual(2, scenarioResult.ActionStepResults.Count());
            }

            [Test]
            public void Should_have_different_result_for_each_step()
            {
                Action<string> action = name => Assert.AreEqual("Morgan", name);
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"my name is (?<name>\w+)"), action, action.Method, "Given"));

                var scenario = CreateScenarioWithSteps();
                scenario.AddStep("Given my name is Morgan");
                scenario.AddStep("Given my name is Axel");
                var scenarioResult = _runner.RunScenarios(new[] { scenario }).First();


                Assert.That(scenarioResult.ActionStepResults.First().Result, Is.TypeOf(typeof(Passed)));
                Assert.That(scenarioResult.ActionStepResults.Last().Result, Is.TypeOf(typeof(Failed)));
            }
        }


        public class When_Running_scenario_stream_with_multiple_scenarios : ScenarioStepRunnerSpec
        {
            [Test]
            public void Should_only_call_eventlistener_once_for_each_given()
            {
                Action<string> action = name => Assert.AreEqual("Morgan", name);
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"my name is (?<name>\w+)"), action, action.Method, "Given"));

                var evtListener = MockRepository.GenerateMock<IEventListener>();

                var fooScenario = CreateScenarioWithSteps(evtListener);
                fooScenario.Title = "foo";
                fooScenario.AddStep("Given foo");
                fooScenario.AddStep("When foo");
                fooScenario.AddStep("Then foo");

                var barScenario = CreateScenarioWithSteps(evtListener);
                barScenario.Title = "bar";
                barScenario.AddStep("Given bar");
                barScenario.AddStep("When bar");
                barScenario.AddStep("Then bar");

                _runner.EventListener = evtListener;
                var scenarioResults = _runner.RunScenarios(new List<ScenarioWithSteps> { fooScenario, barScenario });

                evtListener.AssertWasCalled(f => f.ScenarioMessageAdded("Given foo - PENDING"));
                evtListener.AssertWasCalled(f => f.ScenarioMessageAdded("Given bar - PENDING"));

                StringAssert.DoesNotContain("foo", scenarioResults.Skip(1).First().Message);
            }
        }


        [ActionSteps, TestFixture]
        public class When_running_many_scenarios_and_class_with_actionSteps_implements_notification_attributes : ScenarioStepRunnerSpec
        {
            private int _timesBeforeScenarioWasCalled;
            private int _timesBeforeStepWasCalled;
            private int _timesAfterStepWasCalled;
            private int _timesAfterScenarioWasCalled;

            [Given(@"something")]
            public void Given_something()
            { }

            [BeforeScenario]
            public void OnBeforeScenario()
            {
                _timesBeforeScenarioWasCalled++;
            }

            [BeforeStep]
            public void OnBeforeStep()
            {
                _timesBeforeStepWasCalled++;
            }

            [AfterStep]
            public void OnAfterStep()
            {
                _timesAfterStepWasCalled++;
            }

            [AfterScenario]
            public void OnAfterScenario()
            {
                _timesAfterScenarioWasCalled++;
            }

            [TestFixtureSetUp]
            public void Setup()
            {
                base.SetUp();
                Action action = Given_something;
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"something to count$"), action, action.Method, "Given", this));

                var firstScenario = CreateScenarioWithSteps();

                firstScenario.AddStep("Scenario: One");
                firstScenario.AddStep("Given something to count");
                var secondScenario = CreateScenarioWithSteps();
                secondScenario.AddStep("Scenario: Two");
                secondScenario.AddStep("Given something to count");
                secondScenario.AddStep("Given something to count");

                _runner.EventListener = MockRepository.GenerateStub<IEventListener>();
                _runner.RunScenarios(new List<ScenarioWithSteps> { firstScenario, secondScenario });
            }

            [Specification]
            public void should_Call_before_Scenario_once_per_scenario()
            {
                Assert.That(_timesBeforeScenarioWasCalled, Is.EqualTo(2));
            }

            [Specification]
            public void should_Call_after_Scenario_once_per_scenario()
            {
                Assert.That(_timesAfterScenarioWasCalled, Is.EqualTo(2));
            }

            [Specification]
            public void should_Call_before_step_once_per_step()
            {
                Assert.That(_timesBeforeStepWasCalled, Is.EqualTo(3));
            }

            [Specification]
            public void should_call_after_step_once_per_step()
            {
                Assert.That(_timesAfterStepWasCalled, Is.EqualTo(3));
            }
        }
    }
}