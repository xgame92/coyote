// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Coyote.TestService
{
    /// <summary>
    /// The context of the <see cref="SchedulingService"/>.
    /// </summary>
    internal class Context
    {
        /// <summary>
        /// Map from unique scheduler ids to schedulers.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, RemoteScheduler> SchedulerMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        internal Context()
        {
            this.SchedulerMap = new ConcurrentDictionary<Guid, RemoteScheduler>();
        }

        /// <summary>
        /// Creates a new scheduler with the specified strategy, or returns an existing one if the id already exists.
        /// </summary>
        internal RemoteScheduler CreateScheduler(Guid schedulerId, SchedulingStrategy strategy, ILogger logger) =>
            this.SchedulerMap.GetOrAdd(schedulerId, id => new RemoteScheduler(id, strategy, logger));

        /// <summary>
        /// Returns the scheduler with the specified id, if it exists, else null.
        /// </summary>
        internal RemoteScheduler GetScheduler(Guid schedulerId)
        {
            this.SchedulerMap.TryGetValue(schedulerId, out RemoteScheduler scheduler);
            return scheduler;
        }
    }
}
