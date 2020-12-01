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
        /// The program schedule trace.
        /// </summary>
        private readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Map from unique operation ids to operations.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, RemoteOperation> OperationMap;

        /// <summary>
        /// Map from unique resource ids to resources.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, RemoteResource> ResourceMap;

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
            this.ScheduleTrace = new ScheduleTrace();
            this.OperationMap = new ConcurrentDictionary<Guid, RemoteOperation>();
            this.ResourceMap = new ConcurrentDictionary<Guid, RemoteResource>();
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
                this.ScheduleTrace.Clear();

                this.Logger.LogDebug("Creating main operation with id '{0}'.", this.MainOperationId);
                this.CreateOperationInner(this.MainOperationId);

                this.Logger.LogDebug("Starting main operation with id '{0}'.", this.MainOperationId);
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
                this.Logger.LogDebug("Detaching all operations.");

                this.IsAttached = false;

                foreach (var kvp in this.OperationMap)
                {
                    RemoteOperation op = kvp.Value;
                    if (!op.IsCompleted)
                    {
                        this.Logger.LogDebug("Detaching the operation with id '{0}'.", op.Id);

                        // If the operation has not already completed, then cancel it.
                        // op->is_scheduled = true;
                        op.Cancel();
                    }
                }

                this.OperationMap.Clear();
                this.ResourceMap.Clear();
                this.EnabledOperations.Clear();
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
        /// Waits for the specified operation to complete and returns the next operation to schedule.
        /// </summary>
        internal Guid WaitOperation(Guid operationId)
        {
            lock (this.SyncObject)
            {
                this.OperationMap.TryGetValue(operationId, out RemoteOperation op);
                if (!op.IsCompleted)
                {
                    this.ScheduledOperation.WaitOperationCompletes(op);

                    // Waiting for the operation to complete, so schedule the next enabled operation.
                    return this.ScheduleNextInner();
                }

                return this.ScheduledOperation.Guid;
            }
        }

        /// <summary>
        /// Waits for the specified resource to get released and returns the next operation to schedule.
        /// </summary>
        internal Guid WaitResource(Guid resourceId)
        {
            lock (this.SyncObject)
            {
                this.ResourceMap.TryGetValue(resourceId, out RemoteResource resource);
                this.ScheduledOperation.WaitResourceSignal(resource);

                // Waiting for the resource to signal, so schedule the next enabled operation.
                return this.ScheduleNextInner();
            }
        }

        /// <summary>
        /// Signals the specified waiting operation that the specified resource is released.
        /// </summary>
        internal void SignalOperation(Guid operationId, Guid resourceId)
        {
            lock (this.SyncObject)
            {
                this.ResourceMap.TryGetValue(resourceId, out RemoteResource resource);
                this.OperationMap.TryGetValue(operationId, out RemoteOperation op);
                resource.Signal(op);
            }
        }

        /// <summary>
        /// Signals all waiting operations that the specified resource is released.
        /// </summary>
        internal void SignalOperations(Guid resourceId)
        {
            lock (this.SyncObject)
            {
                this.ResourceMap.TryGetValue(resourceId, out RemoteResource resource);
                resource.SignalAll();
            }
        }

        /// <summary>
        /// Completes the specified operation and returns the next operation to schedule.
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

        internal void CreateResource(Guid resourceId)
        {
            lock (this.SyncObject)
            {
                this.ResourceMap.GetOrAdd(resourceId, id => new RemoteResource(id));
            }
        }

        internal void DeleteResource(Guid resourceId)
        {
            lock (this.SyncObject)
            {
                this.ResourceMap.TryRemove(resourceId, out RemoteResource _);
            }
        }

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
                return Guid.Empty;
            }

            // Choose the next operation to schedule.
            if (!this.Strategy.GetNextOperation(this.EnabledOperations, this.ScheduledOperation, false, out AsyncOperation next))
            {
                return Guid.Empty;
            }

            this.ScheduleTrace.AddSchedulingChoice(next.Id);

            // var previousOp = this.ScheduledOperation;
            this.ScheduledOperation = next as RemoteOperation;
            return this.ScheduledOperation.Guid;
        }

        //internal bool NextBoolean()
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //this.ScheduleTrace.AddNondeterministicBooleanChoice(choice);
        //    }
        //}

        //internal int NextInteger()
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //this.ScheduleTrace.AddNondeterministicIntegerChoice(choice);
        //    }
        //}

        //internal int NextInteger(ulong maxValue)
        //{
        //    if (SchedulerMap.TryGetValue(schedulerId, out OperationScheduler scheduler))
        //    {
        //this.ScheduleTrace.AddNondeterministicIntegerChoice(choice);
        //    }
        //}

        /// <summary>
        /// Returns the current trace.
        /// </summary>
        internal string GetTrace()
        {
            lock (this.SyncObject)
            {
                return this.ScheduleTrace.GetText(",");
            }
        }
    }
}
