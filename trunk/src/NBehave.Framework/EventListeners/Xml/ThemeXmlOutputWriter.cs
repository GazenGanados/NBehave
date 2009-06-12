﻿using System;
using System.Collections.Generic;
using System.Xml;


namespace NBehave.Narrator.Framework.EventListeners.Xml
{
    public class ThemeXmlOutputWriter : XmlOutputBase
    {
        private Timer _currentThemeTimer;
        public int TotalStories { get; set; }

        public ThemeXmlOutputWriter(XmlWriter writer, Queue<Action> actions, StoryResults resultsAlreadyDone)
            : base(writer, actions, resultsAlreadyDone)
        { }

        public void ThemeStarted(string name)
        {
            _currentThemeTimer = new Timer();
            var themeTimer = _currentThemeTimer; // so we have a reference to the correct theme when the code actually executes
            Actions.Enqueue(
                () =>
                {
                    WriteStartElement("theme", name, themeTimer);
                    Writer.WriteAttributeString("stories", TotalStories.ToString());
                    WriteScenarioResult();
                });
        }

        public void ThemeFinished()
        {
            _currentThemeTimer.Stop();
            Actions.Enqueue(
               () => Writer.WriteEndElement()); // </theme>
        }
    }
}
