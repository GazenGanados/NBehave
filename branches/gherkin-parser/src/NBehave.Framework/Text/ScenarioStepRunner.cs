﻿using System;
using System.Collections.Generic;

namespace NBehave.Narrator.Framework
{
    public class ScenarioStepRunner
    {
        private Feature _lastFeature;

        public static event EventHandler<EventArgs<ScenarioResult>> ScenarioResultCreated;

        public IEnumerable<ScenarioResult> Run(IEnumerable<ScenarioWithSteps> scenarios)
        {
            var allResults = new List<ScenarioResult>();

            foreach (var scenario in scenarios)
            {
                if (scenario.Feature != _lastFeature)
                    NewFeature(scenario);
                var scenarioResults = scenario.Run();
                RaiseFeatureResultsEvent(scenarioResults);
                allResults.Add(scenarioResults);
            }
            return allResults;
        }

        private void NewFeature(ScenarioWithSteps scenario)
        {
            _lastFeature = scenario.Feature;
            _lastFeature.RaiseFeatureCreated();
        }

        private void RaiseFeatureResultsEvent(ScenarioResult scenarioResult)
        {
            if (ScenarioResultCreated == null)
                return;
            var e = new EventArgs<ScenarioResult>(scenarioResult);
            ScenarioResultCreated.Invoke(this, e);
        }
    }
}