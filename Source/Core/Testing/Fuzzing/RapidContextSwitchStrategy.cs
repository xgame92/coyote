// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class RapidContextSwitchStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        /// <summary>
        /// Dictionary to keep a track of delay per thread.
        /// </summary>
        private readonly Dictionary<int, AutoResetEvent> PerTaskARE;

        private readonly Dictionary<int, Thread> ThreadIdToHandle;

        private readonly SemaphoreSlim syncObj;

        /// <summary>
        /// Initializes a new instance of the <see cref="RapidContextSwitchStrategy"/> class.
        /// </summary>
        internal RapidContextSwitchStrategy(IRandomValueGenerator random, int maxDelays)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PerTaskARE = new Dictionary<int, AutoResetEvent>();
            this.ThreadIdToHandle = new Dictionary<int, Thread>();
            this.syncObj = new SemaphoreSlim(1);
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.PerTaskARE.Clear();
            this.ThreadIdToHandle.Clear();
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            this.StepCount++;
            next = 0;
            return true;
        }

        // This method isn't race-free. Protect with a lock.
        internal void ShouldBlockThread(ILogger logger)
        {
            _ = logger;
            this.syncObj.Wait();
            bool isFirstTime = false;

            // Assuming one-to-one mapping between Tasks and Threads.
            int? currentThreadId = Thread.CurrentThread.ManagedThreadId;

            if (currentThreadId is null)
            {
                this.syncObj.Release();
                return;
            }

            // If we are encountering this thread for the first time.
            if (!this.PerTaskARE.ContainsKey((int)currentThreadId))
            {
                Console.WriteLine("<ScheduleDebug> Creating new Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                // Every thread should be associate with an AutoResetEvent object.
                AutoResetEvent temp = new AutoResetEvent(false);
                this.PerTaskARE.Add((int)currentThreadId, temp);
                // Store the handle of this thread.
                this.ThreadIdToHandle.Add((int)currentThreadId, Thread.CurrentThread);
                isFirstTime = true;
            }

            if (this.PerTaskARE.TryGetValue((int)currentThreadId, out AutoResetEvent are) && !isFirstTime)
            {
                // Find next thread to schedule.
                // No other thread to schedule.
                if (this.ThreadIdToHandle.Count < 2)
                {
                    this.syncObj.Release();
                    return;
                }

                bool unBlockedAnotherThread = false;
                foreach (var index in this.Shuffle(Enumerable.Range(0, this.ThreadIdToHandle.Count - 1)))
                {
                    KeyValuePair<int, Thread> nextThread = this.ThreadIdToHandle.ElementAt<KeyValuePair<int, Thread>>(index);

                    // Continue if this Thead is block/suspended/stopped.
                    if (nextThread.Key == (int)currentThreadId || (nextThread.Value.ThreadState != ThreadState.WaitSleepJoin && nextThread.Value.ThreadState != ThreadState.Background))
                    {
                        continue;
                    }

                    // Retrieve ARE object of this Thread.
                    if (this.PerTaskARE.TryGetValue(nextThread.Key, out AutoResetEvent temp))
                    {
                        Console.WriteLine("<ScheduleDebug> Unblocking thread: {0}. Current thread is {1}", nextThread.Key, Thread.CurrentThread.ManagedThreadId);
                        // Unblock this thread.
                        temp.Set();
                        unBlockedAnotherThread = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                this.syncObj.Release();
                // Release the global lock and block.
                if (unBlockedAnotherThread)
                {
                    Console.WriteLine("<ScheduleDebug> Blocking current thread: {0}.", Thread.CurrentThread.ManagedThreadId);
                    are.Reset();
                    are.WaitOne(TimeSpan.FromMilliseconds(300));
                }
            }
            else if (isFirstTime && this.ThreadIdToHandle.Count > 1)
            {
                // Release the global lock and block.
                this.syncObj.Release();
                Console.WriteLine("<ScheduleDebug> Blocking current thread: {0}.", Thread.CurrentThread.ManagedThreadId);
                are.Reset();
                are.WaitOne(TimeSpan.FromMilliseconds(300));
            }
            else
            {
                this.syncObj.Release();
            }
        }

        /// <summary>
        /// Shuffles the specified range using the Fisher-Yates algorithm.
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle.
        /// </remarks>
        private IList<int> Shuffle(IEnumerable<int> range)
        {
            var result = new List<int>(range);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = this.RandomValueGenerator.Next(result.Count);
                int temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            if (this.MaxSteps is 0)
            {
                return false;
            }

            return this.StepCount >= this.MaxSteps;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"Rapid Context Switch Strategy";
    }
}
