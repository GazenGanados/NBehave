﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NBehave.Narrator.Framework.Specifications.Text
{
    [TestFixture]
    public abstract class ScenarioParserSpec
    {
        private GherkinScenarioParser CreateScenarioParser()
        {
            return new GherkinScenarioParser();
        }

        private StringStep NewStringStep(string step)
        {
            return new StringStep(step, "filename");
        }

        private IEnumerable<ScenarioWithSteps> _scenarios;
        private IEnumerable<Feature> _feature;

        protected void Parse(string scenario)
        {
            var parser = CreateScenarioParser();
            _feature = parser.Parse(scenario.ToStream());
            _scenarios = _feature.SelectMany(feature => feature.Scenarios);
        }

        [TestFixture]
        public class ScenarioSimpleScenarioWithoutTitle : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3";

                Parse(scenario);
            }

            [Test]
            public void ShouldHaveGivenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("Given numbers 1 and 2"));
            }

            [Test]
            public void ShouldHaveWhenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("When I add the numbers"));
            }

            [Test]
            public void ShouldHaveThenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("Then the sum is 3"));
            }
        }

        public class ScenarioSimpleScenarioWithTitle : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Scenario: Adding numbers" + Environment.NewLine +
                                  "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3";

                Parse(scenario);
            }

            [Test]
            public void ShouldFind3Steps()
            {
                Assert.That(_scenarios.First().Steps.Count(), Is.EqualTo(3));
            }

            [Test]
            public void ShouldHaveAScenarioTitle()
            {
                Assert.That(_scenarios.First().Title, Is.EqualTo("Adding numbers"));
            }
        }

        public class ScenarioTwoScenariosWithTitle : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Scenario: Adding numbers" + Environment.NewLine +
                                  "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3" + Environment.NewLine +
                                  Environment.NewLine +
                                  "Scenario: Adding numbers again" + Environment.NewLine +
                                  "  Given numbers 3 and 5" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 8";

                Parse(scenario);
            }

            [Test]
            public void ShouldFind2Scenarios()
            {
                Assert.That(_scenarios.Count(), Is.EqualTo(2));
            }

            [Test]
            public void ShouldHaveAScenarioTitleOnBothScenarios()
            {
                Assert.That(_scenarios.First().Title, Is.EqualTo("Adding numbers"));
                Assert.That(_scenarios.Skip(1).First().Title, Is.EqualTo("Adding numbers again"));
            }
        }

        public class ScenarioFeatureWithScenario : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Feature: Calculator" + Environment.NewLine +
                                  "Scenario: Adding numbers" + Environment.NewLine +
                                  "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3" + Environment.NewLine +
                                  Environment.NewLine +
                                  "Scenario: Adding numbers again" + Environment.NewLine +
                                  "  Given numbers 3 and 5" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 8";

                Parse(scenario);
            }

            [Test]
            public void ShouldFind2Scenarios()
            {
                Assert.That(_scenarios.Count(), Is.EqualTo(2));
            }

            [Test]
            public void ShouldHaveAFeatureTitle()
            {
                Assert.That(_scenarios.First().Feature.Title, Is.EqualTo("Calculator"));
            }
        }

        public class ScenarioFeatureNarrativeIsAllTextUptoNextStepKeyWord : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Feature: Calculator" + Environment.NewLine +
                                  "  This is the narrative" + Environment.NewLine +
                                  "  This is second row of narrative" + Environment.NewLine +
                                  "Scenario: Adding numbers" + Environment.NewLine +
                                  "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3" + Environment.NewLine +
                                  Environment.NewLine +
                                  "Scenario: Adding numbers again" + Environment.NewLine +
                                  "  Given numbers 3 and 5" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 8";

                Parse(scenario);
            }

            [Test]
            public void ShouldHaveNarrative()
            {
                Assert.That(_scenarios.First().Feature.Narrative, Is.EqualTo(
                                                                 "This is the narrative" + Environment.NewLine +
                                                                 "This is second row of narrative"));
            }

            [Test]
            public void ShouldFind2Scenarios()
            {
                Assert.That(_scenarios.Count(), Is.EqualTo(2));
            }

            [Test]
            public void ShouldHaveAFeatureTitle()
            {
                Assert.That(_scenarios.First().Feature.Title, Is.EqualTo("Calculator"));
            }
        }

        public class ScenarioScenarioWithExampleTable : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Scenario: Adding numbers" + Environment.NewLine +
                                  "  Given numbers [left] and [right]" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is [sum]" + Environment.NewLine +
                                  Environment.NewLine +
                                  "Examples:" + Environment.NewLine +
                                  "|left|right|sum|" + Environment.NewLine +
                                  "|1|2|3|" + Environment.NewLine +
                                  "|2|3|5|";

                Parse(scenario);
            }

            [Test]
            public void ScenarioShouldHaveTwoExamples()
            {
                Assert.That(_scenarios.First().Examples.Count(), Is.EqualTo(2));
            }

            [Test]
            public void ShouldFind3Steps()
            {
                Assert.That(_scenarios.First().Steps.Count(), Is.EqualTo(3));
            }

            [Test]
            public void ShouldHaveAScenarioTitle()
            {
                Assert.That(_scenarios.First().Title, Is.EqualTo("Adding numbers"));
            }

            [Test]
            public void ShouldHaveGivenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("Given numbers [left] and [right]"));
            }

            [Test]
            public void ShouldHaveThenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("Then the sum is [sum]"));
            }
        }

        public class ScenarioScenarioWithTableOnGiven : ScenarioParserSpec
        {
            private StringTableStep _givenStep;
            private StringTableStep _thenStep;

            [SetUp]
            public void Scenario()
            {
                var scenario = "  Given the following people exists:" + Environment.NewLine +
                                  "  |Name          |Country|" + Environment.NewLine +
                                  "  |Morgan Persson|Sweden |" + Environment.NewLine +
                                  "  |Jimmy Nilsson |Sweden |" + Environment.NewLine +
                                  "  |Jimmy bogard  |USA    |" + Environment.NewLine +
                                  "  When I search for people in sweden" + Environment.NewLine +
                                  "  Then I should get:" + Environment.NewLine +
                                  "  |Name          |" + Environment.NewLine +
                                  "  |Morgan Persson|" + Environment.NewLine +
                                  "  |Jimmy Nilsson |";

                Parse(scenario);
                this._givenStep = _scenarios.First().Steps.First() as StringTableStep;
                this._thenStep = _scenarios.First().Steps.Last() as StringTableStep;
            }

            [Test]
            public void GivenStepShouldHaveThreeTableSteps()
            {
                Assert.That(this._givenStep.TableSteps.Count(), Is.EqualTo(3));
            }

            [Test]
            public void ThenStepShouldHaveTwoTableSteps()
            {
                Assert.That(this._thenStep.TableSteps.Count(), Is.EqualTo(2));
            }

            [Test]
            public void TableStepColumnNamesShouldBeStoredInLowerCase()
            {
                var step = this._givenStep.TableSteps.First();
                CollectionAssert.Contains(step.ColumnNames, "name");
                Assert.That(step.ColumnValues["name"], Is.Not.Null); 
            }

            [Test]
            public void ShouldHaveGivenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("Given the following people exists:"));
            }

            [Test]
            public void ShouldHaveWhenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("When I search for people in sweden"));
            }

            [Test]
            public void ShouldHaveThenStep()
            {
                CollectionAssert.Contains(_scenarios.First().Steps, NewStringStep("Then I should get:"));
            }
        }

        public class ScenarioMultipleFeatures : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Feature: Calculator 1" + Environment.NewLine +
                                  "Scenario: Adding numbers 1" + Environment.NewLine +
                                  "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3" + Environment.NewLine +
                                  "" + Environment.NewLine +
                                  "Feature: Calculator 2" + Environment.NewLine +
                                  "Scenario: Adding numbers 2" + Environment.NewLine +
                                  "  Given numbers 1 and 2" + Environment.NewLine +
                                  "  When I add the numbers" + Environment.NewLine +
                                  "  Then the sum is 3";

                Parse(scenario);
            }

            [Test]
            public void Feature1ShouldBeReferencedByScenario2()
            {
                Assert.That(_scenarios.First().Feature.Title, Is.EqualTo("Calculator 1"));
            }

            [Test]
            public void Feature2ShouldBeReferencedByScenario2()
            {
                Assert.That(_scenarios.Skip(1).First().Feature.Title, Is.EqualTo("Calculator 2"));
            }
        }

        public class ScenarioWithBackground : ScenarioParserSpec
        {
            [SetUp]
            public void Scenario()
            {
                var scenario = "Feature: Support for background sections                                   " + Environment.NewLine +
                               "  As a NBehave user                                                        " + Environment.NewLine +
                               "  I want to be able to declare background sections                         " + Environment.NewLine +
                               "  So that I can add context to my scenarios                                " + Environment.NewLine +
                               "                                                                           " + Environment.NewLine +
                               "  Background:                                                              " + Environment.NewLine +
                               "    Given this background section declaration                              " + Environment.NewLine +
                               "    And this one                                                           " + Environment.NewLine +
                               "                                                                           " + Environment.NewLine +
                               "  Scenario: Running a feature file with a background section               " + Environment.NewLine +
                               "    Given this scenario under the context of a background section          " + Environment.NewLine +
                               "    When the scenario is executed                                          " + Environment.NewLine +
                               "    Then the background section steps should be called before this scenario" + Environment.NewLine;

                Parse(scenario);
            }

            [Test]
            public void ShouldHaveAddedBackgroundGivenStepToFeature()
            {
                Assert.That(_scenarios.First().Steps.First().Step, Is.EqualTo("Given this background section declaration"));
            }

            [Test]
            public void ShouldHaveAddedBackgroundAndStepToFeature()
            {
                Assert.That(_scenarios.First().Steps.Skip(1).First().Step, Is.EqualTo("And this one"));
            }
        }
    }
}