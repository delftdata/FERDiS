using BlackSP.Checkpointing.UnitTests.Models;
using Moq;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests
{

    public class MessageLoggingServiceTests
    {

        MessageLoggingService<TestMessage> _testService;

        [SetUp]
        public void SetUp()
        {
            var loggerMock = new Mock<ILogger>();
            _testService = new MessageLoggingService<TestMessage>(loggerMock.Object);
        } 

        [Test]
        public void Initialize_Yields_Correct_Datastructures()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            
            //cannot receive from downstream..
            Assert.IsFalse(_testService.ReceivedSequenceNumbers.ContainsKey("d1"));
            Assert.IsFalse(_testService.ReceivedSequenceNumbers.ContainsKey("d2"));

            //must be able to receive from upstream
            Assert.IsTrue(_testService.ReceivedSequenceNumbers.ContainsKey("u1"));
            Assert.IsTrue(_testService.ReceivedSequenceNumbers.ContainsKey("u2"));

            Assert.AreEqual(-1, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.AreEqual(-1, _testService.ReceivedSequenceNumbers["u2"]);
        }

        [Test]
        public void Receive_Accepts_NextSequenceNumber()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(-1, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.IsTrue(_testService.Receive("u1", 0));
            Assert.AreEqual(0, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.IsTrue(_testService.Receive("u1", 1));
            Assert.AreEqual(1, _testService.ReceivedSequenceNumbers["u1"]);
        }

        [Test]
        public void Receive_Rejects_OutOfOrder_SequenceNumber()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(-1, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.IsTrue(_testService.Receive("u1", 0));
            Assert.AreEqual(0, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.IsTrue(_testService.Receive("u1", 1));
            Assert.AreEqual(1, _testService.ReceivedSequenceNumbers["u1"]);
            
            //next expected is 2..
            Assert.IsFalse(_testService.Receive("u1", 0)); 
            Assert.IsFalse(_testService.Receive("u1", 1)); 
            Assert.IsFalse(_testService.Receive("u1", 3));

            //ensure seqnr was not affected
            Assert.AreEqual(1, _testService.ReceivedSequenceNumbers["u1"]);
        }

        [Test]
        public void Receive_UpdatesSequenceNumbersPerInstance()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.IsTrue(_testService.Receive("u1", 0));
            Assert.IsTrue(_testService.Receive("u1", 1));
            Assert.IsTrue(_testService.Receive("u1", 2));
            
            Assert.AreEqual(2, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.AreEqual(-1, _testService.ReceivedSequenceNumbers["u2"]);

            Assert.IsFalse(_testService.Receive("u2", 1)); //next seq nr for u1 should not work for u2

            Assert.IsTrue(_testService.Receive("u2", 0));
            Assert.IsTrue(_testService.Receive("u2", 1));
            Assert.IsTrue(_testService.Receive("u2", 2));
            Assert.IsTrue(_testService.Receive("u2", 3));

            Assert.AreEqual(2, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.AreEqual(3, _testService.ReceivedSequenceNumbers["u2"]);
        }

        [Test]
        public void ExpectReplay_Drops_OutOfSequenceMessages_UntilReplay_Completed()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.IsFalse(_testService.Receive("u1", 1)); //expects 0 first
            Assert.IsTrue(_testService.Receive("u1", 0));
            Assert.IsTrue(_testService.Receive("u1", 1));
            Assert.IsTrue(_testService.Receive("u1", 2));
            Assert.IsTrue(_testService.Receive("u1", 3));

            _testService.ExpectReplay("u1", 0); //Replay from 0 --> first accepted message by receive should be 0+1=1

            Assert.IsFalse(_testService.Receive("u1", 4));
            Assert.IsFalse(_testService.Receive("u1", 5));
            Assert.IsFalse(_testService.Receive("u1", 6));
            //so far messages have been dropped

            //replay messages arrive
            Assert.IsTrue(_testService.Receive("u1", 1));
            Assert.IsTrue(_testService.Receive("u1", 2));
            Assert.IsTrue(_testService.Receive("u1", 3));
            //and continuation is now also accepted
            Assert.IsTrue(_testService.Receive("u1", 4));
            Assert.IsTrue(_testService.Receive("u1", 5));
            Assert.IsTrue(_testService.Receive("u1", 6));
        }

        [Test]
        public void CyclicConnection_DoesNotInterfere_With_Receive()
        {
            //u1 is both up and downstream
            _testService.Initialize(new[] { "u1", "d2" }, new[] { "u1", "u2" });

            Assert.IsTrue(_testService.Receive("u1", 0));
            Assert.IsTrue(_testService.Receive("u1", 1));
            Assert.IsTrue(_testService.Receive("u1", 2));

            Assert.AreEqual(2, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.AreEqual(-1, _testService.ReceivedSequenceNumbers["u2"]);

            Assert.IsFalse(_testService.Receive("u2", 1)); //next seq nr for u1 should not work for u2

            Assert.IsTrue(_testService.Receive("u2", 0));
            Assert.IsTrue(_testService.Receive("u2", 1));
            Assert.IsTrue(_testService.Receive("u2", 2));
            Assert.IsTrue(_testService.Receive("u2", 3));

            Assert.AreEqual(2, _testService.ReceivedSequenceNumbers["u1"]);
            Assert.AreEqual(3, _testService.ReceivedSequenceNumbers["u2"]);
        }

        [Test]
        public void CyclicConnection_DoesNotInterfere_With_Append()
        {
            _testService.Initialize(new[] { "u1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("u1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("u1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("u1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("u1", new TestMessage()));
            Assert.AreEqual(4, _testService.Append("u1", new TestMessage()));

            Assert.AreEqual(new[] { 2, 3, 4 }, _testService.Replay("u1", 2).Select(p => p.Item1));
            Assert.AreEqual(new[] { 1, 2, 3, 4 }, _testService.Replay("u1", 1).Select(p => p.Item1));
            Assert.AreEqual(new int[0], _testService.Replay("u1", 5).Select(p => p.Item1));
        }

        [Test]
        public void Append_Returns_Gapless_SequenceNumbers()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("d1", new TestMessage()));
        }

        [Test]
        public void Append_ReturnsGaplessSequenceNumbers_ForEveryInstance()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("d1", new TestMessage()));
            
            Assert.AreEqual(0, _testService.Append("d2", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d2", new TestMessage()));

            Assert.AreEqual(4, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d2", new TestMessage()));
        }

        [Test]
        public void Replay_Returns_Enumerable_With_CorrectMessages()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(4, _testService.Append("d1", new TestMessage()));

            Assert.AreEqual(new[] { 2,3,4 }, _testService.Replay("d1", 2).Select(p => p.Item1));
            Assert.AreEqual(new[] { 1,2,3,4 }, _testService.Replay("d1", 1).Select(p => p.Item1)); 
            Assert.AreEqual(new int[0], _testService.Replay("d1", 5).Select(p => p.Item1));
        }

        [Test]
        public void Prune_Removes_MessagesBeforeSequenceNr()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(4, _testService.Append("d1", new TestMessage()));

            Assert.AreEqual(new[] { 2, 3, 4 }, _testService.Replay("d1", 2).Select(p => p.Item1).ToArray());
            
            Assert.AreEqual(3, _testService.Prune("d1", 2)); //prunes messages 0,1,2 (count = 3)
            Assert.AreEqual(new[] { 3, 4 }, _testService.Replay("d1", 2).Select(p => p.Item1).ToArray());
            
            Assert.AreEqual(0, _testService.Prune("d1", 2)); //prunes no messages (count = 0)
            Assert.AreEqual(new[] { 3, 4 }, _testService.Replay("d1", 2).Select(p => p.Item1).ToArray());

            Assert.AreEqual(2, _testService.Prune("d1", 5)); //prunes all remaining messages (count = 2 )
            Assert.AreEqual(new int[] { }, _testService.Replay("d1", 0).Select(p => p.Item1).ToArray()); //proof that no message can be replayed anymore, even from the start
        }


        [Test]
        public void Prune_Emptying_Log_DoesNot_ChangeSequenceNumbers()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("d1", new TestMessage()));

            //pruning removes all 4 messages
            Assert.AreEqual(4, _testService.Prune("d1", 3));
            
            //but next message seqnr is still valid
            Assert.AreEqual(4, _testService.Append("d1", new TestMessage()));
        }

        [Test]
        public void Prune_MultiCall_DoesNot_MultiPrune()
        {
            _testService.Initialize(new[] { "d1", "d2" }, new[] { "u1", "u2" });

            Assert.AreEqual(0, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(1, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(2, _testService.Append("d1", new TestMessage()));
            Assert.AreEqual(3, _testService.Append("d1", new TestMessage()));

            //pruning removes all 4 messages
            Assert.AreEqual(2, _testService.Prune("d1", 1));
            Assert.AreEqual(0, _testService.Prune("d1", 1));
            Assert.AreEqual(2, _testService.Prune("d1", 3));

            //but next message seqnr is still valid
            Assert.AreEqual(4, _testService.Append("d1", new TestMessage()));
        }
    }
}
