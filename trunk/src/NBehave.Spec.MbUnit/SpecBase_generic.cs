﻿using MbUnit.Framework;

namespace NBehave.Spec.MbUnit
{
	public abstract class SpecBase<T> : Spec.SpecBase<T>
	{
		[SetUp]
		public override void SpecSetup()
		{
			base.SpecSetup();
		}

		[TearDown]
		public override void SpecTeardown()
		{
			base.SpecTeardown();
		}
	}
}
