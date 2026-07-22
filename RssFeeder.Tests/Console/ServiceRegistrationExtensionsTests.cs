using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RssFeeder.Console.Models;
using Serilog;

namespace RssFeeder.Tests.Console;

public class ServiceRegistrationExtensionsTests
{
    [Fact]
    public void AddRssFeederConsoleServices_DefaultsCrawlerConfigFilePath_AndBindsLegacyCosmosValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CosmosDB:endpoint"] = "https://legacy-cosmos.example",
                ["CosmosDB:authKey"] = "legacy-key",
                ["database_id"] = "legacy-db",
                ["CrawlerConfig:Source"] = nameof(CrawlerConfigSource.File)
            })
            .Build();

        var services = new ServiceCollection();
        var logger = new LoggerConfiguration().CreateLogger();

        InvokeAddRssFeederConsoleServices(services, configuration, logger);

        using var provider = services.BuildServiceProvider();
        var crawlerConfigOptions = provider.GetRequiredService<CrawlerConfigOptions>();
        var cosmosDbOptions = provider.GetRequiredService<CosmosDbOptions>();

        Assert.Equal(CrawlerConfigSource.File, crawlerConfigOptions.Source);
        Assert.Equal("crawlerconfig.json", crawlerConfigOptions.FilePath);
        Assert.Equal("https://legacy-cosmos.example", cosmosDbOptions.Account);
        Assert.Equal("legacy-key", cosmosDbOptions.Key);
        Assert.Equal("legacy-db", cosmosDbOptions.DatabaseName);
    }

    [Fact]
    public void AddRssFeederConsoleServices_PreservesExplicitCrawlerConfigFilePath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CrawlerConfig:Source"] = nameof(CrawlerConfigSource.File),
                ["CrawlerConfig:FilePath"] = "custom-crawler.json"
            })
            .Build();

        var services = new ServiceCollection();

        InvokeAddRssFeederConsoleServices(
            services,
            configuration,
            new LoggerConfiguration().CreateLogger());

        using var provider = services.BuildServiceProvider();
        var crawlerConfigOptions = provider.GetRequiredService<CrawlerConfigOptions>();

        Assert.Equal("custom-crawler.json", crawlerConfigOptions.FilePath);
    }

    [Fact]
    public void AddRssFeederConsoleServices_PrefersModernCosmosSectionOverLegacyFallback()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CosmosDb:Account"] = "https://modern-cosmos.example",
                ["CosmosDb:Key"] = "modern-key",
                ["CosmosDb:DatabaseName"] = "modern-db",
                ["CosmosDB:endpoint"] = "https://legacy-cosmos.example",
                ["CosmosDB:authKey"] = "legacy-key",
                ["database_id"] = "legacy-db"
            })
            .Build();

        var services = new ServiceCollection();

        InvokeAddRssFeederConsoleServices(
            services,
            configuration,
            new LoggerConfiguration().CreateLogger());

        using var provider = services.BuildServiceProvider();
        var cosmosDbOptions = provider.GetRequiredService<CosmosDbOptions>();

        Assert.Equal("https://modern-cosmos.example", cosmosDbOptions.Account);
        Assert.Equal("modern-key", cosmosDbOptions.Key);
        Assert.Equal("modern-db", cosmosDbOptions.DatabaseName);
    }

    private static void InvokeAddRssFeederConsoleServices(IServiceCollection services, IConfigurationRoot configuration, ILogger logger)
    {
        var extensionType = typeof(RssFeeder.Console.Database.CrawlerConfigProvider).Assembly
            .GetType("RssFeeder.Console.ServiceRegistrationExtensions", throwOnError: true)!;
        var method = extensionType.GetMethod(
            "AddRssFeederConsoleServices",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;

        method.Invoke(null, [services, configuration, logger]);
    }
}