using System.Threading;
using RestSharp;

namespace RssFeeder.Console.HttpClients;

public class RestSharpHttpClient : IHttpClient
{
    private readonly RestClient _client = new();
    private readonly ILogger _log;

    public RestSharpHttpClient(ILogger log)
    {
        _log = log;
    }

    public byte[] DownloadData(string url)
    {
        _log.Information("RestSharpHttpClient DownloadData to {url}", url);
        var request = new RestRequest(url);
        var response = _client.DownloadData(request);

        return response;
    }

    public (HttpStatusCode, Uri, string) GetContentType(string url)
    {
        _log.Information("RestSharpHttpClient GetContentType to {url}", url);
        var request = new RestRequest(url);
        var response = _client.Execute(request, Method.Head);
        _log.Information("{httpMethod} {uri}, {httpStatusCode} {httpStatusText}", "HEAD", response.ResponseUri, (int)response.StatusCode, response.StatusCode);

        return (response.StatusCode, response.ResponseUri, response.ContentType);
    }

    public (HttpStatusCode, string, Uri, string) GetString(string url)
    {
        _log.Information("RestSharpHttpClient GetString to {url}", url);

        var request = new RestRequest(url);
        var response = _client.Execute(request, Method.Get);
        _log.Information("{httpMethod} {uri}, {httpStatusCode} {httpStatusText}", "GET", response.ResponseUri, (int)response.StatusCode, response.StatusCode);

        // Poor man's retry since we can't use Polly here
        if ((int)response.StatusCode == 522)
        {
            Thread.Sleep(3);
            response = _client.Execute(request, Method.Get);
            _log.Information("Retry status code = {httpStatusCode} {httpStatusText}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);
        }

        return (response.StatusCode, response.Content, response.ResponseUri, response.ContentType);
    }
}
