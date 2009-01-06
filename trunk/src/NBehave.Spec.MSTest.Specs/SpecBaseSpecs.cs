﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Rhino.Mocks.Exceptions;
using Context = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Specification = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
using NBehave.Spec.MSTest;

namespace MSTest.SpecBase_Specifications
{
	[Context]
	public class When_initializing_the_SpecBase : SpecBase<StopWatch>
	{
		protected override StopWatch Establish_context()
		{
			return new StopWatch();
		}

		protected override void Because_of()
		{
		}

		[Specification]
		public void should_populate_the_SUT_before_starting_the_specification()
		{
			Sut.ShouldNotBeNull();
		}
	}

	[Context]
	public class When_initializing_the_SpecBase_with_mocks : SpecBase<StopWatch>
	{
		private ITimer _timer;

		protected override StopWatch Establish_context()
		{
			_timer = CreateDependency<ITimer>();

			_timer.Stub(x => x.Start(null)).IgnoreArguments().Return(true);

			return new StopWatch(_timer);
		}

		protected override void Because_of()
		{
			Sut.Start();
		}

		[Specification]
		public void should_call_the_before_each_spec_before_starting_the_specification()
		{
			_timer.AssertWasCalled(x => x.Start(null), opt => opt.IgnoreArguments());
		}
	}

    public class StopWatch
    {
        private readonly ITimer _timer;

        public StopWatch()
        {
        }

        public StopWatch(ITimer timer)
        {
            _timer = timer;
        }

        public void Start()
        {
            _timer.Start("");
        }
    }

    public interface ITimer
    {
        bool Start(string reason);
        void Start();
    }
}
