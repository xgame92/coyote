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

        public override Task<AttachReply> Attach(AttachRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Attaching to the scheduler with id {0}", request.SchedulerId);
            return Task.FromResult(new AttachReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<DetachReply> Detach(DetachRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Detaching to the scheduler with id {0}", request.SchedulerId);
            return Task.FromResult(new DetachReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<CreateOperationReply> CreateOperation(CreateOperationRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Creating operation with id {0}", request.OperationId);
            return Task.FromResult(new CreateOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<StartOperationReply> StartOperation(StartOperationRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Starting operation with id {0}", request.OperationId);
            return Task.FromResult(new StartOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<JoinOperationReply> JoinOperation(JoinOperationRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Joining operation with id {0}", request.OperationId);
            return Task.FromResult(new JoinOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<CompleteOperationReply> CompleteOperation(CompleteOperationRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Completing operation with id {0}", request.OperationId);
            return Task.FromResult(new CompleteOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<ScheduleNextReply> ScheduleNext(ScheduleNextRequest request, ServerCallContext context)
        {
            this.Logger.LogInformation("Scheduling the next operation with id {0}", request.SchedulerId);
            return Task.FromResult(new ScheduleNextReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }
    }
}
