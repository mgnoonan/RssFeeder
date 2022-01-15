using System.Threading;
using Polly;
using RestSharp;

namespace RssFeeder.Console.HttpClients;

public class RestSharpHttpClient : IHttpClient
{
    private readonly RestClient _client = new RestClient();

    public byte[] DownloadData(string url)
    {
        Log.Information("Crawler DownloadData to {url}", url);
        var request = new RestRequest(url, DataFormat.None);
        var response = _client.DownloadData(request, true);

        return response;
    }

    public string GetContentType(string url)
    {
        Log.Information("Crawler GetContentType to {url}", url);
        var request = new RestRequest(url, DataFormat.None);
        var response = _client.Head(request);
        Log.Information("Response status code = {statusCode}", response.StatusCode);

        return response.Headers
                .Where(x => x.Name == "Content-Type")
                .Select(x => x.Value.ToString())
                .FirstOrDefault();
    }

    public (HttpStatusCode, string, Uri) GetString(string url)
    {
        Log.Information("Crawler GetString to {url}", url);

        var request = new RestRequest(url, DataFormat.None);
        var response = _client.Get(request);
        Log.Information("Response status code = {intStatusCode} {statusCode}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);

        // Poor man's retry since we can't use Polly here
        if ((int)response.StatusCode == 522)
        {
            Thread.Sleep(3);
            response = _client.Get(request);
            Log.Information("Retry status code = {intStatusCode} {statusCode}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);
        }

        return (response.StatusCode, response.Content, response.ResponseUri);
    }
}
