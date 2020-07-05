using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Cosmos;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.Database
{
    public class CosmosDbRepository : IRepository
    {
        string _databaseName;
        ILogger _log;
        CosmosClient _client = null;

        public CosmosDbRepository(string databaseName, string endpointUrl, string authKey, ILogger log)
        {
            _databaseName = databaseName;
            _log = log;

            _client = new CosmosClient(endpointUrl, authKey);
        }

        public List<T> GetDocuments<T>(string collectionName, Func<T, bool> predicate)
        {
            // Set some common query options
            var queryOptions = new QueryRequestOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            var container = _client.GetContainer(_databaseName, collectionName);
            var result = container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true, requestOptions: queryOptions)
                .Where(predicate)
                .ToList();

            _log.Information("GetAllDocuments returned {count} documents from collection '{collectionName}'", result.Count, collectionName);
            return result;
        }

        public void CreateDocument<T>(string collectionName, T item)
        {
            var container = _client.GetContainer(_databaseName, collectionName);
            var result = container.CreateItemAsync(item).Result;

            if (result.StatusCode != HttpStatusCode.Created)
            {
                _log.Error("Unable to create document for '{@item}'", item);
            }
        }

        //private List<T> GetStaleDocuments<T>(string collectionName, short maximumAgeInDays)
        //{
        //    DateTime targetDate = DateTime.Now.AddDays(-maximumAgeInDays);

        //    // Set some common query options
        //    var queryOptions = new QueryRequestOptions { MaxItemCount = -1 };

        //    // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
        //    // can be completed efficiently and with low latency
        //    var container = _client.GetContainer(_databaseName, collectionName);
        //    return container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true, requestOptions: queryOptions)
        //        .Where(f => f.DateAdded <= targetDate)
        //        .ToList();
        //}

        public bool DocumentExists<T>(string collectionName, Func<T, bool> predicate)
        {
            // Set some common query options
            var queryOptions = new QueryRequestOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            var container = _client.GetContainer(_databaseName, collectionName);
            var result = container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true, requestOptions: queryOptions)
                .Where(predicate);

            return result.Count() > 0;
        }

        public bool DocumentExists(string collectionName, string urlHash)
        {
            // Set some common query options
            var queryOptions = new QueryRequestOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            var container = _client.GetContainer(_databaseName, collectionName);
            var result = container.GetItemLinqQueryable<RssFeedItem>(allowSynchronousQueryExecution: true, requestOptions: queryOptions)
                .Where(f => f.UrlHash == urlHash);

            return result.Count() > 0;
        }

        public void DeleteDocument<T>(string collectionName, string documentID, string partitionKey)
        {
            try
            {
                var pk = string.IsNullOrEmpty(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey);

                var container = _client.GetContainer(_databaseName, collectionName);
                var result = container.DeleteItemAsync<T>(documentID, pk).Result;

                if (result.StatusCode != HttpStatusCode.NoContent)
                {
                    _log.Warning("Unable to delete document '{documentID}' from collection '{collectionName}'", documentID, collectionName);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error deleting document '{documentID}' from collection '{collectionName}'", documentID, collectionName);
            }
        }
    }
}
