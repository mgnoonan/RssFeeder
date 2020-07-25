using System.Collections.Generic;

namespace RssFeeder.Console.Database
{
    public interface IRepository
    {
        List<T> GetDocuments<T>(string collectionName, string sqlQueryText);
        void CreateDocument<T>(string collectionName, T item);
        bool DocumentExists<T>(string collectionName, string sqlQueryText);
        void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
    }
}
