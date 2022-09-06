using System.Threading;
using RestSharp;

namespace RssFeeder.Console.HttpClients;

public class RestSharpHttpClient : IHttpClient
{
    private readonly RestClient _client = new();

    public byte[] DownloadData(string url)
    {
        Log.Information("RestSharpHttpClient DownloadData to {url}", url);
        var request = new RestRequest(url);
        var response = _client.DownloadDataAsync(request).GetAwaiter().GetResult();

        return response;
    }

    public string GetContentType(string url)
    {
        Log.Information("RestSharpHttpClient GetContentType to {url}", url);
        var request = new RestRequest(url);
        var response = _client.Execute(request, Method.Head);
        Log.Information("Response status code = {httpStatusCode} {httpStatusText}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);

        return response.ContentType;
    }

    public (HttpStatusCode, string, Uri, string) GetString(string url)
    {
        Log.Information("RestSharpHttpClient GetString to {url}", url);

        var request = new RestRequest(url);
        var response = _client.Execute(request, Method.Get);
        Log.Information("Response status code = {httpStatusCode} {httpStatusText}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);

        // Poor man's retry since we can't use Polly here
        if ((int)response.StatusCode == 522)
        {
            Thread.Sleep(3);
            response = _client.Execute(request, Method.Get);
            Log.Information("Retry status code = {httpStatusCode} {httpStatusText}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);
        }

        return (response.StatusCode, response.Content, response.ResponseUri, response.ContentType);
    }
}
