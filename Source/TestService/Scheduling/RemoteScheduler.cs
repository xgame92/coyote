// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1027
#pragma warning disable CS1591
#pragma warning disable SA1005
#pragma warning disable SA1506
namespace Microsoft.Coyote.TestService
{
    /// <summary>
    /// Scheduler for systematically testing remote applications.
    /// </summary>
    internal class RemoteScheduler
    {
        /// <summary>
        /// The globally unique scheduler id.
        /// </summary>
        internal readonly Guid Id;

        /// <summary>
        /// The scheduling strategy.
        /// </summary>
        private readonly SchedulingStrategy Strategy;

        /// <summary>
        /// Map from unique operation ids to operations.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, RemoteOperation> OperationMap;

        /// <summary>
        /// Set of enabled operations.
        /// </summary>
        private readonly HashSet<RemoteOperation> EnabledOperations;

        /// <summary>
        /// The unique id of the main operation.
        /// </summary>
        private readonly Guid MainOperationId;

        /// <summary>
        /// The currently scheduled operation.
        /// </summary>
        private RemoteOperation ScheduledOperation;

        /// <summary>
        /// Monotonically increasing operation sequence id counter.
        /// </summary>
        private ulong OperationSequenceIdCounter;

        /// <summary>
        /// An iteration count incrementing on each detach.
        /// </summary>
        private uint IterationCount;

        /// <summary>
        /// Object that is used to synchronize access to the scheduler.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// The current iteration.
        /// </summary>
        internal uint CurrentIteration => this.IterationCount;

        /// <summary>
        /// True if the scheduler is attached to the executing program, else false.
        /// </summary>
        internal bool IsAttached { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteScheduler"/> class.
        /// </summary>
        internal RemoteScheduler(Guid id, SchedulingStrategy strategy, ILogger logger)
        {
            this.Id = id;
            this.Strategy = strategy;
            this.OperationMap = new ConcurrentDictionary<Guid, RemoteOperation>();
            this.EnabledOperations = new HashSet<RemoteOperation>();
            this.MainOperationId = Guid.NewGuid();
            this.OperationSequenceIdCounter = 0;
            this.IterationCount = 0;
            this.SyncObject = new object();
            this.Logger = logger;
            this.IsAttached = false;
        }

        /// <summary>
        /// Attaches the caller to the scheduler, creates and starts executing the main operation, and returns its unique id.
        /// </summary>
        /// <returns>The unique id of the main operation.</returns>
        internal Guid Attach()
        {
            lock (this.SyncObject)
            {
                this.IsAttached = true;

                this.Logger.LogInformation("Creating main operation with id '{0}'.", this.MainOperationId);
                this.CreateOperationInner(this.MainOperationId);

                this.Logger.LogInformation("Starting main operation with id '{0}'.", this.MainOperationId);
                this.StartOperationInner(this.MainOperationId);

                return this.MainOperationId;
            }
        }

        /// <summary>
        /// Detaches the caller from the scheduler.
        /// </summary>
        internal void Detach()
        {
            lock (this.SyncObject)
            {
                this.Logger.LogInformation("Detaching all operations.");

                this.IsAttached = false;

                foreach (var kvp in this.OperationMap)
                {
                    RemoteOperation op = kvp.Value;
                    if (!op.IsCompleted)
                    {
                        this.Logger.LogInformation("Detaching the operation with id '{0}'.", op.Id);

                        // If the operation has not already completed, then cancel it.
                        // op->is_scheduled = true;
                        op.Cancel();
                    }
                }

                this.OperationMap.Clear();
                this.EnabledOperations.Clear();
                // resource_map.clear();
                this.OperationSequenceIdCounter = 0;
                this.IterationCount++;
            }
        }

        /// <summary>
        /// Creates a new operation with the specified id.
        /// </summary>
        internal void CreateOperation(Guid operationId)
        {
            lock (this.SyncObject)
            {
                this.CreateOperationInner(operationId);
            }
        }

        private void CreateOperationInner(Guid operationId)
        {
            var op = this.OperationMap.GetOrAdd(operationId, id => new RemoteOperation(
                id, this.OperationSequenceIdCounter++, this.EnabledOperations, this.Logger));
            if (this.OperationMap.Count is 1)
            {
                // This is the first operation, so schedule it.
                this.ScheduledOperation = op;
                // op->is_scheduled = true;
            }

            if (op.IsCompleted)
            {
                op.Reset();
                // op->is_scheduled = false;
            }
        }

        /// <summary>
        /// Starts executing the operation with the specified id.
        /// </summary>
        internal void StartOperation(Guid operationId)
        {
            lock (this.SyncObject)
            {
                this.StartOperationInner(operationId);
            }
        }

        private void StartOperationInner(Guid operationId)
        {
            this.OperationMap.TryGetValue(operationId, out RemoteOperation op);
            if (!op.IsCompleted)
            {
                op.Enable();
            }
        }

        /// <summary>
        /// Wait for the specified operation to complete.
        /// </summary>
        internal Guid WaitOperation(Guid operationId)
        {
            lock (this.SyncObject)
            {
                this.OperationMap.TryGetValue(operationId, out RemoteOperation waitOp);
                if (!waitOp.IsCompleted)
                {
                    this.ScheduledOperation.WaitOperationCompletes(waitOp);

                    // Waiting for the operation to complete, so schedule the next enabled operation.
                    return this.ScheduleNextInner();
                }

                this.Logger.LogInformation("[Debug-1] Scheduling operation with id '{0}'.", operationId);
                return this.ScheduledOperation.Guid;
            }
        }

        /// <summary>
        /// Complete the specified operation.
        /// </summary>
        internal Guid CompleteOperation(Guid operationId)
        {
            lock (this.SyncObject)
            {
                this.OperationMap.TryGetValue(operationId, out RemoteOperation op);
                op.Complete();

                // The current operation has completed, so schedule the next enabled operation.
                return this.ScheduleNextInner();
            }
        }

        //internal void CreateResource(Guid resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal void WaitResource(Guid resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal void SignalResource(Guid resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal void DeleteResource(Guid resourceId)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        internal Guid ScheduleNext()
        {
            lock (this.SyncObject)
            {
                return this.ScheduleNextInner();
            }
        }

        private Guid ScheduleNextInner()
        {
            // Check if the schedule has finished.
            if (this.EnabledOperations.Count is 0)
            {
                this.Logger.LogInformation("[Debug-2] Scheduling operation with id '{0}'.", Guid.Empty);
                return Guid.Empty;
            }

            // Choose the next operation to schedule.
            if (!this.Strategy.GetNextOperation(this.EnabledOperations, this.ScheduledOperation, false, out AsyncOperation next))
            {
                this.Logger.LogInformation("[Debug-3] Scheduling operation with id '{0}'.", Guid.Empty);
                return Guid.Empty;
            }

            // var previousOp = this.ScheduledOperation;
            this.ScheduledOperation = next as RemoteOperation;
            this.Logger.LogInformation("[Debug-4] Scheduling operation with id '{0}'.", this.ScheduledOperation.Guid);
            return this.ScheduledOperation.Guid;
        }

        //internal bool NextBoolean()
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal int NextInteger()
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal int NextInteger(ulong maxValue)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal ulong ScheduledOperationId()
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}

        //internal int RandomSeed()
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //    }
        //}
    }
}
