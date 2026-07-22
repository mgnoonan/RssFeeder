namespace RssFeeder.Console;

/// <summary>
/// Abstraction for resolving named ITagParser implementations by key.
/// This interface isolates the runtime named-resolution seam, enabling:
/// 1. Easier testing (can mock or provide test implementations)
/// 2. Future migration to .NET 8+ keyed services without changing callers
/// 3. Removal of IContainer coupling from ArticleParser and ParseCommand
/// </summary>
public interface ITagParserResolver
{
    /// <summary>
    /// Resolves a named tag parser implementation.
    /// </summary>
    /// <param name="parserKey">The registered name (e.g., "adaptive-parser", "generic-parser")</param>
    /// <returns>An initialized ITagParser instance</returns>
    /// <exception cref="System.ComponentModel.Composition.ImportCardinalityMismatchException">
    /// If the key is not registered or if the registration cannot be satisfied
    /// </exception>
    ITagParser Resolve(string parserKey);
}
