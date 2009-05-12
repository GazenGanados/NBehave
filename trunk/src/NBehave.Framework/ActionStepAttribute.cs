using System;
using System.Text.RegularExpressions;

namespace NBehave.Narrator.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionStepAttribute : Attribute
    {
        public string TokenString { get; private set; }

        public ActionStepAttribute(string tokenString)
        {
            TokenString = tokenString;
        }
    }

    public class ScenarioAttribute : ActionStepAttribute
    {
        public ScenarioAttribute(string tokenString)
            : base(tokenString)
        {
        }

    }
}