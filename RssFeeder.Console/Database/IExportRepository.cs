using System.Collections.Generic;

namespace RssFeeder.Console.Database
{
    public interface IExportRepository
    {
        void UpsertDocument<T>(string collectionName, T item);
        List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays);
        void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
    }
}
