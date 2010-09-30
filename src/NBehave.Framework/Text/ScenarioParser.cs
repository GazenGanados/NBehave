using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NBehave.Narrator.Framework
{
    public class ScenarioParser
    {
        private readonly char[] _whiteSpaceChars = new[] { ' ', '\t', '\n', '\r' };

        private ActionStep _actionStep;
        private readonly IStringStepRunner _stringStepRunner;

        public ScenarioParser(IStringStepRunner stringStepRunner)
        {
            _stringStepRunner = stringStepRunner;
        }

        public IEnumerable<Feature> Parse(Stream stream)
        {
            var reader = new StreamReader(stream);
            var scenarioText = reader.ReadToEnd();
            ParseLanguage(scenarioText);
            return ParseScenario(scenarioText);
        }

        private void ParseLanguage(string scenarioText)
        {
            var language = ActionStep.DefaultLanguage;
            var trimmed = scenarioText.TrimStart(_whiteSpaceChars);
            var lang = new Regex(@"^# language:\s+(?<language>\w+)\s+");
            var matches = lang.Match(trimmed);
            if (matches.Success)
                language = matches.Groups["language"].Value;

            _actionStep = new ActionStep(Language.LoadLanguages(), language);
        }

        private IEnumerable<Feature> ParseScenario(string scenarioText)
        {
            var scenarios = new List<ScenarioWithSteps>();
            ScenarioWithSteps scenario = null;
            var feature = new Feature();

            while (scenarioText.Length > 0 && scenarioText.Trim().Length > 0)
            {
                var step = GetNextStep(scenarioText);
                if (step.Length > 0)
                {
                    if (_actionStep.IsFeatureTitle(step))
                        feature = CreateFeature(step);
                    else if (_actionStep.IsScenarioTitle(step))
                    {
                        scenario = CreateNewScenario(scenarios, feature);
                        if (_actionStep.IsScenarioTitle(step))
                            scenario.Title = _actionStep.GetTitle(step);
                    }
                    else if (_actionStep.IsExample(step))
                    {
                        if (scenario == null)
                            scenario = CreateNewScenario(scenarios, feature);
                        AddExample(scenario, step);
                    }
                    else
                    {
                        if (scenario == null)
                            scenario = CreateNewScenario(scenarios, feature);

                        AddScenarioStep(scenario, step);
                    }
                }
                scenarioText = RemoveStep(scenarioText, step);
            }
            
            return scenarios.Select(s => s.Feature)
                            .Distinct();
        }

        private ScenarioWithSteps CreateNewScenario(ICollection<ScenarioWithSteps> scenarios, Feature feature)
        {
            var scenario = new ScenarioWithSteps(_stringStepRunner);
            scenarios.Add(scenario);
            scenario.Feature = feature;
            feature.AddScenario(scenario);
            return scenario;
        }

        private void AddExample(ScenarioWithSteps scenario, string step)
        {
            var examples = ParseTable(step);
            scenario.AddExamples(examples);
        }

        private void AddScenarioStep(ScenarioWithSteps scenario, string step)
        {
            if (HasTable(step))
            {
                var table = ParseTable(step);
                var endOfStep = step.IndexOf('|');
                var stepToMatch = step.Substring(0, endOfStep - 1).TrimEnd(_whiteSpaceChars);
                var theStep = new StringTableStep(stepToMatch, scenario.Source, _stringStepRunner);
                foreach (var row in table)
                {
                    theStep.AddTableStep(new Row(row.ColumnNames, row.ColumnValues));
                }
                scenario.AddStep(theStep);
            }
            else
                scenario.AddStep(step);
        }

        private bool HasTable(string step)
        {
            var hasTable = new Regex(@":\s+\|");
            return hasTable.IsMatch(step);
        }

        private List<Example> ParseTable(string step)
        {
            var columnNames = ReadTableColumnNames(step);
            var tableWithValues = GetTableWithValues(step);
            var rowsWithColumnValues = ReadTableColumnValues(tableWithValues, columnNames);

            var examples = new List<Example>();
            foreach (var rowWithColumnValues in rowsWithColumnValues)
            {
                var example = new Example(columnNames, rowWithColumnValues);
                examples.Add(example);
            }
            return examples;
        }

        private ExampleColumns ReadTableColumnNames(string tableWithColumns)
        {
            var listOfColumnNames = new ExampleColumns();
            var tableHeader = GetTableHeader(tableWithColumns);
            var columnNames = new Regex(@"[^\|]+");
            foreach (Match columnName in columnNames.Matches(tableHeader))
                listOfColumnNames.Add(columnName.Value.Trim().ToLower());
            if (listOfColumnNames.Last() == string.Empty)
            {
                var tmp = listOfColumnNames.Take(listOfColumnNames.Count - 1).ToArray();
                listOfColumnNames.Clear();
                listOfColumnNames.AddRange(tmp);
            }
            return listOfColumnNames;
        }

        private string GetTableHeader(string table)
        {
            var tableHeaderStart = table.IndexOf('|');
            var tableHeaderEnd = table.IndexOf(Environment.NewLine, tableHeaderStart);
            var tableHeader = table.Substring(tableHeaderStart, tableHeaderEnd - tableHeaderStart - 1).Trim();
            return tableHeader;
        }

        private IEnumerable<Dictionary<string, string>> ReadTableColumnValues(string tableWithValues, IList<string> columnNames)
        {
            var columnValues = new List<Dictionary<string, string>>();
            var rowMatch = new Regex(@".+");
            var columnMatch = new Regex(@"[^\|]+");
            while (tableWithValues.Length > 0)
            {
                var row = rowMatch.Match(tableWithValues).Value.Trim();
                if (string.IsNullOrEmpty(row) == false)
                {
                    var columns = columnMatch.Matches(row);

                    var d = new Dictionary<string, string>();
                    for (var i = 0; i < columnNames.Count(); i++)
                    {
                        var value = columns[i].Value;
                        d.Add(columnNames[i], value);
                    }
                    columnValues.Add(d);
                }
                tableWithValues = tableWithValues.Substring(row.Length).Trim();
            }
            return columnValues;
        }

        private string GetTableWithValues(string table)
        {
            var tableNames = GetTableHeader(table);
            var index = table.IndexOf(tableNames) + tableNames.Length;
            var newLineIdx = table.IndexOf(Environment.NewLine, index);
            return table.Substring(newLineIdx).Trim(_whiteSpaceChars);
        }

        private Feature CreateFeature(string step)
        {
            var rows = Split(step);
            var feature = new Feature
                              {
                                  Title = _actionStep.GetTitle(rows.First()),
                                  Narrative = RemoveStep(step, rows[0]).TrimStart(Environment.NewLine.ToCharArray())
                              };
            return feature;
        }

        private string RemoveStep(string scenarioText, string step)
        {
            var idx = scenarioText.IndexOf(step);
            return scenarioText.Remove(0, step.Length + idx);
        }

        private string GetNextStep(string scenario)
        {
            var regexString = BuildRegexString(); //Optimize
            var regex = new Regex(regexString, RegexOptions.IgnoreCase);
            //scenario = scenario.Trim(_whiteSpaceChars);

            var actionRows = RemoveComments(Split(scenario));
            var firstActionWordRow = FindRowForFirstStepWord(actionRows, regex);
            var secondActionWordRow = FindRowForNextStepWord(actionRows, regex, firstActionWordRow + 1);
            string actionRow;
            if (actionRows.Length == 1 && secondActionWordRow == 0 && regex.IsMatch(actionRows[0]))
                actionRow = actionRows[0];
            else
                actionRow = BuildActionStep(actionRows, firstActionWordRow, secondActionWordRow);
            return actionRow;
        }
        
        private string[] RemoveComments(string[] rows)
        {
        	return rows.Where(_=>_.TrimStart().StartsWith("#") == false).ToArray();
        }

        private string BuildActionStep(string[] rows, int startRow, int endRow)
        {
            var actionRow = string.Empty;
            for (var row = startRow; row < endRow; row++)
            {
                actionRow += rows[row] + Environment.NewLine;
            }
            return actionRow.TrimEnd(Environment.NewLine.ToCharArray());
        }

        private int FindRowForFirstStepWord(string[] rows, Regex regex)
        {
            return FindRowForNextStepWord(rows, regex, 0);
        }

        private int FindRowForNextStepWord(string[] rows, Regex regex, int startAtRow)
        {
            var row = startAtRow;
            while (row < rows.Length && regex.IsMatch(rows[row]) == false)
                row++;
            if (row == rows.Length && regex.IsMatch(rows[row - 1]))
                row--;
            return row;
        }

        private string BuildRegexString()
        {
            var regex = @"^\s*(";
            var allWords = _actionStep.AllWords;
            foreach (var alias in allWords)
            {
                regex += alias + "|";
            }
            regex = regex.Substring(0, regex.Length - 1) + ")";
            return regex;
        }

        private string[] Split(string text)
        {
            var sw = WriteTextToStream(text);
            var stream = new StreamReader(sw.BaseStream);
            var rows = new List<string>();
            while (stream.EndOfStream == false)
            {
                var row = stream.ReadLine();
                if (string.IsNullOrEmpty(row.Trim(_whiteSpaceChars)) == false)
                    rows.Add(row);
            }
            return rows.ToArray();
        }

        private StreamWriter WriteTextToStream(string text)
        {
            var sw = new StreamWriter(new MemoryStream());
            sw.Write(text);
            sw.Flush();
            sw.BaseStream.Seek(0, SeekOrigin.Begin);
            return sw;
        }
    }
}