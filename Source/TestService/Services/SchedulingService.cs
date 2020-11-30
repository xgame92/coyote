// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Coyote.SystematicTesting.Strategies;
using Microsoft.Extensions.Logging;

namespace Microsoft.Coyote.TestService
{
    /// <summary>
    /// Operation scheduling service using gRPC.
    /// </summary>
    internal class SchedulingService : Scheduler.SchedulerBase
    {
        private readonly Context Context;
        private readonly ILogger<SchedulingService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingService"/> class.
        /// </summary>
        public SchedulingService(Context context, ILogger<SchedulingService> logger)
        {
            this.Context = context;
            this.Logger = logger;
        }

        public override Task<InitializeReply> Initialize(InitializeRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.NewGuid();
            var configuration = Configuration.Create();
            var randomValueGenerator = new RandomValueGenerator(configuration);
            var strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, randomValueGenerator);

            this.Logger.LogInformation("Creating scheduler '{0}' and strategy {1}.", schedulerId, strategy.GetDescription());
            this.Context.CreateScheduler(schedulerId, strategy, this.Logger);
            return Task.FromResult(new InitializeReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                SchedulerId = schedulerId.ToString()
            });
        }

        public override Task<AttachReply> Attach(AttachRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            this.Logger.LogInformation("Attaching to scheduler '{0}'.", schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            Guid mainOperationId = scheduler.Attach();

            return Task.FromResult(new AttachReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                Iteration = scheduler.CurrentIteration,
                MainOperationId = mainOperationId.ToString()
            });
        }

        public override Task<DetachReply> Detach(DetachRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            this.Logger.LogInformation("Detaching from scheduler '{0}'.", schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.Detach();
            return Task.FromResult(new DetachReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<CreateOperationReply> CreateOperation(CreateOperationRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid operationId = Guid.Parse(request.OperationId);
            this.Logger.LogInformation("Creating operation '{0}' in scheduler '{1}'", operationId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.CreateOperation(operationId);
            return Task.FromResult(new CreateOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<StartOperationReply> StartOperation(StartOperationRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid operationId = Guid.Parse(request.OperationId);
            this.Logger.LogInformation("Starting operation '{0}' in scheduler '{1}'", operationId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.StartOperation(operationId);
            return Task.FromResult(new StartOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<WaitOperationReply> WaitOperation(WaitOperationRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid operationId = Guid.Parse(request.OperationId);
            this.Logger.LogInformation("Waiting operation '{0}' in scheduler '{1}'", operationId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            Guid nextOperationId = scheduler.WaitOperation(operationId);
            return Task.FromResult(new WaitOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                NextOperationId = nextOperationId.ToString()
            });
        }

        public override Task<CompleteOperationReply> CompleteOperation(CompleteOperationRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid operationId = Guid.Parse(request.OperationId);
            this.Logger.LogInformation("Completing operation '{0}' in scheduler '{1}'", operationId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            Guid nextOperationId = scheduler.CompleteOperation(operationId);
            return Task.FromResult(new CompleteOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                NextOperationId = nextOperationId.ToString()
            });
        }

        public override Task<ScheduleNextReply> ScheduleNext(ScheduleNextRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            this.Logger.LogInformation("Scheduling next operation in scheduler '{0}'.", schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            Guid nextOperationId = scheduler.ScheduleNext();
            return Task.FromResult(new ScheduleNextReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                NextOperationId = nextOperationId.ToString()
            });
        }
    }
}
