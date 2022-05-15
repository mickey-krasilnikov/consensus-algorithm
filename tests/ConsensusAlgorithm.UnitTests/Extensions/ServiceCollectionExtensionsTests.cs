using ConsensusAlgorithm.Core.Extensions;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ConsensusAlgorithm.UnitTests.Extensions
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void ServiceCollectionExtensionsTest()
        {
            //Arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigurationManager configurationManager = new();
            var loggerMock = new Mock<ILogger<ConsensusService>>();

            //Act
            configurationManager.AddJsonFile("appsettings.json", true, reloadOnChange: true);
            configurationManager.AddJsonFile("appsettings.Development.json", true, reloadOnChange: true);
            serviceCollection.AddSingleton(loggerMock.Object);
            serviceCollection.AddConsensusRelatedServices(configurationManager);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var consensusService = serviceProvider.GetRequiredService<IConsensusService>();

            //Assert
            consensusService.Should().NotBeNull();
        }
    }
}
