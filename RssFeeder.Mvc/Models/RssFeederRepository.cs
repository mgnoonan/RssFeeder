using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;


namespace RssFeeder.Mvc.Models
{
    public class RssFeederRepository : RepositoryBase<RssFeederRepository>, IRepository<RssFeederRepository>
    {
        public RssFeederRepository(IConfiguration config)
        {
            Endpoint = config["endpoint"];
            Key = config["authKey"];
            DatabaseId = "rssfeeder";
        }

        public override void Init(string collectionId)
        {
            if (client == null)
                client = new DocumentClient(new Uri(Endpoint), Key);

            CollectionId = collectionId;
            //collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
        }
    }
}
