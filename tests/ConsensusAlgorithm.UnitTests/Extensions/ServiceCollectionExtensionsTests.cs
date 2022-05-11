using ConsensusAlgorithm.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ConsensusAlgorithm.UnitTests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void ServiceCollectionExtensionsTest() 
        {
            var serviceCollection = new ServiceCollection();
            ConfigurationManager configurationManager = new();
            configurationManager.AddJsonFile("appsettings.json", true, reloadOnChange: true);
            Assert.DoesNotThrow(() => serviceCollection.AddConsensusRelatedServices(configurationManager));
        }
    }
}
