using System;
using System.Collections.Generic;
using System.Text;

namespace NBehave.Narrator.Framework
{
    public class ScenarioResult
    {
        private Result _result = new Passed();
        private string _message;
        private string _stackTrace;
        private readonly List<Result> _actionStepResults;


        public ScenarioResult(string storyTitle, string scenarioTitle)
        {
            StoryTitle = storyTitle;
            ScenarioTitle = scenarioTitle;
            _actionStepResults = new List<Result>();
        }

        public string StoryTitle { get; set; }

        public string ScenarioTitle { get; set; }

        public Result Result
        {
            get { return _result; }
        }

        public string Message
        {
            get { return _message; }
        }

        public string StackTrace
        {
            get { return _stackTrace; }
        }

        public void AddActionStepResult(Result actionStepResult)
        {
            _actionStepResults.Add(actionStepResult);
            if (actionStepResult.GetType() == typeof(Failed))
                _result = actionStepResult;
            if (actionStepResult.GetType() == typeof(Pending) && _result.GetType() != typeof(Failed))
                _result = actionStepResult;
        }

        public IEnumerable<Result> ActionStepResults
        {
            get { return _actionStepResults; }
        }

        public void Fail(Exception exception)
        {
            _message = BuildMessage(exception);
            _stackTrace = BuildStackTrace(exception);
            _result = new Failed(exception);
        }

        public void Pend(string reason)
        {
            if (string.IsNullOrEmpty(_message))
                _message = reason;
            else
                _message += Environment.NewLine + reason;

            _result = new Pending(_message);
        }

        private string BuildMessage(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} : {1}", exception.GetType(), exception.Message);

            Exception inner = exception.InnerException;
            while (inner != null)
            {
                sb.AppendLine();
                sb.AppendFormat("  ----> {0} : {1}", inner.GetType(), inner.Message);
                inner = inner.InnerException;
            }

            return sb.ToString();
        }

        private string BuildStackTrace(Exception exception)
        {
            var builder = new StringBuilder(exception.StackTrace);

            Exception inner = exception.InnerException;
            while (inner != null)
            {
                builder.AppendLine();
                builder.Append("--");
                builder.Append(inner.GetType().Name);
                builder.AppendLine();
                builder.Append(inner.StackTrace);
                inner = inner.InnerException;
            }
            return builder.ToString();
        }
    }
}