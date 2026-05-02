namespace RssFeeder.Console.Database;

public class CrawlerConfigProvider : ICrawlerConfigProvider
{
    private readonly CrawlerConfigOptions _options;
    private readonly RavenDbOptions _ravenDbOptions;
    private readonly IDocumentStore _store;

    public CrawlerConfigProvider(CrawlerConfigOptions options, RavenDbOptions ravenDbOptions, IDocumentStore store)
    {
        _options = options;
        _ravenDbOptions = ravenDbOptions;
        _store = store;
    }

    public CrawlerConfig GetConfig()
    {
        if (_options.Source == CrawlerConfigSource.File)
        {
            var resolvedPath = Path.IsPathRooted(_options.FilePath)
                ? _options.FilePath
                : Path.Combine(AppContext.BaseDirectory, _options.FilePath);

            return JsonConvert.DeserializeObject<CrawlerConfig>(File.ReadAllText(resolvedPath));
        }

        using IDocumentSession session = _store.OpenSession(database: _ravenDbOptions.DatabaseName);
        return session.Advanced.RawQuery<CrawlerConfig>("from CrawlerConfig").First();
    }
}