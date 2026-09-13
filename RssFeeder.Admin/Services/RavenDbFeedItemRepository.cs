using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RssFeeder.Models;

namespace RssFeeder.Admin.Services;

public interface IFeedItemRepository
{
    Task<IReadOnlyList<RssFeedItem>> GetFeedItemsAsync(int take = 200, CancellationToken cancellationToken = default);

    Task<RssFeedItem?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<RssFeedItem> SaveAsync(RssFeedItem item, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class RavenDbFeedItemRepository : IFeedItemRepository, IDisposable
{
    private readonly IDocumentStore _store;
    private readonly RavenDbOptions _options;

    public RavenDbFeedItemRepository(IOptions<RavenDbOptions> options)
    {
        _options = options.Value;
        _store = new DocumentStore
        {
            Urls = [ _options.Url ],
            Database = _options.Database
        }.Initialize();

        EnsureDatabaseExists();
    }

    public void Dispose() => _store.Dispose();

    public async Task<IReadOnlyList<RssFeedItem>> GetFeedItemsAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession(database: _options.Database);

        return await session
            .Query<RssFeedItem>()
            .OrderByDescending(item => item.FeedAttributes.DateAdded)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);
    }

    public async Task<RssFeedItem?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession(database: _options.Database);
        return await session.LoadAsync<RssFeedItem>(id, cancellationToken);
    }

    public async Task<RssFeedItem> SaveAsync(RssFeedItem item, CancellationToken cancellationToken = default)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession(database: _options.Database);

        if (string.IsNullOrWhiteSpace(item.Id))
        {
            await session.StoreAsync(item, cancellationToken);
        }
        else
        {
            await session.StoreAsync(item, item.Id, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession(database: _options.Database);
        session.Delete(id);
        await session.SaveChangesAsync(cancellationToken);
    }

    private void EnsureDatabaseExists()
    {
        if (string.IsNullOrWhiteSpace(_options.Database))
        {
            throw new InvalidOperationException("RavenDB database name is required.");
        }

        try
        {
            _store.Maintenance.ForDatabase(_options.Database).Send(new GetStatisticsOperation());
        }
        catch (DatabaseDoesNotExistException)
        {
            try
            {
                _store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(_options.Database)));
            }
            catch (ConcurrencyException)
            {
                // The database was created after the statistics check.
            }
        }
    }
}

public sealed class RavenDbOptions
{
    public const string SectionName = "RavenDb";

    public string Url { get; set; } = "http://127.0.0.1:8080";

    public string Database { get; set; } = "feed-items";

    public int DefaultTake { get; set; } = 200;
}