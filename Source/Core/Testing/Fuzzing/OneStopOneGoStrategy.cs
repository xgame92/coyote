// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class OneStopOneGoStrategy : FuzzingStrategy
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
        /// The maximum number of steps after which we should reshuffle the probabilities.
        /// </summary>
        protected readonly int PriorityChangePoints;

        private readonly List<int> LowPrioritySet = new List<int>();

        private readonly List<int> HighPrioritySet = new List<int>();

        private enum Strategy
        {
            OneStop,
            OneGo
        }

        private Strategy strategy;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneStopOneGoStrategy"/> class.
        /// </summary>
        internal OneStopOneGoStrategy(IRandomValueGenerator random, int maxDelays = 500, int priorityChangePoints = 2)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PriorityChangePoints = priorityChangePoints;
            this.strategy = Strategy.OneStop;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.LowPrioritySet.Clear();
            this.HighPrioritySet.Clear();
            this.strategy = this.RandomValueGenerator.NextDouble() < 0.5 ? Strategy.OneStop : Strategy.OneGo;
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

            // Reshuffle the probabilities after every (this.MaxSteps / this.PriorityChangePoints) steps.
            /*
            if (this.StepCount % (this.MaxSteps / this.PriorityChangePoints) == 0)
            {
                this.LowPrioritySet.Clear();
                this.HighPrioritySet.Clear();
            }
            */

            // Stop all tasks except one.
            if (this.strategy == Strategy.OneGo)
            {
                // If this task is not assigned to any Low/High priority group.
                if (!this.LowPrioritySet.Contains((int)currentTaskId) && !this.HighPrioritySet.Contains((int)currentTaskId))
                {
                    // Randomly assign a Task to long/short delay group.
                    if (this.RandomValueGenerator.NextDouble() < 0.5 && this.HighPrioritySet.Count == 0)
                    {
                        this.HighPrioritySet.Add((int)currentTaskId);
                    }
                    else
                    {
                        this.LowPrioritySet.Add((int)currentTaskId);
                    }
                }
            }
            else if (this.strategy == Strategy.OneStop)
            {
                // If this task is not assigned to any Low/High priority group.
                if (!this.LowPrioritySet.Contains((int)currentTaskId) && !this.HighPrioritySet.Contains((int)currentTaskId))
                {
                    // Randomly assign a Task to long/short delay group.
                    if (this.RandomValueGenerator.NextDouble() < 0.5 && this.LowPrioritySet.Count == 0)
                    {
                        this.LowPrioritySet.Add((int)currentTaskId);
                    }
                    else
                    {
                        this.HighPrioritySet.Add((int)currentTaskId);
                    }
                }
            }

            // If this Task lies in the HighPrioritySet, we will return a delay of 1ms else 10ms.
            if (this.HighPrioritySet.Contains((int)currentTaskId))
            {
                next = 0;
            }
            else
            {
                next = 100;
            }

            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            return false;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"OneStopOneGo fuzzing";
    }
}
