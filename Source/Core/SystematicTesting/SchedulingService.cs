// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting.Strategies;

#pragma warning disable CS1591
#pragma warning disable SA1005
#pragma warning disable SA1506
namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Service that gives access to scheduling APIs for controlling the execution
    /// of asynchronous operations during systematic testing.
    /// </summary>
    public static class SchedulingService
    {
        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private static readonly ConcurrentDictionary<Guid, OperationScheduler> SchedulerMap =
            new ConcurrentDictionary<Guid, OperationScheduler>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingService"/> class.
        /// </summary>
        //internal SchedulingService()
        //{
        //    this.SchedulerMap = new ConcurrentDictionary<Guid, OperationScheduler>();
        //}

        public static Guid Attach()
        {
            Guid schedulerId = Guid.NewGuid();
            SchedulerMap.GetOrAdd(schedulerId, id =>
            {
                var configuration = Configuration.Create();
                var randomValueGenerator = new RandomValueGenerator(configuration);
                var strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, randomValueGenerator);
                var runtime = new CoyoteRuntime(configuration, strategy, randomValueGenerator);
                return runtime.Scheduler;
            });

            return schedulerId;
        }

        public static void Detach(Guid schedulerId)
        {
            if (SchedulerMap.TryRemove(schedulerId, out OperationScheduler scheduler))
            {
                // scheduler.Dispose();
            }
        }

        public static void CreateOperation(Guid schedulerId, ulong operationId)
        {
            if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
            {
                var op = new TaskOperation(operationId, $"op({operationId})", scheduler);
                scheduler.CreateOperation(op);
            }
        }

        public static void StartOperation(Guid schedulerId, ulong operationId)
        {
            if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler) &&
                scheduler.OperationMap.TryGetValue(operationId, out AsyncOperation op))
            {
                scheduler.StartOperation(op);
            }
        }

        //public static void JoinOperation(Guid schedulerId, ulong operationId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler) &&
        //        scheduler.OperationMap.TryGetValue(operationId, out AsyncOperation op))
        //    {
        //        var currentOp = scheduler.GetExecutingOperation<TaskOperation>();
        //        currentOp.BlockUntilTaskCompletes
        //        scheduler.JoinOperation(op);
        //    }
        //}

        //public static void CompleteOperation(Guid schedulerId, ulong operationId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler) &&
        //        scheduler.OperationMap.TryGetValue(operationId, out AsyncOperation op))
        //    {
        //    }
        //}

        //public static void CreateResource(Guid schedulerId, ulong resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static void WaitResource(Guid schedulerId, ulong resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static void SignalResource(Guid schedulerId, ulong resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static void DeleteResource(Guid schedulerId, ulong resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static void ScheduleNext(Guid schedulerId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static bool NextBoolean(Guid schedulerId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static int NextInteger(Guid schedulerId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static int NextInteger(Guid schedulerId, ulong maxValue)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static ulong ScheduledOperationId(Guid schedulerId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //public static int RandomSeed(Guid schedulerId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}
    }
}
