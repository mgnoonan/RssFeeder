namespace RssFeeder.Console.HttpClients;

public interface IHttpClient
{
    (HttpStatusCode, string, Uri, string) GetString(string url);
    byte[] DownloadData(string url);
    (HttpStatusCode, Uri, string) GetContentType(string url);
}
