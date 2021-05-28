// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// Abstract fuzzing strategy used during testing.
    /// </summary>
    internal abstract class FuzzingStrategy : ExplorationStrategy
    {
        /// <summary>
        /// Creates a <see cref="FuzzingStrategy"/> from the specified configuration.
        /// </summary>
        internal static FuzzingStrategy Create(Configuration configuration, IRandomValueGenerator generator)
        {
            System.Environment.SetEnvironmentVariable("CoyoteStrategy", configuration.SchedulingStrategy);
            FuzzingStrategy strategy;

            switch (configuration.SchedulingStrategy)
            {
                case "pct":
                case "ppct":
                    strategy = new PPCTStrategy(generator, configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound);
                    break;
                case "portfolio":
                    strategy = new PortfolioStrategy(generator, configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound);
                    break;
                case "random":
                    strategy = new RandomStrategy(generator, configuration.MaxUnfairSchedulingSteps);
                    break;
                case "torch-random":
                    strategy = new TorchRandomStrategy(generator, configuration.MaxUnfairSchedulingSteps);
                    break;
                case "coin-toss":
                    strategy = new CoinTossStrategy(generator, configuration.MaxUnfairSchedulingSteps);
                    break;
                case "rapid-context-switch":
                    strategy = new RapidContextSwitchStrategy(generator, configuration.MaxUnfairSchedulingSteps);
                    break;
                case "one-stop-one-go":
                    strategy = new OneStopOneGoStrategy(generator, configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound);
                    break;
                case "low-delay-percentage":
                    strategy = new LowDelayPercentageStrategy(generator, configuration.MaxUnfairSchedulingSteps);
                    break;
                default:
                    strategy = new PortfolioStrategy(generator, configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound);
                    break;
            }

            return strategy;
        }

        /// <summary>
        /// Returns the next delay.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next delay.</param>
        /// <returns>True if there is a next delay, else false.</returns>
        internal abstract bool GetNextDelay(int maxValue, out int next);
    }
}
