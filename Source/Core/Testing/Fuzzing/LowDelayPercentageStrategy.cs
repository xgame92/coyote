// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// 10% probability of a random delay.
    /// </summary>
    internal class LowDelayPercentageStrategy : FuzzingStrategy
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
        /// Initializes a new instance of the <see cref="LowDelayPercentageStrategy"/> class.
        /// </summary>
        internal LowDelayPercentageStrategy(IRandomValueGenerator random, int maxDelays)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            // 1% delay probability.
            if (this.RandomValueGenerator.NextDouble() < 0.01)
            {
                next = this.RandomValueGenerator.Next(maxValue);
            }
            else
            {
                next = 0;
            }

            this.StepCount++;
            return true;
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
        internal override string GetDescription() => $"1% delay probability";
    }
}
