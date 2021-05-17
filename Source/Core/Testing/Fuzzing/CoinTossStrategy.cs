// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// CoinToss Strategy: For each task, do a coin toss;
    /// Double the delay value if heads else delay remains same. Default delay value = 0; Max = 500ms.
    /// </summary>
    internal class CoinTossStrategy : FuzzingStrategy
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
        private readonly ConcurrentDictionary<int, int> PerTaskDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinTossStrategy"/> class.
        /// </summary>
        internal CoinTossStrategy(IRandomValueGenerator random, int maxDelays)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PerTaskDelay = new ConcurrentDictionary<int, int>(5, 10000);
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.StepCount = 0;
            this.PerTaskDelay.Clear();
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            int? currentTaskId = Task.CurrentId;
            if (currentTaskId == null)
            {
                next = 0;
                return true;
            }

            this.StepCount++;

            int retval = 0;
            if (!this.PerTaskDelay.TryGetValue((int)currentTaskId, out int delay))
            {
                retval = 0;
                if (this.CoinToss())
                {
                    retval = 1;
                }
            }
            else
            {
                if (this.CoinToss())
                {
                    retval = delay == 0 ? 1 : delay * 2;
                }

                this.PerTaskDelay.TryRemove((int)currentTaskId, out delay);
            }

            // Make sure that the delay value is always < 500ms.
            if (retval >= 500)
            {
                retval = 1;
            }

            // Save this delay.
            this.PerTaskDelay.TryAdd((int)currentTaskId, retval);

            next = retval;
            return true;
        }

        internal bool CoinToss() => this.RandomValueGenerator.NextDouble() < 0.5;

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
        internal override string GetDescription() => $"Coin Toss";
    }
}
