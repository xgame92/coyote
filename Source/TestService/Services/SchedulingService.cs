// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Coyote.TestService
{
    /// <summary>
    /// Scheduling service using gRPC.
    /// </summary>
    internal class SchedulingService : Scheduler.SchedulerBase
    {
        private readonly ILogger<SchedulingService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingService"/> class.
        /// </summary>
        public SchedulingService(ILogger<SchedulingService> logger)
        {
            this.Logger = logger;
        }

        public override Task<ScheduleReply> Attach(ScheduleRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Attaching to the scheduler with id {0}", request.SchedulerId);
            return Task.FromResult(new ScheduleReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }
    }
}
