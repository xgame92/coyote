// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.TestService
{
    /// <summary>
    /// Represents a remote resource that can be used to synchronize controlled operations.
    /// </summary>
    internal class RemoteResource
    {
        /// <summary>
        /// The globally unique id of this resource.
        /// </summary>
        internal readonly Guid Id;

        /// <summary>
        /// Set of operations that this resource must signal upon release.
        /// </summary>
        private readonly HashSet<RemoteOperation> SignalOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteResource"/> class.
        /// </summary>
        internal RemoteResource(Guid id)
        {
            this.Id = id;
            this.SignalOperations = new HashSet<RemoteOperation>();
        }

        /// <summary>
        /// Registers the specified operation to get a notification once the resource is released.
        /// </summary>
        internal void Register(RemoteOperation op) => this.SignalOperations.Add(op);

        /// <summary>
        /// Signals the specified waiting operation that the resource has been released.
        /// </summary>
        internal void Signal(RemoteOperation op)
        {
            if (this.SignalOperations.Contains(op))
            {
                op.Enable();
                this.SignalOperations.Remove(op);
            }
        }

        /// <summary>
        /// Signals all waiting operations that the resource has been released.
        /// </summary>
        internal void SignalAll()
        {
            foreach (var op in this.SignalOperations)
            {
                op.Enable();
            }

            this.SignalOperations.Clear();
        }
    }
}
