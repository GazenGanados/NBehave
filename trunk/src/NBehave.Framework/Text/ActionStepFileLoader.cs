﻿using System.Collections.Generic;
using System.IO;

namespace NBehave.Narrator.Framework
{
    public class ActionStepFileLoader
    {
        private readonly StringStepRunner _stringStepRunner;
        private readonly IEventListener _listener;

        public ActionStepFileLoader(StringStepRunner stringStepRunner, IEventListener listener)
        {
            _stringStepRunner = stringStepRunner;
            _listener = listener;
        }

        public List<List<ScenarioWithSteps>> Load(IEnumerable<string> scenarioLocations)
        {
            var stories = new List<List<ScenarioWithSteps>>();

            foreach (var location in scenarioLocations)
            {
                string[] files = GetFiles(location);
                stories.AddRange(LoadFiles(files));
            }
            return stories;
        }

        private string[] GetFiles(string location)
        {
            string[] files;
            if (Path.IsPathRooted(location))
                files = Directory.GetFiles(Path.GetDirectoryName(location), Path.GetFileName(location));
            else
                files = Directory.GetFiles(".", location);
            return files;
        }

        private List<List<ScenarioWithSteps>> LoadFiles(IEnumerable<string> files)
        {
            var stories = new List<List<ScenarioWithSteps>>();
            foreach (var file in files)
            {
                List<ScenarioWithSteps> scenarios = GetScenarios(file);
                stories.Add(scenarios);
            }
            return stories;
        }

        private List<ScenarioWithSteps> GetScenarios(string file)
        {
            List<ScenarioWithSteps> scenarios;
            using (Stream stream = File.OpenRead(file))
            {
                scenarios = Load(stream);
                foreach (var scenario in scenarios)
                    scenario.FileName = file;
            }
            return scenarios;
        }

        public List<ScenarioWithSteps> Load(Stream stream)
        {
            var scenarioTextParser = new ScenarioParser(_stringStepRunner);
            List<ScenarioWithSteps> steps = scenarioTextParser.Parse(stream);
            return steps;
        }
    }
}
