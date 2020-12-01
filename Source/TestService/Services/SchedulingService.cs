// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Coyote.SystematicTesting;
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
            if (!Guid.TryParse(request.SchedulerId, out Guid schedulerId))
            {
                schedulerId = Guid.NewGuid();
            }

            var configuration = Configuration.Create();

            SchedulingStrategy strategy;
            if (request.StrategyType is "replay")
            {
                var trace = request.Trace.Split(new string[] { "," }, StringSplitOptions.None);
                ScheduleTrace schedule = new ScheduleTrace(trace);
                strategy = new ReplayStrategy(configuration, schedule, false);
            }
            else if (request.StrategyType is "probabilistic")
            {
                strategy = new ProbabilisticRandomStrategy(configuration.MaxFairSchedulingSteps,
                    configuration.StrategyBound, new RandomValueGenerator(configuration));
            }
            else if (request.StrategyType is "fairpct")
            {
                var randomValueGenerator = new RandomValueGenerator(configuration);
                var prefixLength = configuration.SafetyPrefixBound is 0 ?
                    configuration.MaxUnfairSchedulingSteps : configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, configuration.StrategyBound, randomValueGenerator);
                var suffixStrategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, randomValueGenerator);
                strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (request.StrategyType is "pct")
            {
                strategy = new PCTStrategy(configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound,
                    new RandomValueGenerator(configuration));
            }
            else
            {
                strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, new RandomValueGenerator(configuration));
            }

            this.Logger.LogInformation("Creating scheduler '{0}' and strategy {1}.", schedulerId, strategy.GetDescription());
            this.Context.GetOrCreateScheduler(schedulerId, strategy, this.Logger);
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

        public override Task<WaitResourceReply> WaitResource(WaitResourceRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid resourceId = Guid.Parse(request.ResourceId);
            this.Logger.LogInformation("Waiting resource '{0}' in scheduler '{1}'", resourceId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            Guid nextOperationId = scheduler.WaitResource(resourceId);
            return Task.FromResult(new WaitResourceReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                NextOperationId = nextOperationId.ToString()
            });
        }

        public override Task<SignalOperationReply> SignalOperation(SignalOperationRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid resourceId = Guid.Parse(request.ResourceId);
            Guid operationId = Guid.Parse(request.OperationId);
            this.Logger.LogInformation("Signaling operation '{0}' waiting resource '{1}' in scheduler '{2}'",
                operationId, resourceId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.SignalOperation(operationId, resourceId);
            return Task.FromResult(new SignalOperationReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<SignalOperationsReply> SignalOperations(SignalOperationsRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid resourceId = Guid.Parse(request.ResourceId);
            this.Logger.LogInformation("Signaling all operations waiting resource '{0}' in scheduler '{1}'", resourceId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.SignalOperations(resourceId);
            return Task.FromResult(new SignalOperationsReply
            {
                ErrorCode = (uint)ErrorCode.Success
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

        public override Task<CreateResourceReply> CreateResource(CreateResourceRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid resourceId = Guid.Parse(request.ResourceId);
            this.Logger.LogInformation("Creating resource '{0}' in scheduler '{1}'", resourceId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.CreateResource(resourceId);
            return Task.FromResult(new CreateResourceReply
            {
                ErrorCode = (uint)ErrorCode.Success
            });
        }

        public override Task<DeleteResourceReply> DeleteResource(DeleteResourceRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            Guid resourceId = Guid.Parse(request.ResourceId);
            this.Logger.LogInformation("Deleting resource '{0}' in scheduler '{1}'", resourceId, schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            scheduler.DeleteResource(resourceId);
            return Task.FromResult(new DeleteResourceReply
            {
                ErrorCode = (uint)ErrorCode.Success
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

        public override Task<GetNextBooleanReply> GetNextBoolean(GetNextBooleanRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            this.Logger.LogInformation("Scheduling next operation in scheduler '{0}'.", schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            bool value = scheduler.GetNextBoolean();
            return Task.FromResult(new GetNextBooleanReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                Value = value
            });
        }

        public override Task<GetNextIntegerReply> GetNextInteger(GetNextIntegerRequest request, ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            this.Logger.LogInformation("Scheduling next operation in scheduler '{0}'.", schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            int value = scheduler.GetNextInteger(request.MaxValue);
            return Task.FromResult(new GetNextIntegerReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                Value = value
            });
        }

        public override Task<GetTraceReply> GetTrace(GetTraceRequest request,
            ServerCallContext context)
        {
            Guid schedulerId = Guid.Parse(request.SchedulerId);
            this.Logger.LogInformation("Scheduling next operation in scheduler '{0}'.", schedulerId);
            var scheduler = this.Context.GetScheduler(schedulerId);
            string trace = scheduler.GetTrace();
            return Task.FromResult(new GetTraceReply
            {
                ErrorCode = (uint)ErrorCode.Success,
                Trace = trace
            });
        }
    }
}
