using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace RssFeeder.Mvc;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddRssFeederMvcConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FeedDefinitionOptions>(configuration.GetSection(FeedDefinitionOptions.SectionName));
        services.AddSingleton<IFeedDefinitionProvider, FileFeedDefinitionProvider>();

        services.Configure<CosmosDbOptions>(configuration.GetSection(CosmosDbOptions.SectionName));
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            return new CosmosClient(options.Account, options.Key);
        });
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var client = serviceProvider.GetRequiredService<CosmosClient>();
            EnsureCosmosContainerExistsAsync(client, options).GetAwaiter().GetResult();
            return new CosmosDbService(client, options.DatabaseName, options.ContainerName);
        });

        return services;
    }

    private static async Task EnsureCosmosContainerExistsAsync(CosmosClient client, CosmosDbOptions options)
    {
        DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(options.DatabaseName);
        await database.Database.CreateContainerIfNotExistsAsync(options.ContainerName, "/HostName");
    }
}