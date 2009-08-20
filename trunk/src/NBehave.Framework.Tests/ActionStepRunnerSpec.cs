﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using NBehave.Narrator.Framework.EventListeners;
using NBehave.Narrator.Framework.EventListeners.Xml;
using NUnit.Framework.SyntaxHelpers;
using NUnit.Framework;
using Context = NUnit.Framework.TestFixtureAttribute;
using Specification = NUnit.Framework.TestAttribute;


namespace NBehave.Narrator.Framework.Specifications
{
    [Context]
    public class ActionStepRunnerSpec
    {
        private StoryResults RunAction(string actionStep, ActionStepRunner runner)
        {
            var ms = new MemoryStream();
            var sr = new StreamWriter(ms);
            sr.WriteLine(actionStep);
            sr.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            runner.Load(ms);
            var writer = new StringWriter();
            var listener = new TextWriterEventListener(writer);
            return runner.Run(listener);
        }

        [Context]
        public class When_running_plain_text_scenarios : ActionStepRunnerSpec
        {
            private ActionStepRunner _runner;
            [SetUp]
            public void SetUp()
            {
                _runner = new ActionStepRunner();
                _runner.LoadAssembly("TestPlainTextAssembly.dll");
            }

            [Specification]
            public void Should_find_Given_ActionStep_in_assembly()
            {
                Assert.That(_runner.ActionCatalog.ActionExists("my name is Axel"), Is.True);
            }

            [Specification]
            public void Should_find_When_ActionStep_in_assembly()
            {
                Assert.That(_runner.ActionCatalog.ActionExists("I'm greeted"), Is.True);
            }

            [Specification]
            public void Should_find_Then_ActionStep_in_assembly()
            {
                Assert.That(_runner.ActionCatalog.ActionExists("I should be greeted with “Hello, Axel!”"), Is.True);
            }

            [Specification]
            public void Should_invoke_action_given_a_token_string()
            {
                _runner.InvokeTokenString("my name is Morgan");
            }

            [Specification, ExpectedException(typeof(ArgumentException))]
            public void Should_throw_ArgumentException_if_action_given_in_token_string_doesnt_exist()
            {
                _runner.InvokeTokenString("This doesnt exist");
            }

            [Specification]
            public void Should_run_text_scenario_in_stream()
            {
                var ms = new MemoryStream();
                var sr = new StreamWriter(ms);
                sr.WriteLine("Given my name is Morgan");
                sr.WriteLine("When I'm greeted");
                sr.WriteLine("Then I should be greeted with “Hello, Morgan!”");
                sr.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                _runner.Load(ms);
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                StoryResults result = _runner.Run(listener);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_run_scenarios_in_text_file()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystem.txt" });
                StoryResults result = _runner.Run(listener);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_get_result_of_running_scenarios_in_text_file()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystem.txt" });
                StoryResults results = _runner.Run(listener);
                Assert.That(results.NumberOfThemes, Is.EqualTo(0));
                Assert.That(results.NumberOfStories, Is.EqualTo(0));
                Assert.That(results.NumberOfScenariosFound, Is.EqualTo(1));
                Assert.That(results.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_get_correct_errormessage_from_failed_scenario()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystemFailure.txt" });
                StoryResults results = _runner.Run(listener);
                Assert.That(results.NumberOfFailingScenarios, Is.EqualTo(1));
                Assert.That(results.ScenarioResults[0].Message.StartsWith("NUnit.Framework.AssertionException :"), Is.True);
            }

            [Specification]
            public void Should_mark_failing_step_as_failed_in_output()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystemFailure.txt" });
                StoryResults results = _runner.Run(listener);
                StringAssert.Contains("Then I should be greeted with “Hello, Scott!” - FAILED", writer.ToString());
            }

            [Specification]
            public void Should_execute_more_than_one_scenario_in_text_file()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystem_ManyGreetings.txt" });
                StoryResults results = _runner.Run(listener);
                Assert.That(results.NumberOfThemes, Is.EqualTo(0));
                Assert.That(results.NumberOfStories, Is.EqualTo(0));
                Assert.That(results.NumberOfScenariosFound, Is.EqualTo(2));
                Assert.That(results.NumberOfPassingScenarios, Is.EqualTo(2));
            }

            [Specification]
            public void Should_run_scenario_in_text_file_with_scenario_title()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystemWithScenarioTitle.txt" });
                var results = _runner.Run(listener);

                Assert.That(results.ScenarioResults[0].ScenarioTitle, Is.EqualTo("A simple greeting example"));
                Assert.That(results.ScenarioResults[0].ScenarioResult, Is.EqualTo(ScenarioResult.Passed));
            }

