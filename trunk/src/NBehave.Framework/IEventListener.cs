using System;
using System.Collections.Generic;
using System.Text;

namespace NBehave.Narrator.Framework
{
    public interface IEventListener
    {
        void StoryCreated();
        void StoryMessageAdded(string message);
        void RunStarted();
        void RunFinished();
        void ThemeStarted(string name);
        void ThemeFinished();
    }
}