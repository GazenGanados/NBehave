﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextRunner.cs" company="NBehave">
//   Copyright (c) 2007, NBehave - http://nbehave.codeplex.com/license
// </copyright>
// <summary>
//   Defines the TextRunner type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NBehave.Narrator.Framework
{
    using NBehave.Narrator.Framework.Messages;
    using NBehave.Narrator.Framework.Tiny;

    using TinyIoC;

    public class TextRunner
    {
        private readonly NBehaveConfiguration _configuration;
        private ITinyMessengerHub _hub;

        public TextRunner(NBehaveConfiguration configuration)
        {
            _configuration = configuration;
        }

        public FeatureResults Run()
        {
            var container = TinyIoCContainer.Current;
            NBehaveInitialiser.Initialise(container, _configuration);
            _hub = container.Resolve<ITinyMessengerHub>();

            FeatureResults results = null;
            _hub.Subscribe<FeatureResults>(featureResults => results = featureResults);
            
            try
            {
                this._hub.Publish(new RunStartedEvent(this));
            }
            finally
            {
                this._hub.Publish(new RunFinishedEvent(this));
            }

            container.Dispose();
            return results;
        }
    }
}
