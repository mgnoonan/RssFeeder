namespace RssFeeder.Console.Database;

public class NullRepository : IRepository, IExportRepository
{
    private static readonly CrawlerConfig _emptyConfig = new()
    {
        Exclusions = Array.Empty<string>(),
        VideoHosts = Array.Empty<string>(),
        IncludeScripts = Array.Empty<string>(),
        WebDriver = Array.Empty<string>()
    };

    public CrawlerConfig Config => _emptyConfig;

    public List<T> GetDocuments<T>(string collectionName, string sqlQueryText, Dictionary<string, object> parameters, bool addWait)
    {
        throw BuildAuditOnlyException();
    }

    public List<T> GetExportDocuments<T>(string collectionName, string feedId, Guid runID)
    {
        throw BuildAuditOnlyException();
    }

    public List<T> GetAllDocuments<T>(string collectionName)
    {
        throw BuildAuditOnlyException();
    }

    public void SaveDocument<T>(string collectionName, T item, int expirationDays)
    {
        throw BuildAuditOnlyException();
    }

    public void SaveDocument<T>(string collectionName, T item, int expirationDays, string filename, Stream stream, string contentType)
    {
        throw BuildAuditOnlyException();
    }

    public bool DocumentExists<T>(string collectionName, string feedID, string urlHash)
    {
        throw BuildAuditOnlyException();
    }

    void IRepository.DeleteDocument<T>(string collectionName, string documentID, string partitionKey)
    {
        throw BuildAuditOnlyException();
    }

    void IExportRepository.DeleteDocument<T>(string collectionName, string documentID, string partitionKey)
    {
        throw BuildAuditOnlyException();
    }

    public void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true)
    {
        // Intentionally no-op for audit mode.
    }

    public List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays)
    {
        throw BuildAuditOnlyException();
    }

    public void UpsertDocument<T>(string collectionName, T item)
    {
        throw BuildAuditOnlyException();
    }

    private static NotSupportedException BuildAuditOnlyException()
    {
        return new NotSupportedException("NullRepository is audit-only. This operation is not available outside AUDIT mode.");
    }
}