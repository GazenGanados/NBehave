﻿using NUnit.Framework;

namespace NBehave.Spec.NUnit
{
	public abstract class SpecBase<T> : Spec.SpecBase<T>
	{
		[SetUp]
		public override void MainSetup()
		{
			base.MainSetup();
		}

		[TearDown]
		public override void MainTeardown()
		{
			base.MainTeardown();
		}
	}
}
