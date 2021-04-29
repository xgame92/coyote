// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class FairPCTStrategy : FuzzingStrategy
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
        protected int PriorityChangePoint;

        /// <summary>
        /// Dictionry to store step count per task.
        /// </summary>
        private readonly Dictionary<int, int> StepCountPerTask;

        /// <summary>
        /// Steps after which we should inject long delays.
        /// </summary>
        private double MaxStepCountPerIteration;

        /// <summary>
        /// Number of iterations.
        /// </summary>
        private int IterationCount;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int GlobalStepCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="FairPCTStrategy"/> class.
        /// </summary>
        internal FairPCTStrategy(IRandomValueGenerator random, int maxDelays = 500)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.MaxStepCountPerIteration = 0;
            this.GlobalStepCount = 0;
            this.StepCountPerTask = new Dictionary<int, int>();
            this.PriorityChangePoint = 2;
            this.IterationCount = 0;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // This should be true in second iteration only.
            if (this.GlobalStepCount != 0 && this.MaxStepCountPerIteration == 0)
            {
                this.MaxStepCountPerIteration = this.GlobalStepCount;
            }

            this.GlobalStepCount = 0;
            this.StepCountPerTask.Clear();
            this.IterationCount++;

            // After every 100 iterations, increment the priority change point depth.
            if (this.IterationCount % 100 == 0)
            {
                this.PriorityChangePoint = this.PriorityChangePoint + 2;

                // Make sure that Priority chnage point depth is always less than max StepCount Per iteration.
                if (this.MaxStepCountPerIteration != 0 && this.PriorityChangePoint > this.MaxStepCountPerIteration)
                {
                    this.PriorityChangePoint = 2;
                }
            }

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

            this.GlobalStepCount++;

            // Fetch the step count per task, increment it and store again.
            if (this.StepCountPerTask.TryGetValue((int)currentTaskId, out int taskStepCount))
            {
                this.StepCountPerTask.Remove((int)currentTaskId);
            }
            else
            {
                taskStepCount = 0;
            }

            this.StepCountPerTask.Add((int)currentTaskId, ++taskStepCount);

            // Inject long delays after every PriorityChangePoint steps.
            if (taskStepCount % this.PriorityChangePoint == 0)
            {
                next = 5 + this.RandomValueGenerator.Next(20);
            }
            else
            {
                next = 0;
            }

            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.GlobalStepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            return this.GlobalStepCount > this.MaxSteps;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"FairPCT fuzzing";
    }
}
