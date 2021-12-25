namespace RssFeeder.Console.HttpClients;

public interface IHttpClient
{
    (string, Uri) GetString(string url);
    byte[] DownloadData(string url);
    string GetContentType(string url);
}
