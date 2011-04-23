﻿using System;
using System.Text.RegularExpressions;
using NUnit.Framework;


namespace NBehave.Narrator.Framework.Specifications
{
    [TestFixture]
    public class StringStepRunnerSpec
    {
        private IStringStepRunner _runner;
        private ActionCatalog _actionCatalog;

        [SetUp]
        public void SetUp()
        {
            _actionCatalog = new ActionCatalog();
            _runner = new StringStepRunner(_actionCatalog);
        }

        [TestFixture]
        public class WhenRunningPlainTextScenarios : StringStepRunnerSpec
        {
            [Test]
            public void ShouldInvokeActionGivenATokenString()
            {
                var wasCalled = false;
                Action<string> action = name => { wasCalled = true; };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"my name is (?<name>\w+)"), action, action.Method, "Given"));
                _runner.Run(new ActionStepText("Given my name is Morgan", ""));
                Assert.IsTrue(wasCalled, "Action was not called");
            }

            [Test]
            public void ShouldGetParameterValueForAction()
            {
                var actual = string.Empty;
                Action<string> action = name => { actual = name; };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"my name is (?<name>\w+)"), action, action.Method, "Given"));
                _runner.Run(new ActionStepText("Given my name is Morgan", ""));
                Assert.That(actual, Is.EqualTo("Morgan"));
            }

            [Test]
            public void ShouldReturnPendingIfActionGivenInTokenStringDoesntExist()
            {
                var result = _runner.Run(new ActionStepText("Given this doesnt exist", ""));
                Assert.That(result.Result, Is.TypeOf(typeof(Pending)));
            }
        }

        [TestFixture, ActionSteps]
        public class WhenClassWithActionStepsImplementsNotificationAttributes : StringStepRunnerSpec
        {
            private bool _beforeScenarioWasCalled;
            private bool _beforeStepWasCalled;
            private bool _afterStepWasCalled;
            private bool _afterScenarioWasCalled;

            [Given(@"something$")]
            public void GivenSomething()
            { }

            [BeforeScenario]
            public void OnBeforeScenario()
            {
                _beforeScenarioWasCalled = true;
            }

            [BeforeStep]
            public void OnBeforeStep()
            {
                _beforeStepWasCalled = true;
            }

            [AfterStep]
            public void OnAfterStep()
            {
                _afterStepWasCalled = true;
            }

            [AfterScenario]
            public void OnAfterScenario()
            {
                _afterScenarioWasCalled = true;
            }

            [SetUp]
            public void Setup()
            {
                SetUp();

                Action action = GivenSomething;
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"something"), action, action.Method, "Given", this));

                _beforeScenarioWasCalled = false;
                _beforeStepWasCalled = false;
                _afterStepWasCalled = false;
                _afterScenarioWasCalled = false;
            }

            [Test]
            public void RunningAStepShouldCallMostAttributedMethods()
            {
                var actionStepText = new ActionStepText("something", "");
                _runner.Run(actionStepText);

                Assert.That(_beforeScenarioWasCalled);
                Assert.That(_beforeStepWasCalled);
                Assert.That(_afterStepWasCalled);
                Assert.That(!_afterScenarioWasCalled);
            }

            [Specification]
            public void Completing_a_scenario_should_call_all_attributed_methods()
            {
                var actionStepText = new ActionStepText("something", "");
                _runner.Run(actionStepText);
                _runner.OnCloseScenario();

                Assert.That(_beforeScenarioWasCalled);
                Assert.That(_beforeStepWasCalled);
                Assert.That(_afterStepWasCalled);
                Assert.That(_afterScenarioWasCalled);
            }

        }
    }
}
