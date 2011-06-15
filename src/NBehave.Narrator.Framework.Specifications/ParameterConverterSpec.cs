using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace NBehave.Narrator.Framework.Specifications
{
    [TestFixture]
    public class ParameterConverterSpec
    {
        private ParameterConverter _parameterConverter;
        private ActionCatalog _actionCatalog;

        [SetUp]
        public void EstablishContext()
        {
            _actionCatalog = new ActionCatalog();
            _parameterConverter = new ParameterConverter(_actionCatalog);
        }

        [TestFixture]
        public class WhenFetchingParametersForActionStep : ParameterConverterSpec
        {
            [Test]
            public void ShouldGetParameterForActionWithTokenInMiddleOfString()
            {
                Action<int> action = accountBalance => { };

                _actionCatalog.Add(new ActionMethodInfo("I have $amount euros on my cash account".AsRegex(), action, action.Method, null));
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText("I have 20 euros on my cash account", ""));

                Assert.That(values.Length, Is.EqualTo(1));
                Assert.That(values[0].GetType(), Is.EqualTo(typeof(int)));
            }

            [Test]
            public void ShouldGetParameterForActionIfTokenHasNewlines()
            {
                Action<string> action = someAction => { };
                _actionCatalog.Add(new ActionMethodInfo("I have a board like this\n$board".AsRegex(), action, action.Method, null));
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText("I have a board like this\nxo \n x \no x", ""));

                Assert.That(values.Length, Is.EqualTo(1));
                Assert.That(values[0], Is.EqualTo("xo \n x \no x"));
            }

            [Test]
            public void ShouldGetParametersForMessageWithActionRegisteredTwice()
            {
                Action<string> action = someAction => { };
                _actionCatalog.Add(new ActionMethodInfo("Given $value something".AsRegex(), action, action.Method, null));
                _actionCatalog.Add(new ActionMethodInfo("And $value something".AsRegex(), action, action.Method, null));
                var givenValue = _parameterConverter.GetParametersForActionStepText(new ActionStepText("Given 20 something", ""));
                var andValue = _parameterConverter.GetParametersForActionStepText(new ActionStepText("And 20 something", ""));

                Assert.That(givenValue.Length, Is.EqualTo(1));
                Assert.That(andValue.Length, Is.EqualTo(1));
            }

            [Test]
            public void ShouldGetParametersForMessageWithANegativeParameter()
            {
                Action<string> action = someAction => { };
                _actionCatalog.Add(new ActionMethodInfo("Given $value something".AsRegex(), action, action.Method, null));
                var givenValue = _parameterConverter.GetParametersForActionStepText(new ActionStepText("Given -20 something", ""));

                Assert.That(givenValue.Length, Is.EqualTo(1));
                Assert.That(givenValue.First(), Is.EqualTo("-20"));
            }

            [Test]
            public void ShouldGetIntParameter()
            {
                Action<int> action = value => { };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"an int (?<value>\d+)"), action, action.Method, "Given"));
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText("an int 42", ""));
                Assert.That(values[0], Is.TypeOf(typeof(int)));
            }

            [Test]
            public void ShouldGetDecimalParameter()
            {
                Action<decimal> action = value => { };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a decimal (?<value>\d+)"), action, action.Method, "Given"));
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText("a decimal 42", ""));
                Assert.That(values[0], Is.TypeOf(typeof(decimal)));
            }

            [Test]
            public void ShouldGetEnumParameter()
            {
                Action<AttributeTargets> action = value => { };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"an enum (?<value>\w+)"), action, action.Method, "Given"));
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText("an enum Assembly", ""));
                Assert.That(values[0], Is.TypeOf(typeof(AttributeTargets)));
            }

            [Test]
            public void ShouldGetMultilineValueAsString()
            {
                Action<object> action = value => { };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a string\s+(?<value>(\w+\s+)*)"), action, action.Method, "Given"));
                var multiLineValue = "one" + Environment.NewLine + "two";
                var actionString = "a string " + multiLineValue;
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText(actionString, ""));
                Assert.That(values[0], Is.TypeOf(typeof(string)));
            }

            [Test]
            public void ShouldGetMultilineValueAsArrayOfStrings()
            {
                //                object paramReceived = null;
                Action<string[]> actionStep = p => { };
                //                Action<object> action = value => { paramReceived = value; };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a string\s+(?<value>(\w+,?\s*)+)"), actionStep, actionStep.Method, "Given"));
                const string multiLineValue = "one, two";
                var actionString = "a string " + Environment.NewLine + multiLineValue;
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText(actionString, ""));
                Assert.That(values[0], Is.TypeOf(typeof(string[])));
                var arr = (string[])values[0];
                Assert.AreEqual("one", arr[0]);
                Assert.AreEqual("two", arr[1]);
            }

            [Test]
            public void ShouldRemoveEmptyEntriesAtEndOfArrayValues()
            {
                Action<string[]> action = value => { };

                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a string\s+(?<value>(\w+,?\s*)+)"), action, action.Method, "Given"));
                var multiLineValue = "one,two," + Environment.NewLine;
                var actionString = "a string " + Environment.NewLine + multiLineValue;
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText(actionString, ""));
                Assert.That((values[0] as string[]), Is.EqualTo(new[] { "one", "two" }));
            }

            [Test]
            public void ShouldGetMultilineValueAsArrayOfIntegers()
            {
                ShouldGetMultilineValueAsGenericCollectionOfIntegers<int[]>();
            }

            [Test]
            public void ShouldGetMultilineValueAsGenericIEnumerableOfIntegers()
            {
                ShouldGetMultilineValueAsGenericCollectionOfIntegers<IEnumerable<int>>();
            }

            [Test]
            public void ShouldGetMultilineValueAsGenericICollectionOfIntegers()
            {
                ShouldGetMultilineValueAsGenericCollectionOfIntegers<ICollection<int>>();
            }

            [Test]
            public void ShouldGetMultilineValueAsGenericIListOfIntegers()
            {
                ShouldGetMultilineValueAsGenericCollectionOfIntegers<IList<int>>();
            }

            [Test]
            public void ShouldGetMultilineValueAsGenericListOfIntegers()
            {
                ShouldGetMultilineValueAsGenericCollectionOfIntegers<List<int>>();
            }

            public void ShouldGetMultilineValueAsGenericCollectionOfIntegers<T>() where T : IEnumerable<int>
            {
                //                object paramReceived = null;
                Action<T> actionStep = p => { };
                //                Action<object> action = value => { paramReceived = value; };
                _actionCatalog.Add(new ActionMethodInfo(new Regex(@"a list of integers (?<value>(\d+,?\s*)+)"), actionStep, actionStep.Method, "Given"));
                const string multiLineValue = "1, 2, 5";
                const string actionString = "a list of integers " + multiLineValue;
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText(actionString, ""));
                Assert.That(values[0], Is.AssignableTo(typeof(T)));
                var arr = (T)values[0];
                Assert.AreEqual(1, arr.First());
                Assert.AreEqual(2, arr.Skip(1).First());
                Assert.AreEqual(5, arr.Last());
            }
        }

        [TestFixture]
        public class WhenFetchingParametersWithRowValue : ParameterConverterSpec
        {
            [Test]
            public void ShouldGetParameterForActionWithTokenInMiddleOfString()
            {
                Action<string> action = name => { };
                _actionCatalog.Add(new ActionMethodInfo("I have a name".AsRegex(), action, action.Method, null));
                var row = new Row(new ExampleColumns(new[] { new ExampleColumn("name") }), new Dictionary<string, string> { { "name", "Morgan" } });
                var values = _parameterConverter.GetParametersForActionStepText(new ActionStepText("I have a name", ""), row);

                Assert.That(values.Length, Is.EqualTo(1));
                Assert.That(values[0].GetType(), Is.EqualTo(typeof(string)));
            }
        }
    }
}