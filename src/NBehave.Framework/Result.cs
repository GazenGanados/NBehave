using System;

namespace NBehave.Narrator.Framework
{
    [Serializable]
    public class ActionStepResult : Result
    {
        public string StringStep { get; private set; }
        public Result Result { get; private set; }

        public ActionStepResult(string stringStep, Result resultForActionStep)
            : base(resultForActionStep.Message)
        {
            StringStep = stringStep;
            Result = resultForActionStep;
        }


        public void MergeResult(Result stepResult)
        {
            if (stepResult is Passed)
                return;
            if (stepResult is Pending && Result is Passed)
            {
                Result = stepResult;
                Message = stepResult.Message;
            }
            if (stepResult is Failed && (Result is Passed || Result is Pending))
            {
                Result = stepResult;
                Message = stepResult.Message;
            }
            if (Result == null)
            {
                Result = stepResult;
                Message = stepResult.Message;
            }
        }

        public override string ToString()
        {
            return Result.ToString();
        }
    }

    [Serializable]
    public class Passed : Result
    {
        public Passed()
            : base(string.Empty)
        {
        }
    }

    [Serializable]
    public class Failed : Result
    {
        public Exception Exception { get; private set; }
        public Failed(Exception exception)
            : base(exception.ToString())
        {
            Exception = exception;
        }
    }

    [Serializable]
    public class Pending : Result
    {
        public Pending(string pendingReason)
            : base(pendingReason)
        {
        }
    }

    [Serializable]
    public abstract class Result : MarshalByRefObject
    {
        public string Message { get; protected set; }

        protected Result(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return GetType().Name.Replace(typeof(Result).Name, "").ToLower();
        }
    }
}