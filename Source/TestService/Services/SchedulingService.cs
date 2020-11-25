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
    public class SchedulingService : Scheduler.SchedulerBase
    {
        private readonly ILogger<SchedulingService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingService"/> class.
        /// </summary>
        public SchedulingService(ILogger<SchedulingService> logger)
        {
            this.Logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
