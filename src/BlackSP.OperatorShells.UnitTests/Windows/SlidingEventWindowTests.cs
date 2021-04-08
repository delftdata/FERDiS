using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.Windows;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Windows
{
    public class SlidingEventWindowTests
    {
        [Test]
        public async Task Add_DropsEventsThatAreOutOfWindow()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(2);
            var windowSlidingSize = TimeSpan.FromSeconds(1);
            var window = new SlidingEventWindow<TestEvent>(startTime.AddMilliseconds(-1), windowSize, windowSlidingSize);
            var testEvent1 = new TestEvent { Key = 1, Value = 1 };
            var testEvent2 = new TestEvent { Key = 2, Value = 1 };
            var testEvent3 = new TestEvent { Key = 3, Value = 1 };
            var testEvent4 = new TestEvent { Key = 4, Value = 1 };
            Assert.IsEmpty(window.Events); //assert empty on start

            bool didAdvance;
            var windowBeforeSlide = window.Insert(testEvent1, startTime, out didAdvance); //first event arrives
            Assert.IsEmpty(windowBeforeSlide); //no resulting events unless window slides

            windowBeforeSlide = window.Insert(testEvent2, startTime.Add(windowSlidingSize), out didAdvance); //second one arrives
            Assert.AreEqual(new TestEvent[] { testEvent1, testEvent2 }, window.Events);
            Assert.IsEmpty(windowBeforeSlide); //window didnt slide yet

            windowBeforeSlide = window.Insert(testEvent3, startTime.Add(windowSize), out didAdvance); //third arrives and window slides
            Assert.AreEqual(new TestEvent[] { testEvent1, testEvent2 }, windowBeforeSlide);
            Assert.AreEqual(new TestEvent[] { testEvent2, testEvent3 }, window.Events);

            windowBeforeSlide = window.Insert(testEvent4, startTime.Add(2*windowSize), out didAdvance); //fourth arrives and window slides two steps ahead
            Assert.AreEqual(new TestEvent[] { testEvent2, testEvent3 }, windowBeforeSlide);
            Assert.AreEqual(new TestEvent[] { testEvent4 }, window.Events);
        }

        [Test]
        public void Add_DoesNotThrowOnDuplicateKey()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(10);
            var window = new SlidingEventWindow<TestEvent>(startTime.AddMilliseconds(-1), windowSize, windowSize/2);
            var testEvent = new TestEvent { Key = 0, Value = 1 };

            Assert.IsEmpty(window.Events); //assert empty on start
            bool didAdvance;

            window.Insert(testEvent, startTime, out didAdvance);
            window.Insert(testEvent, startTime.AddMilliseconds(1), out didAdvance);
            window.Insert(testEvent, startTime.AddMilliseconds(2), out didAdvance);
            window.Insert(testEvent, startTime.AddMilliseconds(3), out didAdvance);

            Assert.AreEqual(4, window.Events.Count);
        }

        [Test]
        public async Task Add_DropsDuplicateKeyEntriesCorrectly()
        {
            var startTime = DateTime.Now;
            var windowSize = TimeSpan.FromSeconds(1);
            var windowSlidingSize = windowSize / 2;
            var window = new SlidingEventWindow<TestEvent>(startTime.AddMilliseconds(-1), windowSize, windowSlidingSize);
            var testEvent = new TestEvent { Key = 0, Value = 1 };
            var testEvent2 = new TestEvent { Key = 0, Value = 1 };
            Assert.IsEmpty(window.Events); //assert empty on start
            bool didAdvance;

            window.Insert(testEvent, startTime, out didAdvance);
            window.Insert(testEvent, startTime.AddMilliseconds(1), out didAdvance);
            window.Insert(testEvent, startTime.AddMilliseconds(2), out didAdvance);
            var windowBeforeSlide = window.Insert(testEvent, startTime.AddMilliseconds(700), out didAdvance);
            Assert.IsEmpty(windowBeforeSlide);
            Assert.AreEqual(4, window.Events.Count);
            //next insert causes sliding, leaving only the last event (of the last 4) and the new event
            windowBeforeSlide = window.Insert(testEvent2, startTime.AddSeconds(1), out didAdvance);
            Assert.AreEqual(4, windowBeforeSlide.Count());
            Assert.AreEqual(2, window.Events.Count);

        }
    }
}
