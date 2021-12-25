namespace RssFeeder.Console.Database;

public interface IExportRepository
{
    void UpsertDocument<T>(string collectionName, T item);
    List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays);
    void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
    void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true);
}
