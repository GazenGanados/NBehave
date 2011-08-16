using System;
using System.Collections.Generic;
using System.Linq;
using NBehave.Narrator.Framework.Tiny;

namespace NBehave.Narrator.Framework.Processors
{
    public class ScenarioRunner : IScenarioRunner
    {
        private readonly ITinyMessengerHub _hub;
        private readonly IStringStepRunner _stringStepRunner;

        public ScenarioRunner(ITinyMessengerHub hub, IStringStepRunner stringStepRunner)
        {
            _hub = hub;
            _stringStepRunner = stringStepRunner;
        }

        public void Run(Feature feature)
        {
            foreach (var scenario in feature.Scenarios)
            {
                _hub.Publish(new ScenarioStartedEvent(this, scenario));
                if (scenario.Examples.Any())
                    RunExamples(scenario);
                else
                    RunScenario(scenario);
                _hub.Publish(new ScenarioFinishedEvent(this, scenario));
            }
        }

        private void RunScenario(Scenario scenario)
        {
            var scenarioResult = new ScenarioResult(scenario.Feature, scenario.Title);
            BeforeScenario();
            var backgroundResults = RunBackground(scenario.Feature.Background);
            scenarioResult.AddActionStepResults(backgroundResults);
            var stepResults = RunSteps(scenario.Steps);
            scenarioResult.AddActionStepResults(stepResults);
            AfterScenario(scenario, scenarioResult);
            _hub.Publish(new ScenarioResultEvent(this, scenarioResult));
        }

        private void RunExamples(Scenario scenario)
        {
            var runner = new ExampleRunner(_stringStepRunner);
            var exampleResults = runner.RunExamples(scenario, RunSteps, BeforeScenario, AfterScenario);
            _hub.Publish(new ScenarioResultEvent(this, exampleResults));
        }

        private IEnumerable<StepResult> RunBackground(Scenario background)
        {
            return RunSteps(background.Steps)
                .Select(_ => new BackgroundStepResult(background.Title, _))
                .Cast<StepResult>()
                .ToList();
        }

        private IEnumerable<StepResult> RunSteps(IEnumerable<StringStep> stepsToRun)
        {
            var failedStep = false;
            var stepResults = new List<StepResult>();
            foreach (var step in stepsToRun)
            {
                if (failedStep)
                {
                    step.StepResult = new StepResult(step, new PendingBecauseOfPreviousFailedStep("Previous step has failed"));
                    stepResults.Add(step.StepResult);
                    continue;
                }

                if (step is StringTableStep)
                    step.StepResult = RunStringTableStep((StringTableStep)step);
                else
                    step.StepResult = _stringStepRunner.Run(step);

                if (step.StepResult.Result is Failed)
                {
                    failedStep = true;
                }
                stepResults.Add(step.StepResult);
            }
            return stepResults;
        }

        private void BeforeScenario()
        {
            _stringStepRunner.BeforeScenario();
        }

        private void AfterScenario(Scenario scenario, ScenarioResult scenarioResult)
        {
            if (scenario.Steps.Any())
            {
                try
                {
                    _stringStepRunner.AfterScenario();
                }
                catch (Exception e)
                {
                    if (!scenarioResult.HasFailedSteps())
                        scenarioResult.Fail(e);
                }
            }
        }

        private StepResult RunStringTableStep(StringTableStep stringStep)
        {
            var r = new StringTableStepRunner(_stringStepRunner);
            return r.RunStringTableStep(stringStep);
        }
    }
}