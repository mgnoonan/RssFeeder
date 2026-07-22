using Raven.Client.Documents;
using RssFeeder.Console.Database;
using RssFeeder.Console.Models;

namespace RssFeeder.Tests.Console;

public class CrawlerConfigProviderTests
{
    [Fact]
    public void GetConfig_UsesAbsolutePathWhenProvided()
    {
        using var workspace = new Infrastructure.TestWorkspace();
        var configPath = workspace.CreateFile(
            "crawlerconfig.json",
            """
            {
              "Exclusions": ["one"],
              "VideoHosts": ["video.example"],
              "IncludeScripts": ["script.js"],
              "WebDriver": ["msedgedriver.exe"]
            }
            """);

        var provider = new CrawlerConfigProvider(
            new CrawlerConfigOptions
            {
                Source = CrawlerConfigSource.File,
                FilePath = configPath
            },
            new RavenDbOptions(),
            new DocumentStore());

        var config = provider.GetConfig();

        Assert.Equal(["one"], config.Exclusions);
        Assert.Equal(["video.example"], config.VideoHosts);
        Assert.Equal(["script.js"], config.IncludeScripts);
        Assert.Equal(["msedgedriver.exe"], config.WebDriver);
    }

    [Fact]
    public void GetConfig_ResolvesRelativePathFromAppContextBaseDirectory()
    {
        var relativeFileName = $"crawlerconfig-{Guid.NewGuid():N}.json";
        var configPath = Path.Combine(AppContext.BaseDirectory, relativeFileName);

        try
        {
            File.WriteAllText(
                configPath,
                """
                {
                  "Exclusions": ["relative"],
                  "VideoHosts": [],
                  "IncludeScripts": [],
                  "WebDriver": []
                }
                """);

            var provider = new CrawlerConfigProvider(
                new CrawlerConfigOptions
                {
                    Source = CrawlerConfigSource.File,
                    FilePath = relativeFileName
                },
                new RavenDbOptions(),
                new DocumentStore());

            var config = provider.GetConfig();

            Assert.Equal(["relative"], config.Exclusions);
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }
}