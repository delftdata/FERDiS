using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.Windows;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.UnitTests.Windows
{
    public class SlidingEventWindowTests
    {
        [Test]
        public void Add_DropsEventsThatAreOutOfWindow()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new SlidingEventWindow<TestEvent>(windowSize);
            var testEvent1 = new TestEvent { Key = "key1", EventTime = startTime.AddSeconds(4), Value = 1 };
            var testEvent2 = new TestEvent { Key = "key2", EventTime = startTime.AddSeconds(6), Value = 1 };
            var testEvent3 = new TestEvent { Key = "key3", EventTime = testEvent2.EventTime.Add(windowSize), Value = 1 };
            Assert.IsEmpty(window.Events); //assert empty on start

            window.Add(testEvent1);
            window.Add(testEvent2);
            Assert.AreEqual(2, window.Events.Count);
            window.Add(testEvent3);
            Assert.AreEqual(1, window.Events.Count);
            Assert.AreEqual(window.Events.First().Key, testEvent3.Key);
        }

        [Test]
        public void Add_DoesNotThrowOnDuplicateKey()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new SlidingEventWindow<TestEvent>(windowSize);
            var testEvent = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(5), Value = 1 };

            Assert.IsEmpty(window.Events); //assert empty on start

            window.Add(testEvent);
            window.Add(testEvent);
            window.Add(testEvent);
            window.Add(testEvent);

            Assert.AreEqual(4, window.Events.Count);
        }

        [Test]
        public void Add_DropsDuplicateKeyEntriesCorrectly()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new SlidingEventWindow<TestEvent>(windowSize);
            var testEvent = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(5), Value = 1 };
            var testEvent2 = new TestEvent { Key = "key", EventTime = startTime.Add(windowSize).AddSeconds(5), Value = 1 };
            Assert.IsEmpty(window.Events); //assert empty on start

            window.Add(testEvent);
            window.Add(testEvent);
            window.Add(testEvent);
            window.Add(testEvent);
            Assert.AreEqual(4, window.Events.Count);

            window.Add(testEvent2);
            Assert.AreEqual(1, window.Events.Count);

        }
    }
}
