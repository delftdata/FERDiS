using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.Windows;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.UnitTests.Windows
{
    public class FixedEventWindowTests
    {

        [Test]
        public void Add_ReturnsWindowOnReceivingAdvancedWaterMark()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new FixedEventWindow<TestEvent>(startTime, windowSize);
            var testEvent = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(5), Value = 1 };
            Assert.IsEmpty(window.Events); //assert empty on start
            
            window.Insert(testEvent);
            Assert.AreEqual(1, window.Events.Count);
            
            testEvent.EventTime = startTime.AddSeconds(7);
            var addResult = window.Insert(testEvent);
            Assert.IsEmpty(addResult);
            Assert.AreEqual(2, window.Events.Count);
            
            //insert event in new window so it returns the previous window
            testEvent.EventTime = startTime.Add(windowSize);
            addResult = window.Insert(testEvent);
            Assert.AreEqual(2, addResult.Count());
            Assert.AreEqual(1, window.Events.Count);
        }

        [Test]
        public void Add_ReturnsWindowOnReceivingAdvancedWaterMarkMultipleTimes()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new FixedEventWindow<TestEvent>(startTime, windowSize);
            var testEvent1 = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(2), Value = 1 };
            var testEvent2 = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(5), Value = 1 };
            var testEvent3 = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(7), Value = 1 };
            
            Assert.IsEmpty(window.Events); //assert empty on start

            window.Insert(testEvent1);
            window.Insert(testEvent2);
            window.Insert(testEvent3);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(3, window.Events.Count);
                testEvent1.EventTime = testEvent1.EventTime.Add(windowSize);
                testEvent2.EventTime = testEvent2.EventTime.Add(windowSize);
                testEvent3.EventTime = testEvent3.EventTime.Add(windowSize);
                Assert.AreEqual(3, window.Insert(testEvent1).Count()); //this is in a new window
                window.Insert(testEvent2);
                window.Insert(testEvent3);
            }
        }

        [Test]
        public void Add_DoesNotThrowOnDuplicateKey()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new FixedEventWindow<TestEvent>(startTime, windowSize);
            var testEvent = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(5), Value = 1 };

            Assert.IsEmpty(window.Events); //assert empty on start

            window.Insert(testEvent);
            window.Insert(testEvent);
            window.Insert(testEvent);
            window.Insert(testEvent);

            Assert.AreEqual(4, window.Events.Count);
        }

        [Test]
        public void Add_ReturnsWindowCorrectlyWithDuplicateKeys()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new FixedEventWindow<TestEvent>(startTime, windowSize);
            var testEvent = new TestEvent { Key = "key", EventTime = startTime.AddSeconds(5), Value = 1 };
            var testEvent2 = new TestEvent { Key = "key", EventTime = startTime.Add(windowSize).AddSeconds(5), Value = 1 };

            Assert.IsEmpty(window.Events); //assert empty on start

            window.Insert(testEvent);
            window.Insert(testEvent);
            window.Insert(testEvent);
            window.Insert(testEvent);
            Assert.AreEqual(4, window.Events.Count); //4 events in window

            var closedWindow = window.Insert(testEvent2); //now we add event which is in next window
            Assert.IsNotEmpty(closedWindow);
            Assert.AreEqual(4, closedWindow.Count()); //previous 4 events in closed window
            Assert.AreEqual(1, window.Events.Count); //now only testEvent2 in window

        }
    }
}
