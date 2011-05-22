﻿namespace NBehave.Narrator.Framework.Processors
{
    using System.Collections.Generic;
    using System.Linq;

    using Gherkin;

    using NBehave.Narrator.Framework.Tiny;

    class InlineStepBuilder : AbstracModelBuilder
    {
        private readonly ITinyMessengerHub _hub;
        private Scenario _scenario;
        private readonly Queue<ParsedStep> _lastStep = new Queue<ParsedStep>();

        public InlineStepBuilder(ITinyMessengerHub hub)
            : base(hub)
        {
            _hub = hub;

            _hub.Subscribe<ScenarioBuilt>(built => _scenario = built.Content);
            _hub.Subscribe<ParsedStep>(message => this._lastStep.Enqueue(message));

            _hub.Subscribe<ITinyMessage>(
                anyMessage =>
                    {
                        if (anyMessage is ParsedTable && anyMessage != _lastStep.Peek())
                        {
                            ExtractInlineTableStepsFromTable(anyMessage);
                        }
                        else
                        {
                            _lastStep.Dequeue();
                        }
                    },
                tinyMessage => _lastStep.Any());
        }

        private void ExtractInlineTableStepsFromTable(ITinyMessage anyMessage)
        {
            var stringTableStep = new StringTableStep(this._lastStep.Dequeue().Content, this._scenario.Source);
            this._scenario.AddStep(stringTableStep);

            IList<IList<Token>> content = ((ParsedTable)anyMessage).Content;
            
            var exampleColumns = new ExampleColumns(content.First().Select(token => token.Content.ToLower()));

            foreach (var list in content.Skip(1))
            {
                var example = list.Select(token => token.Content);

                var row = new Dictionary<string, string>();

                for (int i = 0; i < example.Count(); i++)
                {
                    row.Add(exampleColumns[i], example.ElementAt(i));
                }

                stringTableStep.AddTableStep(new Row(exampleColumns, row));
            }
        }

        public override void Cleanup()
        {
            _scenario = null;
            _lastStep.Clear();
        }
    }
}