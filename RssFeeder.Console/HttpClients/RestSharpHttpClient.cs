﻿using System.Threading;
using RestSharp;

namespace RssFeeder.Console.HttpClients;

public class RestSharpHttpClient : IHttpClient
{
    private readonly RestClient _client = new RestClient();

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
        var response = _client.HeadAsync(request).GetAwaiter().GetResult();
        Log.Information("Response status code = {statusCode}", response.StatusCode);
        Log.Information("Headers = {@headers}", response.Headers.ToList());

        return response.Headers
                .Where(x => x.Name.ToLower() == "content-type")
                .Select(x => x.Value.ToString())
                .FirstOrDefault();
    }

    public (HttpStatusCode, string, Uri) GetString(string url)
    {
        Log.Information("RestSharpHttpClient GetString to {url}", url);

        var request = new RestRequest(url);
        var response = _client.GetAsync(request).GetAwaiter().GetResult();
        Log.Information("Response status code = {httpStatusCode} {httpStatusText}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);

        // Poor man's retry since we can't use Polly here
        if ((int)response.StatusCode == 522)
        {
            Thread.Sleep(3);
            response = _client.GetAsync(request).GetAwaiter().GetResult();
            Log.Information("Retry status code = {httpStatusCode} {httpStatusText}, {uri}", (int)response.StatusCode, response.StatusCode, response.ResponseUri);
        }

        return (response.StatusCode, response.Content, response.ResponseUri);
    }
}
