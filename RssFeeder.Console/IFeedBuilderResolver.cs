namespace RssFeeder.Console;

/// <summary>
/// Abstraction for resolving named IRssFeedBuilder implementations by key.
/// This interface isolates the runtime named-resolution seam, enabling:
/// 1. Easier testing (can mock or provide test implementations)
/// 2. Future migration to .NET 8+ keyed services without changing callers
/// 3. Removal of IContainer coupling from WebCrawler
/// </summary>
public interface IFeedBuilderResolver
{
    /// <summary>
    /// Resolves a named feed builder implementation.
    /// </summary>
    /// <param name="builderKey">The registered name (e.g., "drudge-report", "liberty-daily")</param>
    /// <returns>An initialized IRssFeedBuilder instance</returns>
    /// <exception cref="System.ComponentModel.Composition.ImportCardinalityMismatchException">
    /// If the key is not registered or if the registration cannot be satisfied
    /// </exception>
    IRssFeedBuilder Resolve(string builderKey);
}
