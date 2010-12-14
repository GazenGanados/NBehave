﻿namespace NBehave.Narrator.Framework.Processors
{
    using System.Collections.Generic;
    using System.Linq;

    using NBehave.Narrator.Framework.Tiny;

    class InlineStepBuilder : IModelBuilder
    {
        private readonly ITinyMessengerHub _hub;
        private Scenario _scenario;
        private readonly Stack<ParsedStep> _lastStep = new Stack<ParsedStep>();

        public InlineStepBuilder(ITinyMessengerHub hub)
        {
            _hub = hub;

            _hub.Subscribe<ScenarioBuilt>(built => _scenario = built.Content);
            _hub.Subscribe<ParsedStep>(message => _lastStep.Push(message));

            _hub.Subscribe<ITinyMessage>(
                anyMessage =>
                    {
                        if (anyMessage is ParsedTable)
                        {
                            ExtractInlineTableStepsFromTable(anyMessage);
                        }
                    },
                _lastStep.Any());
        }

        private void ExtractInlineTableStepsFromTable(ITinyMessage anyMessage)
        {
            var stringTableStep = new StringTableStep(this._lastStep.Pop().Content, this._scenario.Source);
            this._scenario.AddStep(stringTableStep);

            foreach (var list in ((ParsedTable)anyMessage).Content)
            {
                var exampleColumns = new ExampleColumns(list.Select(token => token.Content.ToLower()));

                var example = list.Select(token => token.Content);

                var row = new Dictionary<string, string>();

                for (int i = 0; i < example.Count(); i++)
                {
                    row.Add(exampleColumns[i], example.ElementAt(i));
                }

                stringTableStep.AddTableStep(new Row(exampleColumns, row));
            }
        }
    }
}