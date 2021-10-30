using System;
using System.Collections.Generic;
using System.IO;

namespace RssFeeder.Console.Database
{
    public interface IRepository
    {
        List<T> GetDocuments<T>(string collectionName, string sqlQueryText);
        List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays);
        List<T> GetExportDocuments<T>(string collectionName, string feedId, DateTime startDate);
        List<T> GetAllDocuments<T>(string collectionName);
        void CreateDocument<T>(string collectionName, T item, int expirationDays, string filename, Stream stream, string contentType);
        bool DocumentExists<T>(string collectionName, string feedID, string urlHash);
        void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
        void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true);
    }
}