            [Specification]
            public void Should_run_text_scenario_whith_newlines_in_given()
            {
                var ms = new MemoryStream();
                var sr = new StreamWriter(ms);
                sr.WriteLine("Given");
                sr.WriteLine("my name is Morgan");
                sr.WriteLine("When I'm greeted");
                sr.WriteLine("Then I should be greeted with “Hello, Morgan!”");
                sr.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                _runner.Load(ms);
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                StoryResults result = _runner.Run(listener);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_set_scenario_pending_if_action_given_in_token_string_doesnt_exist()
            {
                var stream = new MemoryStream();
                var sr = new StreamWriter(stream);
                sr.WriteLine("Given something that has no ActionStep");
                sr.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                _runner.Load(stream);
                StoryResults result = _runner.Run(new NullEventListener());
                Assert.That(result.NumberOfPendingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_list_all_pending_actionSteps()
            {
                var stream = new MemoryStream();
                var sr = new StreamWriter(stream);
                sr.WriteLine("Given something that has no ActionStep");
                sr.WriteLine("And something else that has no ActionStep");
                sr.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                _runner.Load(stream);
                StoryResults result = _runner.Run(new NullEventListener());
                StringAssert.Contains("No matching Action found for \"Given something that has no ActionStep\"", result.ScenarioResults[0].Message);
                StringAssert.Contains("No matching Action found for \"And something else that has no ActionStep\"", result.ScenarioResults[0].Message);
            }

            [Specification]
            public void Should_use_wildcard_and_run_all_scenarios_in_all_matching_text_files()
            {
                var writer = new StringWriter();
                var listener = new TextWriterEventListener(writer);
                _runner.Load(new[] { @"GreetingSystem*.txt" });
                StoryResults result = _runner.Run(listener);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(4));
            }
        }

        [Context]
        public class When_running_plain_text_scenarios_with_non_string_parameters : ActionStepRunnerSpec
        {
            [ActionSteps]
            public class ActionStepsParameterTypes
            {
                [ActionStep("Given a parameter of type Int32 with value $value")]
                public void param_is_int(int param)
                {
                    Assert.AreEqual(42, param);
                }

                [ActionStep("Given a parameter of type decimal with value $value")]
                public void param_is_decimal(decimal param)
                {
                    const decimal expected = 42;
                    Assert.AreEqual(expected, param);
                }
            }

            private ActionStepRunner _runner;
            [SetUp]
            public void SetUp()
            {
                _runner = new ActionStepRunner();
                string path = GetType().Assembly.Location;
                _runner.LoadAssembly(path);
            }

            [Specification]
            public void Should_run_scenario_with_int_parameter()
            {
                StoryResults result = RunAction("Given a parameter of type Int32 with value 42", _runner);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_run_scenario_with_decimal_parameter()
            {
                StoryResults result = RunAction("Given a parameter of type decimal with value 42", _runner);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }
        }

        [Context, ActionSteps]
        public class When_having_ActionStepAttribute_multiple_times_on_same_method : ActionStepRunnerSpec
        {
            [ActionStep("Given one")]
            [ActionStep("Given two")]
            public void Multiple()
            {
                Assert.IsTrue(true);
            }

            private ActionStepRunner _runner;
            [SetUp]
            public void SetUp()
            {
                _runner = new ActionStepRunner();
                string path = GetType().Assembly.Location;
                _runner.LoadAssembly(path);
            }

            [Specification]
            public void Should_run_scenario_using_first_ActionStep_registration()
            {
                StoryResults result = RunAction("Given one", _runner);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_run_scenario_using_second_ActionStep_registration()
            {
                //Fix this in ActionStepRunner.GetMethodsWithActionStepAttribute
                StoryResults result = RunAction("Given two", _runner);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }
        }

        [Context, ActionSteps]
        public class When_having_ActionStepAttribute_without_tokenString : ActionStepRunnerSpec
        {
            [ActionStep()]
            public void Given_a_method_with_no_parameters()
            {
                Assert.IsTrue(true);
            }

            private int _intParam;
            private string _stringParam;

            [ActionStep()]
            public void Given_a_method_with_a_value_intParam_plus_text_stringParam(int intParam, string stringParam)
            {
                _intParam = intParam;
                _stringParam = stringParam;
            }

            [ActionStep()]
            public void Then_value_should_equal_expected(int expected)
            {
                Assert.AreEqual(expected, _intParam);
            }

            [ActionStep()]
            public void Then_text_should_equal_expected(string expected)
            {
                Assert.AreEqual(expected, _stringParam);
            }

            private ActionStepRunner _runner;
            [SetUp]
            public void SetUp()
            {
                _runner = new ActionStepRunner();
                string path = GetType().Assembly.Location;
                _runner.LoadAssembly(path);
            }

            [Specification]
            public void Should_use_method_name_as_tokenString()
            {
                StoryResults result = RunAction("Given a method with no parameters", _runner);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_use_infer_parameters_in_tokenString_from_parameterNames_in_method()
            {
                StoryResults result = RunAction("Given a method with a value 3 plus text HELLO\nThen value should equal 3\nAnd text should equal HELLO", _runner);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }
        }

        [Context]
        public class When_running_plain_text_scenarios_with_xml_listener : ActionStepRunnerSpec
        {
            private ActionStepRunner _runner;

            [SetUp]
            public void SetUp()
            {
                _runner = new ActionStepRunner();
                _runner.LoadAssembly("TestPlainTextAssembly.dll");
            }

            [Specification]
            public void Should_run_scenarios_in_text_file()
            {
                var writer = new XmlTextWriter(new MemoryStream(), Encoding.UTF8);
                var listener = new XmlOutputEventListener(writer);
                _runner.Load(new[] { @"GreetingSystem.txt" });
                StoryResults result = _runner.Run(listener);
                Assert.That(result.NumberOfPassingScenarios, Is.EqualTo(1));
            }

            [Specification]
            public void Should_set_correct_story_title_in_result_xml()
            {
                var writer = new XmlTextWriter(new MemoryStream(), Encoding.UTF8);
                var listener = new XmlOutputEventListener(writer);

                var ms = new MemoryStream();
                var sr = new StreamWriter(ms);
                sr.WriteLine("Story: A fancy greeting system");
                sr.WriteLine("Scenario: A greeting");
                sr.WriteLine("Given my name is Morgan");
                sr.WriteLine("When I'm greeted");
                sr.WriteLine("Then I should be greeted with “Hello, Morgan!”");
                sr.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                _runner.Load(ms);

                StoryResults result = _runner.Run(listener);
                Assert.That(result.ScenarioResults[0].StoryTitle, Is.EqualTo("A fancy greeting system"));
            }
        }

        [Context]
        public class When_running_plain_text_scenarios_with_story : ActionStepRunnerSpec
        {
            private StoryResults _result;

            [SetUp]
            public void SetUp()
            {
                var _runner = new ActionStepRunner();
                _runner.LoadAssembly("TestPlainTextAssembly.dll");

                string actionSteps = "Story: Greeting system" + Environment.NewLine +
                                        "As a project member" + Environment.NewLine +
                                        "I want specs written in a non techie way" + Environment.NewLine +
                                        "So that everyone can understand them" + Environment.NewLine +
                                        "Scenario: Greeting someone" + Environment.NewLine +
                                        "Given my name is Morgan" + Environment.NewLine +
                                        "When I'm greeted" + Environment.NewLine +
                                        "Then I should be greeted with “Hello, Morgan!”";

                _result = RunAction(actionSteps, _runner);
            }

            [Specification]
            public void Should_set_story_title_on_result()
            {
                Assert.That(_result.ScenarioResults[0].StoryTitle, Is.EqualTo("Greeting system"));
            }

            [Specification]
            public void Should_set_scenario_title_on_result()
            {
                Assert.That(_result.ScenarioResults[0].ScenarioTitle, Is.EqualTo("Greeting someone"));
            }
        }

        [Context]
        public class When_running_plain_text_scenarios_with_story_events_raised : ActionStepRunnerSpec
        {
            private Story _storyCreated;
            private Scenario _scenarioCreated;

            [SetUp]
            public void SetUp()
            {
                var _runner = new ActionStepRunner();
                Story.StoryCreated += (o, e) => _storyCreated = e.EventData;
                Story.ScenarioCreated += (o, e) => _scenarioCreated = e.EventData;
                _runner.LoadAssembly("TestPlainTextAssembly.dll");

                string actionSteps = "Story: Greeting system" + Environment.NewLine +
                                        "As a project member" + Environment.NewLine +
                                        "I want specs written in a non techie way" + Environment.NewLine +
                                        "So that everyone can understand them" + Environment.NewLine +
                                        "Scenario: Greeting someone" + Environment.NewLine +
                                        "Given my name is Morgan" + Environment.NewLine +
                                        "When I'm greeted" + Environment.NewLine +
                                        "Then I should be greeted with “Hello, Morgan!”";

                var result = RunAction(actionSteps, _runner);
            }

            [Specification]
            public void Should_get_story_created_event()
            {
                Assert.IsNotNull(_storyCreated);
            }

            [Specification]
            public void Should_get_story_title()
            {
                Assert.That(_storyCreated.Title, Is.EqualTo("Greeting system"));
            }

            [Specification]
            public void Should_get_story_narrative()
            {
                StringAssert.Contains("As a", _storyCreated.Narrative);
                StringAssert.Contains("I want", _storyCreated.Narrative);
                StringAssert.Contains("So that", _storyCreated.Narrative);
            }

            [Specification]
            public void Should_get_scenario_created_event()
            {
                Assert.IsNotNull(_scenarioCreated);
            }

            [Specification]
            public void Should_get_scenario_title()
            {
                Assert.That(_scenarioCreated.Title, Is.EqualTo("Greeting someone"));
            }
        }
    }
}
