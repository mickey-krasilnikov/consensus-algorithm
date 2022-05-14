﻿using Microsoft.Extensions.DependencyInjection;
using ConsensusAlgorithm.Core.Configuration;
using Microsoft.Extensions.Configuration;
using ConsensusAlgorithm.Core.StateMachine;
using ConsensusAlgorithm.DataAccess;
using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.Core.Services.TimerService;

namespace ConsensusAlgorithm.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConsensusRelatedServices(this IServiceCollection services, ConfigurationManager configuration)
        {
            services.AddSingleton(configuration.GetRequiredSection("ConsensusCluster").Get<ConsensusClusterConfig>());
            services.AddSingleton<IConsensusRepository, ConsensusInMemoryRepository>();
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<IServerStatusService>(s => new ServerStatusService(s.GetRequiredService<ConsensusClusterConfig>().CurrentServerId));
            services.AddSingleton<IStateMachine, DictionaryStateMachine>();
            services.AddSingleton<IConsensusService, ConsensusService>();
            services.AddSingleton<IList<IConsensusApiClient>>(s =>
            {
                var config = s.GetRequiredService<ConsensusClusterConfig>();
                var timerService = s.GetRequiredService<ITimerService>();
                return config!.ServerList
                    .Where(s => s.Key != config!.CurrentServerId)
                    .Select(s => (IConsensusApiClient)new ConsensusApiClient(s.Key, s.Value, timerService))
                    .ToList();
            });

            services.AddHostedService<ConsensusService>();
            return services;
        }
    }
}
