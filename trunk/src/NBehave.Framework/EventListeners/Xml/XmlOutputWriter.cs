using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace NBehave.Narrator.Framework.EventListeners.Xml
{
    public class XmlOutputWriter
    {
        private XmlWriter Writer { get; set; }
        private IList<EventReceived> EventsReceived { get; set; }

        public XmlOutputWriter(XmlWriter xmlWriter, IList<EventReceived> eventsReceived)
        {
            Writer = xmlWriter;
            EventsReceived = eventsReceived;
        }

        public void WriteAllXml()
        {
            var evt = (from e in EventsReceived
                       where e.EventType == EventType.RunStart
                       select e).First();

            Writer.WriteStartElement("results");
            string[] assemblyString = typeof(XmlOutputEventListener).AssemblyQualifiedName.Split(new[] { ',' });
            Writer.WriteAttributeString("name", assemblyString[1]);
            Writer.WriteAttributeString("version", assemblyString[2]);
            Writer.WriteAttributeString("date", evt.Time.ToShortDateString());
            Writer.WriteAttributeString("time", evt.Time.ToShortTimeString());
            Writer.WriteAttributeString("themes", CountThemes().ToString());
            Writer.WriteAttributeString("stories", CountStories().ToString());

            Writer.WriteAttributeString("scenarios", CountScenarios().ToString());
            Writer.WriteAttributeString("scenariosFailed", CountFailingScenarios().ToString());
            Writer.WriteAttributeString("scenariosPending", CountPendingScenarios().ToString());

            foreach (var e in EventsReceived.Where(e => e.EventType == EventType.ThemeStarted))
                DoTheme(e);

            DoRunFinished();
        }

        public void DoTheme(EventReceived evt)
        {
            var events = EventsOf(evt, EventType.ThemeFinished);
            string themeTitle = evt.Message;
            WriteStartElement("theme", themeTitle, events.Last().Time.Subtract(events.First().Time));
            Writer.WriteAttributeString("stories", events.Where(e => e.EventType == EventType.StoryCreated).Count().ToString());
            Writer.WriteAttributeString("scenarios", events.Where(e => e.EventType == EventType.ScenarioCreated).Count().ToString());
            Writer.WriteAttributeString("scenariosFailed", CountFailingScenarios(events).ToString());
            Writer.WriteAttributeString("scenariosPending", CountPendingScenarios(events).ToString());
            Writer.WriteStartElement("stories");
            foreach (var e in events.Where(x => x.EventType == EventType.StoryCreated))
                DoStory(themeTitle, e);
            Writer.WriteEndElement();
            Writer.WriteEndElement();
        }

        public void DoStory(string theme, EventReceived evt)
        {
            var events = EventsOf(evt, EventType.StoryResult);
            string storyTitle = evt.Message;
            WriteStartElement("story", storyTitle, events.Last().Time.Subtract(events.First().Time));
            IEnumerable<ScenarioResult> scenarioResultsForStory = GetScenarioResultsForStory(storyTitle, events);

            WriteStoryDataAttributes(scenarioResultsForStory);
            WriteStoryNarrative(events);
            Writer.WriteStartElement("scenarios");
            foreach (var e in events.Where(evts => evts.EventType == EventType.ScenarioCreated))
            {
                var scenarioTitle = e.Message;
                var scenarioResult = (from r in scenarioResultsForStory
                                      where r.ScenarioTitle == scenarioTitle
                                            && r.StoryTitle == storyTitle
                                      select r).FirstOrDefault();

                DoScenario(e, scenarioResult);
            }
            Writer.WriteEndElement();
            Writer.WriteEndElement();
        }

        void WriteStoryNarrative(IEnumerable<EventReceived> events)
        {
            var storyMessages = from m in events
                                where m.EventType == EventType.StoryMessage
                                select m.Message;
            if (storyMessages.Count() > 0)
            {
                Writer.WriteStartElement("narrative");
                foreach (var row in storyMessages)
                    Writer.WriteString(row + Environment.NewLine);
                Writer.WriteEndElement();
            }
        }

        void WriteStoryDataAttributes(IEnumerable<ScenarioResult> scenarioResultsForStory)
        {
            int totalScenariosFailed = (from f in scenarioResultsForStory
                                        where f.Result.GetType() == typeof(Failed)
                                        select f).Count();
            int totalScenariosPending = (from f in scenarioResultsForStory
                                         where f.Result.GetType() == typeof(Pending)
                                         select f).Count();
            Writer.WriteAttributeString("scenarios", scenarioResultsForStory.Count().ToString());
            Writer.WriteAttributeString("scenariosFailed", totalScenariosFailed.ToString());
            Writer.WriteAttributeString("scenariosPending", totalScenariosPending.ToString());
        }

        IEnumerable<ScenarioResult> GetScenarioResultsForStory(string storyTitle, IEnumerable<EventReceived> eventsReceived)
        {
            var storyResults = (from e in eventsReceived
                                where e.EventType == EventType.StoryResult
                                select e as StoryResultsEventReceived).FirstOrDefault();
            var scenarioResultsForStory = from e in storyResults.StoryResults.ScenarioResults
                                          where e.StoryTitle == storyTitle
                                          select e;
            return scenarioResultsForStory;
        }

        public void DoScenario(EventReceived evt, ScenarioResult scenarioResult)
        {
            var events = from e in EventsOf(evt, EventType.ScenarioCreated)
                         where e.EventType == EventType.ScenarioMessage
                         select e;
            WriteStartElement("scenario", evt.Message, events.Last().Time.Subtract(events.First().Time));

            Writer.WriteAttributeString("outcome", scenarioResult.Result.ToString());
            if (IsPendingAndNoActionStepsResults(scenarioResult))
                CreatePendingSteps(evt, scenarioResult);
            foreach (var step in scenarioResult.ActionStepResults)
                DoActionStep(step);
            Writer.WriteEndElement();
        }

        private bool IsPendingAndNoActionStepsResults(ScenarioResult scenarioResult)
        {
            return scenarioResult.Result.GetType() == typeof(Pending) && (scenarioResult.ActionStepResults.Count() == 0);
        }

        private void CreatePendingSteps(EventReceived evt, ScenarioResult scenarioResult)
        {
            var actionSteps = from e in EventsOf(evt, EventType.StoryResult)
                              where e.EventType == EventType.ScenarioMessage
                              select e;
            foreach (var step in actionSteps)
                scenarioResult.AddActionStepResult(new ActionStepResult(step.Message, new Pending(scenarioResult.Message)));
        }

        public void DoActionStep(ActionStepResult result)
        {
            Writer.WriteStartElement("actionStep");
            Writer.WriteAttributeString("name", result.ActionStep);
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

        private int CountThemes()
        {
            return CountEventsOfType(EventType.ThemeStarted);
        }

        private int CountStories()
        {
            return CountEventsOfType(EventType.StoryCreated);
        }

        private int CountScenarios()
        {
            var storyResults = GetScenarioResults(EventsReceived, p => true);
            return storyResults.Count();
        }

        private int CountFailingScenarios()
        {
            return CountFailingScenarios(EventsReceived);
        }

        private int CountFailingScenarios(IEnumerable<EventReceived> events)
        {
            var scenarioResults = GetScenarioResults(events, s => s.GetType() == typeof(Failed));
            return scenarioResults.Count();
        }

        private bool HasScenario(IEnumerable<EventReceived> eventsToUse, string scenarioTitle)
        {
            var scenario = from s in eventsToUse
                           where s.Message == scenarioTitle
                           select s;
            return scenario.Count() > 0;
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

        private List<EventReceived> EventsOf(EventReceived startEvent, EventType endWithEvent)
        {
            int idxStart = EventsReceived.IndexOf(startEvent);
            int idxEnd = idxStart;
            var events = new List<EventReceived>();
            do
            {
                events.Add(EventsReceived[idxEnd]);
                idxEnd++;
            } while (idxEnd < EventsReceived.Count && EventsReceived[idxEnd].EventType != endWithEvent);
            if (idxEnd < EventsReceived.Count)
                events.Add(EventsReceived[idxEnd]);

            return events;
        }

        private void WriteStartElement(string elementName, string attributeName, TimeSpan timeTaken)
        {
            Writer.WriteStartElement(elementName);
            Writer.WriteAttributeString("name", attributeName);
            Writer.WriteAttributeString("time", timeTaken.TotalSeconds.ToString());
        }

        private IEnumerable<ScenarioResult> GetScenarioResults(IEnumerable<EventReceived> events, Predicate<Result> scenarioResult)
        {
            var storyResults = (from e in events
                                where e.EventType == EventType.StoryResult
                                select e as StoryResultsEventReceived).FirstOrDefault();
            if (storyResults != null)
            {
                var eventsToUse = events.Where(e => e.EventType == EventType.ScenarioCreated);
                IEnumerable<ScenarioResult> sr = from s in storyResults.StoryResults.ScenarioResults
                                                 where HasScenario(eventsToUse, s.ScenarioTitle)
                                                 && scenarioResult(s.Result)
                                                 select s;
                return sr;
            }
            return new List<ScenarioResult>();
        }
    }
}