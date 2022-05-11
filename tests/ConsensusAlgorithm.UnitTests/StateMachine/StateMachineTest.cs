using ConsensusAlgorithm.Core.StateMachine;
using NUnit.Framework;

namespace ConsensusAlgorithm.UnitTests.StateMachineTest
{
    [TestFixture]
    public class StateMachineTest
    {
        [Test]
        public void TestStateMachineRequestStatus()
        {
            var testSM = new DictionaryStateMachine();
            Assert.AreEqual(string.Empty, testSM.RequestStatus(string.Empty));
            Assert.AreEqual(string.Empty, testSM.RequestStatus("random"));
        }

        [Test]
        public void TestStateMachineIgnoresInvalidRequest()
        {
            var testSM = new DictionaryStateMachine();
            Assert.AreEqual(string.Empty, testSM.RequestStatus(string.Empty));
            testSM.Apply("bad request");
            testSM.Apply("CLEAR X X");
            testSM.Apply("SET X X");
            Assert.AreEqual(string.Empty, testSM.RequestStatus("X"));
        }

        [Test]
        public void TestStateMachineAppliesValidRequest()
        {
            var testSM = new DictionaryStateMachine();

            Assert.AreEqual(string.Empty, testSM.RequestStatus("X"));

            testSM.Apply("SET X 0");
            Assert.AreEqual("0", testSM.RequestStatus("X"));
            Assert.AreEqual(string.Empty, testSM.RequestStatus("x"));
            testSM.Apply("SET X 20");
            Assert.AreEqual("20", testSM.RequestStatus("X"));
            testSM.Apply("CLEAR x");
            Assert.AreEqual("20", testSM.RequestStatus("X"));
            testSM.Apply("clear X");
            Assert.AreEqual(string.Empty, testSM.RequestStatus("X"));
            testSM.Apply("set x 5");
            Assert.AreEqual("5", testSM.RequestStatus("x"));
            Assert.AreEqual(string.Empty, testSM.RequestStatus("X"));
        }
    }
}