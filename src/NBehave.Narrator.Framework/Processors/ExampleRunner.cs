using System;
using System.Collections.Generic;
using System.Linq;

namespace NBehave.Narrator.Framework.Processors
{
    public class ExampleRunner
    {
        private readonly IStringStepRunner _stringStepRunner;

        public ExampleRunner(IStringStepRunner stringStepRunner)
        {
            _stringStepRunner = stringStepRunner;
        }

        public ScenarioExampleResult RunExamples(Scenario scenario, 
            Func<IEnumerable<StringStep>, IEnumerable<StepResult>> runSteps, 
            Action beforeScenario,
            Action<Scenario, ScenarioResult> afterScenario)
        {
            var exampleResults = new ScenarioExampleResult(scenario.Feature, scenario.Title, scenario.Steps, scenario.Examples);

            foreach (var example in scenario.Examples)
            {
                beforeScenario();
                var scenarioResult = RunExample(scenario, runSteps, example);
                afterScenario(scenario, scenarioResult);
                exampleResults.AddResult(scenarioResult);
            }
            return exampleResults;
        }

        private ScenarioResult RunExample(Scenario scenario, Func<IEnumerable<StringStep>, IEnumerable<StepResult>> runSteps, Example example)
        {
            var steps = BuildSteps(scenario, example);

            var scenarioResult = new ScenarioResult(scenario.Feature, scenario.Title);
            var stepResults = runSteps(steps);
            scenarioResult.AddActionStepResults(stepResults);
            return scenarioResult;
        }

        private IEnumerable<StringStep> BuildSteps(Scenario scenario, Row example)
        {
            return scenario.Steps.Select(step => step.BuildStep(example));
        }
    }
}