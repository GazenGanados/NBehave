﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using NBehave.Narrator.Framework;
using System.IO;
using System.Xml;


namespace NBehave.MSBuild
{
    public class NBehaveTask : Task
    {
        public bool DryRun { get; set; }

        public string StoryOutputPath { get; set; }

        [Required]
        public string[] TestAssemblies { get; set; }

        public bool FailBuild { get; set; }


        public StoryResults StoryResults { get; private set; }


        public NBehaveTask()
        {
            DryRun = false;
            FailBuild = true;
        }

        public override bool Execute()
        {
            if (TestAssemblies.Length == 0)
                throw new ArgumentException("At least one test assembly is required");

            StringBuilder logString = new StringBuilder();
            TextWriter logWriter = new StringWriter(logString);
            PlainTextOutput output = new PlainTextOutput(logWriter);

            WriteHeaderInto(output);

            StoryRunner runner = new StoryRunner();
            runner.IsDryRun = DryRun;

            foreach (string path in TestAssemblies)
            {
                runner.LoadAssembly(path);
            }

            StoryResults = runner.Run(CreateEventListenerUsing(logWriter));

            if (DryRun)
                return true;

            WriteResultsInto(output, StoryResults);
            Log.LogMessage(logString.ToString());

            if (FailBuild && FailBuildBasedOn(StoryResults))
                return false;

            return true;
        }

        private IEventListener CreateEventListenerUsing(TextWriter nantLogWriter)
        {
            XmlTextWriter writer = new XmlTextWriter(StoryOutputPath, Encoding.UTF8);
            return new NBehave.Narrator.Framework.EventListeners.Xml.XmlOutputEventListener(writer);
        }

        private void WriteHeaderInto(PlainTextOutput output)
        {
            output.WriteHeader();
            output.WriteSeparator();
            output.WriteRuntimeEnvironment();
            output.WriteSeparator();
        }

        private void WriteResultsInto(PlainTextOutput output, StoryResults results)
        {
            output.WriteDotResults(results);
            output.WriteSummaryResults(results);
            output.WriteFailures(results);
            output.WritePending(results);
        }

        private bool FailBuildBasedOn(StoryResults results)
        {
            if (results.NumberOfFailingScenarios == 0)
                return false;

            var exceptionMessage = new StringBuilder();
            foreach (ScenarioResults result in results.ScenarioResults)
            {
                exceptionMessage.AppendLine(result.Message);
                exceptionMessage.AppendLine(result.StackTrace);
                exceptionMessage.AppendLine();
            }

            Log.LogError(exceptionMessage.ToString());
            return true;
        }       
    }




}
