// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.SystematicTesting.Strategies
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    internal class RandomStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// The set of custom hashed states.
        /// </summary>
        private readonly HashSet<int> CustomHashedStates;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        public RandomStrategy(int maxSteps, IRandomValueGenerator random)
        {
            this.RandomValueGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.CustomHashedStates = new HashSet<int>();
            this.Epochs = 0;
        }

        /// <inheritdoc/>
        public virtual bool InitializeNextIteration(uint iteration)
        {
            if (this.Epochs == 10 || this.Epochs == 20 || this.Epochs == 40 || this.Epochs == 80 ||
                this.Epochs == 160 || this.Epochs == 320 || this.Epochs == 640 || this.Epochs == 1280 || this.Epochs == 2560 ||
                this.Epochs == 5120 || this.Epochs == 10240 || this.Epochs == 20480 || this.Epochs == 40960 ||
                this.Epochs == 81920 || this.Epochs == 163840)
            {
                System.Console.WriteLine($"==================> #{this.Epochs} Custom States (size: {this.CustomHashedStates.Count})");
            }

            this.Epochs++;

            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.ScheduledSteps = 0;
            return true;
        }

        /// <summary>
        /// Captures metadata related to the current execution step, and returns
        /// a value representing the current program state.
        /// </summary>
        internal void CaptureExecutionStep(int state)
        {
            this.CustomHashedStates.Add(state);
        }

        /// <inheritdoc/>
        public virtual bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current, bool isYielding, out AsyncOperation next)
        {
            var enabledOps = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOps.Count is 0)
            {
                next = null;
                return false;
            }

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            next = false;
            if (this.RandomValueGenerator.Next(maxValue) is 0)
            {
                next = true;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps is 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <inheritdoc/>
        public bool IsFair() => true;

        /// <inheritdoc/>
        public virtual string GetDescription() => $"random[seed '{this.RandomValueGenerator.Seed}']";

        /// <inheritdoc/>
        public virtual void Reset()
        {
            this.ScheduledSteps = 0;
        }
    }
}
