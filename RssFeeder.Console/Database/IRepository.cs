namespace RssFeeder.Console.Database;

public interface IRepository
{
    CrawlerConfig Config { get; }
    List<T> GetDocuments<T>(string collectionName, string sqlQueryText, Dictionary<string, object> parameters, bool addWait);
    List<T> GetExportDocuments<T>(string collectionName, string feedId, Guid runID);
    List<T> GetAllDocuments<T>(string collectionName);
    void SaveDocument<T>(string collectionName, T item, int expirationDays);
    void SaveDocument<T>(string collectionName, T item, int expirationDays, string filename, Stream stream, string contentType);
    bool DocumentExists<T>(string collectionName, string feedID, string urlHash);
    void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
    void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true);
    List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays);
}
