using Microsoft.Extensions.DependencyInjection;

namespace RssFeeder.Console;

/// <summary>
/// .NET keyed services implementation of ITagParserResolver.
/// Resolves named tag parser registrations using the IServiceProvider keyed service API.
/// </summary>
internal sealed class KeyedServiceTagParserResolver : ITagParserResolver
{
    private readonly IServiceProvider _serviceProvider;

    public KeyedServiceTagParserResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ITagParser Resolve(string parserKey)
    {
        return _serviceProvider.GetRequiredKeyedService<ITagParser>(parserKey);
    }
}
