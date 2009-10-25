using System.Text.RegularExpressions;

namespace NBehave.Narrator.Framework
{
    public class WhenAttribute : ActionStepAttribute
    {
        public WhenAttribute()
        {
            Type = "When";            
        }

        private WhenAttribute(Regex actionMatch)
            : base(actionMatch)
        {
            Type = "When";
        }

        public WhenAttribute(string regex)
            : this(new Regex(regex))
        { }
    }
}