using System.Collections.Generic;

namespace RssFeeder.Console.Database
{
    public interface IRepository
    {
        List<T> GetDocuments<T>(string collectionName, string sqlQueryText);
        List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays);
        List<T> GetAllDocuments<T>(string collectionName);
        void CreateDocument<T>(string collectionName, T item);
        bool DocumentExists<T>(string collectionName, string feedID, string urlHash);
        void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
    }
}
