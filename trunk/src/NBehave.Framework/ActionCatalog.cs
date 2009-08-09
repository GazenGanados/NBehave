﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NBehave.Narrator.Framework
{
    public class ActionValue : ActionMatch
    {
        public object Action { get; set; }
    }

    public class ActionMatch
    {
        public Regex ActionStepMatcher { get; set; }

        public List<string> GetParameterNames()
        {
            var names = new List<string>();
            int index = 0;
            string name = ".";
            Regex regex = ActionStepMatcher;
            while (string.IsNullOrEmpty(name) == false)
            {
                name = regex.GroupNameFromNumber(index);
                if (string.IsNullOrEmpty(name) == false && name != index.ToString())
                    names.Add(name);
                index++;
            }
            return names;
        }
    }

    public class ActionCatalog
    {
        public const char TokenPrefix = '$';

        private readonly List<ActionValue> _actions = new List<ActionValue>();

        [Obsolete("Use Add(Regex actionMatch, object action)")]
        public void Add(string tokenString, object action)
        {
            if (ActionExists(tokenString))
                return;
            var regex = GetRegexForActionKey(tokenString);
            Add(regex, action);
        }

        public void Add(Regex actionMatch, object action)
        {
            _actions.Add(new ActionValue { ActionStepMatcher = actionMatch, Action = action });
        }

        public bool ActionExists(string message)
        {
            return (FindMathingAction(message) != null);
        }

        public string BuildFormatString(string message, ICollection<object> args)
        {
            if ((message.IndexOf(ActionCatalog.TokenPrefix) == -1))
            {
                if (args.Count == 0)
                    return "{0} {1}";
                if (args.Count == 1)
                    return "{0} {1}: {2}";
                string formatString = "{0} {1}: (";
                for (int i = 0; i < args.Count; i++)
                    formatString += "{" + (i + 2) + "}, ";
                return formatString.Remove(formatString.Length - 2) + ")";
            }
            return "{0} {1}";
        }

        public object[] GetParametersForMessage(string message)
        {
            ActionValue action = GetAction(message);
            List<string> paramNames = GetParameterNames(action);
            Type[] args = action.Action.GetType().GetGenericArguments();
            var values = new object[args.Length];

            Match match = action.ActionStepMatcher.Match(message);
            for (int argNumber = 0; argNumber < paramNames.Count(); argNumber++)
            {
                var strParam = match.Groups[paramNames[argNumber]].Value;
                values[argNumber] = Convert.ChangeType(strParam, args[argNumber]); //converts string to an instance of args[argNumber]
            }
            return values;
        }

        public ActionValue GetAction(string message)
        {
            return FindMathingAction(message);
        }

        public string BuildMessage(string message, object[] parameters)
        {
            string resultString = message;
            string[] tokens = GetTokensInMessage(message);
            if (tokens.Length > 0 && tokens.Length != parameters.Length)
                throw new ArgumentException(string.Format("message has {0} tokens and there are {1} parameters", tokens.Length, parameters.Length));
            for (int i = 0; i < tokens.Length; i++)
            {
                resultString = resultString.Replace(tokens[i], parameters[i].ToString());
            }

            return resultString;
        }

        private ActionValue FindMathingAction(string message)
        {
            foreach (var action in _actions)
            {
                Regex regex = action.ActionStepMatcher;
                bool isMatch = regex.IsMatch(message);
                if (isMatch)
                    return action;
            }
            return null;
        }

        private List<string> GetParameterNames(ActionValue actionValue)
        {
            return actionValue.GetParameterNames();
        }

        private string[] GetTokensInMessage(string message)
        {
            var tokens = new List<string>();

            var matches = Regex.Matches(message, @"\$[a-zA-Z]+");
            foreach (var match in matches)
            {
                tokens.Add(match.ToString());
            }
            return tokens.ToArray();
        }

        private IEnumerable<Regex> GetRegexForAction(object action)
        {
            return from r in _actions
                   where r.Action.Equals(action)
                   select r.ActionStepMatcher;
        }

        private Regex GetRegexForActionKey(string actionKey)
        {
            return actionKey.AsRegex();
        }
    }
}
