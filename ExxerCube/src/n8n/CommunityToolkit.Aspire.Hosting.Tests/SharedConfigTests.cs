using ExxerAI.Orchestration.Configuration;
using Xunit;

namespace ExxerAI.Orchestration.Tests;

public class SharedConfigTests
{
    [Fact(Skip = "Under development")]
    public void SharedConfig_Binds_Correctly_From_Defaults()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"SharedConfig:TelemetryEnabled", "true"},
            {"SharedConfig:DefaultRedisPort", "6380"},
            {"SharedConfig:SqlConnection", "DataSource=test;"},
            {"SharedConfig:DefaultOllamaModel", "gemma-7b"},
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var services = new ServiceCollection();
        services.Configure<SharedConfig>(config.GetSection("SharedConfig"));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SharedConfig>>().Value;

        options.DatabaseConfig.DatabaseName.ShouldBe("localai_db");
    }
}