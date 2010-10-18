using System;
using NBehave.Narrator.Framework;
using NBehave.Spec.Extensions;
using NBehave.Spec.Xunit;
using NUnit.Framework;

namespace Xunit.SpecBase_Specifications
{
    public class ScenarioDrivenSpecBaseSpecs : ScenarioDrivenSpecBase
    {
        protected override Feature CreateFeature()
        {
            return new Feature("Setting up features and scenarios")
                .AddStory()
                .AsA("developer")
                .IWant("to specify a feature")
                .SoThat("I can specify behaviour through scenarios");
        }

        [Test]
        public void should_populate_the_feature_narrative()
        {
            Assert.Equal("As a developer, I want to specify a feature so that I can specify behaviour through scenarios", Feature.Narrative);
        }

        [Test]
        public void should_execute_scenarios_implemented_inline()
        {
            string detail1 = null;
            string detail2 = null;
            Feature.AddScenario()
                .Given("a scenario with inline implementation", () => detail1 = "implementation")
                .When("the scenario is executed", () => detail2 = "implementation")
                .Then("the implementation from Given should be called", () => Assert.NotNull(detail1))
                .And("the implementation from When should be called", () => Assert.NotNull(detail2));
        }

        [Test]
        public void should_call_notification_events_before_executing_inline_implementation()
        {
            string lastLoggedScenario = null;
            EventHandler<EventArgs<ActionStepText>> logger = (sender, e) => lastLoggedScenario = e.EventData.Step;
            ScenarioWithSteps.StepAdded += logger;

            try
            {
                Feature.AddScenario()
                    .Given("a scenario with inline implementation", () => Assert.Equal("a scenario with inline implementation", lastLoggedScenario))
                    .When("the scenario is executed", () => Assert.Equal("the scenario is executed", lastLoggedScenario))
                    .Then("the implementation from Given should be called", () => Assert.Equal("the implementation from Given should be called", lastLoggedScenario))
                    .And("the implementation from When should be called", () => Assert.Equal("the implementation from When should be called", lastLoggedScenario));
            }
            finally
            {
                ScenarioWithSteps.StepAdded -= logger;
            }
        }
    }
}
