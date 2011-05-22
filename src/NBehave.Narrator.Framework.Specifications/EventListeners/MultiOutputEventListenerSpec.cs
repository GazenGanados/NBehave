using NBehave.Narrator.Framework.EventListeners;
using NUnit.Framework;
using Rhino.Mocks;

namespace NBehave.Narrator.Framework.Specifications.EventListeners
{
    [TestFixture]
    public class MultiOutputEventListenerSpec
    {
        [Test]
        public void ShouldInvokeMethodOnAllSpecifiedListeners()
        {
            var mockFirstEventListener = MockRepository.GenerateMock<EventListener>();
            var mockSecondEventListener = MockRepository.GenerateMock<EventListener>();

            EventListener listener = new MultiOutputEventListener(mockFirstEventListener, mockSecondEventListener);
            listener.RunStarted();
            mockFirstEventListener.AssertWasCalled(l => l.RunStarted());
            mockSecondEventListener.AssertWasCalled(l => l.RunStarted());
        }
    }
}
