using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RssFeeder.Mvc;
using RssFeeder.Mvc.Services;
using RssFeeder.Tests.Infrastructure;

namespace RssFeeder.Tests.Mvc;

public class ServiceRegistrationExtensionsTests
{
    [Fact]
    public void AddRssFeederMvcConfiguration_BindsOptions_AndResolvesFileBackedProvider()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateFile(
            "feeds-config.json",
            """
            [
              {
                "id": "alpha",
                "title": "Alpha Feed",
                "url": "https://alpha.example",
                "description": "Alpha",
                "outputfile": "alpha.xml",
                "language": "en-us",
                "customparser": "parser",
                "filters": [],
                "collectionname": "alpha"
              }
            ]
            """);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeedDefinitions:SourceFile"] = "feeds-config.json",
                ["CosmosDb:Account"] = "https://cosmos.example",
                ["CosmosDb:Key"] = "cosmos-key",
                ["CosmosDb:DatabaseName"] = "rss-db",
                ["CosmosDb:ContainerName"] = "feeds"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(
            new TestWebHostEnvironment { ContentRootPath = workspace.RootPath });

        services.AddRssFeederMvcConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        var feedOptions = provider.GetRequiredService<IOptions<FeedDefinitionOptions>>().Value;
        var cosmosOptions = provider.GetRequiredService<IOptions<RssFeeder.Mvc.Services.CosmosDbOptions>>().Value;
        var feedProvider = provider.GetRequiredService<IFeedDefinitionProvider>();

        Assert.Equal("feeds-config.json", feedOptions.SourceFile);
        Assert.Equal("https://cosmos.example", cosmosOptions.Account);
        Assert.Equal("rss-db", cosmosOptions.DatabaseName);
        Assert.True(feedProvider.FeedExists("alpha"));
    }
}