using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Polly;
using ConsensusAlgorithm.Core.Configuration;
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
            var clusterConfig = configuration.GetRequiredSection("ConsensusCluster").Get<ConsensusClusterConfig>();
            services.AddSingleton(clusterConfig);
            services.AddSingleton<IConsensusRepository, ConsensusInMemoryRepository>();
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<IServerStatusService>(s => new ServerStatusService(s.GetRequiredService<ConsensusClusterConfig>().CurrentServerId));
            services.AddSingleton<IStateMachine, DictionaryStateMachine>();
            services.AddSingleton<IConsensusService, ConsensusService>();

            services.AddHttpClient<IConsensusApiClient, ConsensusApiClient>()
                .AddTransientHttpErrorPolicy(policy =>
                    policy.WaitAndRetryAsync(clusterConfig.RetryCount, _ => clusterConfig.RetryDelay));

            services.AddHostedService<ConsensusService>();

            return services;
        }
    }
}
