using Microsoft.Extensions.DependencyInjection;

namespace RssFeeder.Console;

/// <summary>
/// .NET keyed services implementation of IFeedBuilderResolver.
/// Resolves named feed builder registrations using the IServiceProvider keyed service API.
/// </summary>
internal sealed class KeyedServiceFeedBuilderResolver : IFeedBuilderResolver
{
    private readonly IServiceProvider _serviceProvider;

    public KeyedServiceFeedBuilderResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IRssFeedBuilder Resolve(string builderKey)
    {
        return _serviceProvider.GetRequiredKeyedService<IRssFeedBuilder>(builderKey);
    }
}
