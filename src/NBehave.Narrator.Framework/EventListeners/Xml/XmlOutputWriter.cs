// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlOutputWriter.cs" company="NBehave">
//   Copyright (c) 2007, NBehave - http://nbehave.codeplex.com/license
// </copyright>
// <summary>
//   Defines the XmlOutputWriter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NBehave.Narrator.Framework.EventListeners.Xml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    public class XmlOutputWriter
    {
        public XmlOutputWriter(XmlWriter xmlWriter, IList<EventReceived> eventsReceived)
        {
            Writer = xmlWriter;
            EventsReceived = eventsReceived;
        }

        private XmlWriter Writer { get; set; }

        private IList<EventReceived> EventsReceived { get; set; }

        public void WriteAllXml()
        {
            var evt = (from e in this.EventsReceived
                       where e.EventType == EventType.RunStart
                       select e).First();

            Writer.WriteStartElement("results");
            var assemblyString = typeof(XmlOutputEventListener).AssemblyQualifiedName.Split(new[] { ',' });
            Writer.WriteAttributeString("name", assemblyString[1]);
            Writer.WriteAttributeString("version", assemblyString[2]);
            Writer.WriteAttributeString("date", evt.Time.ToShortDateString());
            Writer.WriteAttributeString("time", evt.Time.ToShortTimeString());
            Writer.WriteAttributeString("themes", this.CountThemes().ToString());
            Writer.WriteAttributeString("stories", this.CountStories().ToString());

            Writer.WriteAttributeString("scenarios", this.CountScenarios().ToString());
            Writer.WriteAttributeString("scenariosFailed", this.CountFailingScenarios().ToString());
            Writer.WriteAttributeString("scenariosPending", this.CountPendingScenarios().ToString());

            foreach (var e in this.EventsReceived.Where(e => e.EventType == EventType.ThemeStarted))
                DoTheme(e);

            DoRunFinished();
        }

        public void DoTheme(EventReceived evt)
        {
            var events = EventsOf(evt, EventType.ThemeFinished);
            var themeTitle = evt.Message;
            WriteStartElement("theme", themeTitle, events.Last().Time.Subtract(events.First().Time));
            Writer.WriteAttributeString("stories", events.Where(e => e.EventType == EventType.FeatureCreated).Count().ToString());
            Writer.WriteAttributeString("scenarios", events.Where(e => e.EventType == EventType.ScenarioCreated).Count().ToString());
            Writer.WriteAttributeString("scenariosFailed", CountFailingScenarios(events).ToString());
            Writer.WriteAttributeString("scenariosPending", this.CountPendingScenarios(events).ToString());
            Writer.WriteStartElement("stories");
            foreach (var e in events.Where(x => x.EventType == EventType.FeatureCreated))
            {
                DoStory(themeTitle, e);
            }

            Writer.WriteEndElement();
            Writer.WriteEndElement();
        }

        public void DoStory(string theme, EventReceived evt)
        {
            var events = EventsOf(evt, EventType.FeatureCreated);
            var featureTitle = evt.Message;
            WriteStartElement("story", featureTitle, events.Last().Time.Subtract(events.First().Time));
            var scenarioResultsForFeature = GetScenarioResultsForFeature(featureTitle, events);

            WriteStoryDataAttributes(scenarioResultsForFeature);
            WriteStoryNarrative(events);
            Writer.WriteStartElement("scenarios");
            foreach (var e in events.Where(evts => evts.EventType == EventType.ScenarioCreated))
            {
                var scenarioTitle = e.Message;
                var scenarioResult = (from r in scenarioResultsForFeature
                                      where r.ScenarioTitle == scenarioTitle
                                            && r.FeatureTitle == featureTitle
                                      select r).FirstOrDefault();
                if (scenarioResult != null)
                    DoScenario(e, scenarioResult);
            }

            Writer.WriteEndElement();
            Writer.WriteEndElement();
        }

        public void DoScenario(EventReceived evt, ScenarioResult scenarioResult)
        {
            var events = from e in EventsOf(evt, EventType.ScenarioCreated)
                         where e.EventType == EventType.ScenarioCreated
                         select e;
            WriteStartElement("scenario", evt.Message, events.Last().Time.Subtract(events.First().Time));

            Writer.WriteAttributeString("outcome", scenarioResult.Result.ToString());

            if (IsPendingAndNoActionStepsResults(scenarioResult))
                CreatePendingSteps(evt, scenarioResult);

            foreach (var step in scenarioResult.StepResults)
                DoActionStep(step);

            DoExamplesInScenario(scenarioResult as ScenarioExampleResult);
            Writer.WriteEndElement();
        }

        public void DoActionStep(StepResult result)
        {
            Writer.WriteStartElement("actionStep");
            Writer.WriteAttributeString("name", result.StringStep.Step);
            Writer.WriteAttributeString("outcome", result.Result.ToString());
            if (result.Result.GetType() == typeof(Failed))
                Writer.WriteElementString("failure", result.Message);

            Writer.WriteEndElement();
        }

        private void DoRunFinished()
        {
            Writer.WriteEndElement(); // </results>
            Writer.Flush();
        }

        private void DoExamplesInScenario(ScenarioExampleResult scenarioExampleResult)
        {
            if (scenarioExampleResult == null)
            {
                return;
            }

            Writer.WriteStartElement("examples");
            Writer.WriteStartElement("columnNames");

            foreach (var columnName in scenarioExampleResult.Examples.First().ColumnNames)
            {
                Writer.WriteStartElement("columnName");
                Writer.WriteString(columnName.Name);
                Writer.WriteEndElement();
            }

            Writer.WriteEndElement();

            var scenarioResults = scenarioExampleResult.ExampleResults.ToArray();
            var idx = 0;
            foreach (var example in scenarioExampleResult.Examples)
            {
                Writer.WriteStartElement("example");
                Writer.WriteStartAttribute("outcome");
                Writer.WriteString(scenarioResults[idx++].Result.ToString());
                Writer.WriteEndAttribute();
                foreach (var columnName in example.ColumnNames)
                {
                    Writer.WriteStartElement("column");
                    Writer.WriteStartAttribute("columnName");
                    Writer.WriteString(columnName.Name);
                    Writer.WriteEndAttribute();
                    Writer.WriteString(example.ColumnValues[columnName.Name]);
                    Writer.WriteEndElement();
                }

                Writer.WriteEndElement();
            }

            Writer.WriteEndElement();
        }

        private IEnumerable<ScenarioResult> GetScenarioResultsForFeature(string featureTitle, IEnumerable<EventReceived> eventsReceived)
        {
            var featureResults = from e in eventsReceived
                                 where e.EventType == EventType.ScenarioResult
                                 select e as ScenarioResultEventReceived;
            var scenarioResultsForFeature = from e in featureResults
                                            where e.ScenarioResult.FeatureTitle == featureTitle
                                            select e.ScenarioResult;
            return scenarioResultsForFeature;
        }

        private IEnumerable<ScenarioResult> GetScenarioResults(IEnumerable<EventReceived> events, Predicate<Result> scenarioResult)
        {
            var storyResults = from e in events
                               where e.EventType == EventType.ScenarioResult
                               select e as ScenarioResultEventReceived;
            var eventsToUse = events.Where(e => e.EventType == EventType.ScenarioCreated);
            var sr = from s in storyResults
                     where HasScenario(eventsToUse, s.ScenarioResult.ScenarioTitle)
                           && scenarioResult(s.ScenarioResult.Result)
                     select s.ScenarioResult;
            return sr;
        }

        private bool IsPendingAndNoActionStepsResults(ScenarioResult scenarioResult)
        {
            return scenarioResult.Result.GetType() == typeof(Pending) && (scenarioResult.StepResults.Count() == 0);
        }

        private void WriteStoryNarrative(IEnumerable<EventReceived> events)
        {
            var featureMessages = from m in events
                                  where m.EventType == EventType.FeatureNarrative
                                  select m.Message;
            if (featureMessages.Count() > 0)
            {
                Writer.WriteStartElement("narrative");
                foreach (var row in featureMessages)
                    Writer.WriteString(row + Environment.NewLine);

                Writer.WriteEndElement();
            }
        }

        private void WriteStoryDataAttributes(IEnumerable<ScenarioResult> scenarioResultsForFeature)
        {
            var totalScenariosFailed = (from f in scenarioResultsForFeature
                                        where f.Result.GetType() == typeof(Failed)
                                        select f).Count();
            var totalScenariosPending = (from f in scenarioResultsForFeature
                                         where f.Result.GetType() == typeof(Pending)
                                         select f).Count();
            Writer.WriteAttributeString("scenarios", scenarioResultsForFeature.Count().ToString());
            Writer.WriteAttributeString("scenariosFailed", totalScenariosFailed.ToString());
            Writer.WriteAttributeString("scenariosPending", totalScenariosPending.ToString());
        }

        private void WriteStartElement(string elementName, string attributeName, TimeSpan timeTaken)
        {
            Writer.WriteStartElement(elementName);
            Writer.WriteAttributeString("name", attributeName);
            Writer.WriteAttributeString("time", timeTaken.TotalSeconds.ToString());
        }

        private void CreatePendingSteps(EventReceived evt, ScenarioResult scenarioResult)
        {
            var actionSteps = from e in EventsOf(evt, EventType.ScenarioResult)
                              where e.EventType == EventType.ScenarioCreated
                              select e;
            foreach (var step in actionSteps)
                scenarioResult.AddActionStepResult(new StepResult(new StringStep(step.Message, "lost it"), new Pending(scenarioResult.Message)));
        }

        private int CountThemes()
        {
            return CountEventsOfType(EventType.ThemeStarted);
        }

        private int CountStories()
        {
            return CountEventsOfType(EventType.FeatureCreated);
        }

        private int CountScenarios()
        {
            var storyResults = GetScenarioResults(this.EventsReceived, p => true);
            return storyResults.Count();
        }

        private int CountFailingScenarios()
        {
            return CountFailingScenarios(this.EventsReceived);
        }

        private int CountFailingScenarios(IEnumerable<EventReceived> events)
        {
            var scenarioResults = GetScenarioResults(events, s => s.GetType() == typeof(Failed));
            return scenarioResults.Count();
        }

        private int CountPendingScenarios()
        {
            return CountPendingScenarios(EventsReceived);
        }

        private int CountPendingScenarios(IEnumerable<EventReceived> events)
        {
            var scenarioResults = GetScenarioResults(events, s => s.GetType() == typeof(Pending));

            return scenarioResults.Count();
        }

        private int CountEventsOfType(EventType eventType)
        {
            var themes = from e in EventsReceived
                         where e.EventType == eventType
                         select e;
            return themes.Count();
        }

        private bool HasScenario(IEnumerable<EventReceived> eventsToUse, string scenarioTitle)
        {
            var scenario = from s in eventsToUse
                           where s.Message == scenarioTitle
                           select s;
            return scenario.Count() > 0;
        }

        private IEnumerable<EventReceived> EventsOf(EventReceived startEvent, EventType endWithEvent)
        {
            var idxStart = EventsReceived.IndexOf(startEvent);
            var idxEnd = idxStart;
            var events = new List<EventReceived>();
            do
            {
                events.Add(EventsReceived[idxEnd]);
                idxEnd++;
            }
            while (idxEnd < EventsReceived.Count && EventsReceived[idxEnd].EventType != endWithEvent);
            if (idxEnd < EventsReceived.Count)
            {
                events.Add(EventsReceived[idxEnd]);
            }

            return events;
        }
    }
}