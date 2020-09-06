namespace RssFeeder.Console.Database
{
    public interface IExportRepository
    {
        void UpsertDocument<T>(string collectionName, T item);
    }
}
