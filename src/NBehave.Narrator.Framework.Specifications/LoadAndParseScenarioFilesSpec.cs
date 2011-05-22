using System.Collections.Generic;
using NBehave.Narrator.Framework.Specifications.Features;
using NUnit.Framework;

namespace NBehave.Narrator.Framework.Specifications
{
    using global::System.Linq;

    using NBehave.Narrator.Framework.Contracts;
    using NBehave.Narrator.Framework.Messages;
    using NBehave.Narrator.Framework.Processors;
    using NBehave.Narrator.Framework.Tiny;

    using Rhino.Mocks;

    [TestFixture]
    public class LoadAndParseScenarioFilesSpec
    {
        private LoadScenarioFiles _loadScenarioFiles;
        private NBehaveConfiguration _config;
        private ITinyMessengerHub _hub;
        private ParseScenarioFiles _parseScenarioFiles;
        private IEnumerable<Feature> _features;

        private void CreateLoaderAndParser()
        {
            TinyIoCContainer.Current.Register<ITinyMessengerHub, TinyMessengerHub>();
            TinyIoCContainer.Current.RegisterMany<IModelBuilder>().AsSingleton();
            TinyIoCContainer.Current.Resolve<IEnumerable<IModelBuilder>>();

            this._hub = TinyIoCContainer.Current.Resolve<ITinyMessengerHub>();
            this._loadScenarioFiles = new LoadScenarioFiles(this._config, this._hub);
            this._parseScenarioFiles = new ParseScenarioFiles(this._hub);
            this._hub.Subscribe<FeaturesLoaded>(loaded => this._features = loaded.Content);
            this._loadScenarioFiles.Initialise();
        }

        [Test]
        public void ShouldBeAbleToUseRelativePathsWithDots()
        {
            this._config = NBehaveConfiguration.New.SetScenarioFiles(new[]
            {
                @"..\*.*"
            });

            CreateLoaderAndParser();

            Assert.That(_features, Is.Not.Null);
        }
    }
}