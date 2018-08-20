using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace RssFeeder.Mvc.Models
{
    public class RepositoryInitializer
    {
        private static DocumentClient client;

        public static void Initialize(IConfiguration config)
        {
            string databaseId = config["database_id"];

            client = new DocumentClient(new Uri(config["endpoint"]), config["authKey"]);
            CreateDatabaseIfNotExistsAsync(databaseId).Wait();

            // Feed Collection
            CreateCollectionIfNotExistsAsync(databaseId, "feeds").Wait();

            // Site Parser collection
            CreateCollectionIfNotExistsAsync(databaseId, "site-parsers").Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync(string databaseId)
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = databaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync(string databaseId, string collectionId, string partitionkey = null)
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    if (string.IsNullOrEmpty(partitionkey))
                    {
                        await client.CreateDocumentCollectionAsync(
                            UriFactory.CreateDatabaseUri(databaseId),
                            new DocumentCollection { Id = collectionId },
                            new RequestOptions { OfferThroughput = 1000 });
                    }
                    else
                    {
                        await client.CreateDocumentCollectionAsync(
                            UriFactory.CreateDatabaseUri(databaseId),
                            new DocumentCollection
                            {
                                Id = collectionId,
                                PartitionKey = new PartitionKeyDefinition
                                {
                                    Paths = new Collection<string> { "/" + partitionkey }
                                }
                            },
                            new RequestOptions { OfferThroughput = 1000 });
                    }

                }
                else
                {
                    throw;
                }
            }
        }
    }
}
