﻿using RestSharp;
using Serilog;

namespace RssFeeder.Console.WebCrawlers
{
    public class RestSharpWebCrawler : IWebCrawler
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

            return response.Headers
                    .Where(x => x.Name == "Content-Type")
                    .Select(x => x.Value.ToString())
                    .FirstOrDefault();
        }

        public string GetString(string url)
        {
            Log.Information("Crawler GetString to {url}", url);
            var request = new RestRequest(url, DataFormat.None);
            var response = _client.Get(request);

            return response.Content;
        }
    }
}