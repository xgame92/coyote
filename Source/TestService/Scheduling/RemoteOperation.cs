// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Coyote.TestService
{
    /// <summary>
    /// Represents a remote operation that can be controlled during systematic testing.
    /// </summary>
    internal class RemoteOperation : AsyncOperation
    {
        /// <summary>
        /// The globally unique id of this operation.
        /// </summary>
        internal readonly Guid Guid;

        /// <summary>
        /// Set of enabled operations.
        /// </summary>
        private readonly HashSet<RemoteOperation> EnabledOperations;

        /// <summary>
        /// Set of operations that this operation is waiting to be completed before this operation can resume.
        /// </summary>
        private readonly HashSet<RemoteOperation> WaitOperations;

        /// <summary>
        /// Set of operations that this operation must signal once it completes.
        /// </summary>
        private readonly HashSet<RemoteOperation> SignalOperations;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// True if the operation is completed, else false.
        /// </summary>
        internal bool IsCompleted => this.Status is AsyncOperationStatus.Completed ||
            this.Status is AsyncOperationStatus.Canceled;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteOperation"/> class.
        /// </summary>
        internal RemoteOperation(Guid operationId, ulong sequenceId, HashSet<RemoteOperation> enabledOperations, ILogger logger)
            : base(sequenceId, string.Empty)
        {
            this.Guid = operationId;
            this.EnabledOperations = enabledOperations;
            // Perhaps create on demand?
            this.WaitOperations = new HashSet<RemoteOperation>();
            this.SignalOperations = new HashSet<RemoteOperation>();
            this.Logger = logger;
        }

        /// <summary>
        /// Enables this operation.
        /// </summary>
        internal void Enable()
        {
            this.Status = AsyncOperationStatus.Enabled;
            this.EnabledOperations.Add(this);
        }

        /// <inheritdoc/>
        internal override bool TryEnable()
        {
            this.Logger.LogDebug("Try enable operation '{0}' with status '{1}'.", this.Guid, this.Status);
            if ((this.Status is AsyncOperationStatus.BlockedOnWaitAll && this.WaitOperations.All(op => op.IsCompleted)) ||
                (this.Status is AsyncOperationStatus.BlockedOnWaitAny && this.WaitOperations.Any(op => op.IsCompleted)))
            {
                this.Enable();
                this.WaitOperations.Clear();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Blocks this operation until the specified operation completes.
        /// </summary>
        internal void WaitOperationCompletes(RemoteOperation op)
        {
            this.Logger.LogDebug("Operation '{0}' is waiting for operation '{1}'.", this.Guid, op.Guid);
            this.Status = AsyncOperationStatus.BlockedOnWaitAll;
            this.EnabledOperations.Remove(this);
            this.WaitOperations.Add(op);
            op.SignalOperations.Add(this);
        }

        /// <summary>
        /// Blocks the operation until all or any of the specified operations complete.
        /// </summary>
        internal void WaitOperationsComplete(RemoteOperation[] ops, bool waitAll)
        {
            // In the case where `waitAll` is false, we check if all operations are not completed. If that is the case,
            // then we add all operations to `WaitOperations` and wait at least one to complete. If, however, even one
            // operation is completed, then we should not wait, as it can cause potential deadlocks.
            if (waitAll || ops.All(op => !op.IsCompleted))
            {
                foreach (var op in ops)
                {
                    if (!op.IsCompleted)
                    {
                        this.Logger.LogDebug("Operation '{0}' is waiting for operation '{1}'.", this.Guid, op.Guid);
                        this.WaitOperations.Add(op);
                        op.SignalOperations.Add(this);
                    }
                }

                if (this.WaitOperations.Count > 0)
                {
                    this.Status = waitAll ? AsyncOperationStatus.BlockedOnWaitAll : AsyncOperationStatus.BlockedOnWaitAny;
                    // this.Scheduler.ScheduleNextInner();
                }
            }
        }

        /// <summary>
        /// Blocks this operation until the specified resource signals.
        /// </summary>
        internal void WaitResourceSignal(RemoteResource resource)
        {
            this.Logger.LogDebug("Operation '{0}' is waiting for resource '{1}'.", this.Guid, resource.Id);
            this.Status = AsyncOperationStatus.BlockedOnResource;
            this.EnabledOperations.Remove(this);
            resource.Register(this);
        }

        /// <summary>
        /// Cancels this operation.
        /// </summary>
        internal void Cancel()
        {
            this.Status = AsyncOperationStatus.Canceled;
            this.EnabledOperations.Remove(this);
        }

        /// <summary>
        /// Completes this operation.
        /// </summary>
        internal void Complete()
        {
            if (!this.IsCompleted)
            {
                this.Status = AsyncOperationStatus.Completed;
                this.EnabledOperations.Remove(this);

                foreach (var op in this.SignalOperations)
                {
                    op.TryEnable();
                }

                this.SignalOperations.Clear();
            }
        }

        /// <summary>
        /// Resets this operation.
        /// </summary>
        internal void Reset()
        {
            this.Status = AsyncOperationStatus.None;
            this.WaitOperations.Clear();
            this.SignalOperations.Clear();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is RemoteOperation op)
            {
                return this.Guid == op.Guid;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Guid.GetHashCode();
    }
}
