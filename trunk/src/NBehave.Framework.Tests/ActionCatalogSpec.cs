﻿using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Context = NUnit.Framework.TestFixtureAttribute;
using Specification = NUnit.Framework.TestAttribute;

namespace NBehave.Narrator.Framework.Specifications
{
    [Context]
    public class ActionCatalogSpec
    {
        private MethodInfo GetDummyParameterInfo()
        {
            Action<int> a = p => { };
            return a.Method;
        }

        [Context]
        public class when_adding_an_action_to_the_catalog:ActionCatalogSpec
        {
            [Specification]
            public void should_consider_the_2_actions_as_equal()
            {
                var catalog = new ActionCatalog();
                catalog.Add("my savings account balance is $balance", new object(), GetDummyParameterInfo());
                bool actionExists = catalog.ActionExists("my savings account balance is 500");

                Assert.That(actionExists, Is.True);
            }

            [Specification]
            public void should_consider_all_whitespace_as_equal()
            {
                var catalog = new ActionCatalog();

                catalog.Add("my savings account\nbalance is $balance", new object(), GetDummyParameterInfo());
                bool actionExists = catalog.ActionExists("my\tsavings account balance is 500");

                Assert.That(actionExists, Is.True);
            }

            [Specification]
            public void should_get_action()
            {
                var catalog = new ActionCatalog();

                catalog.Add("my savings account balance is $balance", new object(), GetDummyParameterInfo());
                ActionMethodInfo action = catalog.GetAction("my savings account balance is 500");

                Assert.That(action, Is.Not.Null);
            }

            [Specification]
            public void should_get_action_with_token_in_middle_of_string()
            {
                var catalog = new ActionCatalog();
                Action<int> action = accountBalance => { };
                catalog.Add("I have $amount euros on my cash account", action, GetDummyParameterInfo());
                ActionMethodInfo actionFetched = catalog.GetAction("I have 20 euros on my cash account");

                Assert.That(actionFetched, Is.Not.Null);
            }
        }

        [Context]
        public class When_fetching_parameters_for_actionStep : TextRunnerSpec
        {
            ActionCatalog _actionCatalog;

            [SetUp]
            public void Establish_context()
            {
                _actionCatalog = new ActionCatalog();
            }

            [Specification]
            public void should_get_parameter_for_action_with_token_in_middle_of_string()
            {
                var catalog = new ActionCatalog();
                Action<int> action = accountBalance => { };
                catalog.Add("I have $amount euros on my cash account", action, action.Method);
                object[] values = catalog.GetParametersForMessage("I have 20 euros on my cash account");

                Assert.That(values.Length, Is.EqualTo(1));
                Assert.That(values[0].GetType(), Is.EqualTo(typeof(int)));
            }

            [Specification]
            public void should_get_parameter_for_action_if_token_has_newlines()
            {
                var catalog = new ActionCatalog();
                Action<string> action = someAction => { };
                catalog.Add("I have a board like this\n$board", action, action.Method);
                object[] values = catalog.GetParametersForMessage("I have a board like this\nxo \n x \no x");

                Assert.That(values.Length, Is.EqualTo(1));
                Assert.That(values[0], Is.EqualTo("xo \n x \no x"));
            }

            [Specification]
            public void should_get_parameters_for_message_with_action_registered_twice()
            {
                var catalog = new ActionCatalog();
                Action<string> action = someAction => { };
                catalog.Add("Given $value something", action, action.Method);
                catalog.Add("And $value something", action, action.Method);
                object[] givenValue = catalog.GetParametersForMessage("Given 20 something");
                object[] andValue = catalog.GetParametersForMessage("And 20 something");

                Assert.That(givenValue.Length, Is.EqualTo(1));
                Assert.That(andValue.Length, Is.EqualTo(1));
            }

            [Specification]
            public void should_get_parameters_for_message_with_a_negative_parameter()
            {
                var catalog = new ActionCatalog();
                Action<string> action = someAction => { };
                catalog.Add("Given $value something", action, action.Method);
                object[] givenValue = catalog.GetParametersForMessage("Given -20 something");

                Assert.That(givenValue.Length, Is.EqualTo(1));
                Assert.That(givenValue.First(), Is.EqualTo("-20"));
            }

            [Specification]
            public void Should_get_int_parameter()
            {
                Action<int> action = value => { };
                _actionCatalog.Add( new ActionMethodInfo(new Regex(@"an int (?<value>\d+)"), action, action.Method));
                object[] values = _actionCatalog.GetParametersForMessage("an int 42");
                Assert.That(values[0], Is.TypeOf(typeof(int)));

            }

            [Specification]
            public void Should_get_decimal_parameter()
            {
                Action<decimal> action = value => { };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a decimal (?<value>\d+)"), action, action.Method));
                object[] values = _actionCatalog.GetParametersForMessage("a decimal 42");
                Assert.That(values[0], Is.TypeOf(typeof(decimal)));
            }

            [Specification]
            public void Should_get_multiline_value_as_string()
            {
                Action<object> action = value => { };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a string\s+(?<value>(\w+\s+)*)"), action, action.Method));
                string multiLineValue = "one" + Environment.NewLine + "two";
                string actionString = "a string " + multiLineValue;
                object[] values = _actionCatalog.GetParametersForMessage(actionString);
                Assert.That(values[0], Is.TypeOf(typeof(string)));
            }

            [Specification]
            public void Should_get_multiline_value_as_array_of_strings()
            {
                // problem is, Action is Action<object> => o=> { MethodCall(o as string[]); }
                object paramReceived = null;
                Action<string[]> actionStep = p => { };
                Action<object> action = value => { paramReceived = value; };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a string\s+(?<value>(\w+\s+)+)"), action, actionStep.Method));
                string multiLineValue = "one" + Environment.NewLine + "two";
                string actionString = "a string " + Environment.NewLine + multiLineValue;
                object[] values = _actionCatalog.GetParametersForMessage(actionString);
                Assert.That(values[0], Is.TypeOf(typeof(string[])));
            }

            [Specification]
            public void Should_remove_empty_entries_at_end_of_array_values()
            {
                Action<string[]> action = value => { };

                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a string\s+(?<value>(\w+\s*)+)"), action, action.Method));
                string multiLineValue = "one" + Environment.NewLine + "two" + Environment.NewLine;
                string actionString = "a string " + Environment.NewLine + multiLineValue;
                object[] values = _actionCatalog.GetParametersForMessage(actionString);
                Assert.That((values[0] as string[]), Is.EqualTo(new string[] { "one", "two" }));
            }
        }
    }
}
