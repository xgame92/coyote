// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// Torch's Random strategy. (Delay prob - 0.05; random delay range - [0, 100]; upper bound: 5000 per task).
    /// </summary>
    internal class TorchRandomStrategy : FuzzingStrategy
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
        private readonly Dictionary<int, int> PerTaskTotalDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="TorchRandomStrategy"/> class.
        /// </summary>
        internal TorchRandomStrategy(IRandomValueGenerator random, int maxDelays)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PerTaskTotalDelay = new Dictionary<int, int>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.PerTaskTotalDelay.Clear();
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            int currentTaskId = (int)Runtime.CoyoteRuntime.Current.AsyncLocalParentTaskId.Value;

            this.StepCount++;

            int retval = 0;
            // 0.05 probability of 1-100ms delay
            if (this.RandomValueGenerator.NextDouble() < 0.05)
            {
                retval = this.RandomValueGenerator.Next(100);
            }

            if (this.PerTaskTotalDelay.TryGetValue(currentTaskId, out int delay))
            {
                // Max delay per thread.
                if (delay > 5000)
                {
                    retval = 0;
                }

                // Update the total delay per thread.
                this.PerTaskTotalDelay.Remove(currentTaskId);
                this.PerTaskTotalDelay.Add(currentTaskId, delay + retval);
            }
            else
            {
                this.PerTaskTotalDelay.Add(currentTaskId, retval);
            }

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
        internal override string GetDescription() => $"Torch Random";
    }
}
