using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;

namespace RssFeeder.Console.HttpClients;

public class DotNetHttpClient : IHttpClient
{
    private const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.40 Safari/537.36 Edg/92.0.902.8";
    private static HttpClient _client;

    public DotNetHttpClient()
    {
        if (_client is null)
        {
            _client = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                SslOptions = new SslClientAuthenticationOptions
                {
                    AllowRenegotiation = true,
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
                },
                UseCookies = true
            });

            _client.DefaultRequestHeaders.Add("user-agent", USERAGENT);
        }
    }

    public (HttpStatusCode, string, Uri) GetString(string url)
    {
        return (HttpStatusCode.OK, GetStringAsync(url).GetAwaiter().GetResult(), default);
    }

    private Task<string> GetStringAsync(string url)
    {
        Log.Information("Crawler GetStringAsync to {url}", url);
        return _client.GetStringAsync(url);
    }

    public byte[] DownloadData(string url)
    {
        var response = GetAsync(url).GetAwaiter().GetResult();
        Log.Information("Response status code = {statusCode}", response.StatusCode);
        response.EnsureSuccessStatusCode();

        // Check that the remote file was found. The ContentType
        // check is performed since a request for a non-existent
        // image file might be redirected to a 404-page, which would
        // yield the StatusCode "OK", even though the image was not
        // found.
        string contentType = response.Content.Headers.ContentType.ToString();
        if (!contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unexpected content type {contentType}");
        }

        return response.Content.ReadAsByteArrayAsync().Result;
    }

    private Task<HttpResponseMessage> GetAsync(string url)
    {
        Log.Information("Crawler GetAsync to {url}", url);
        return _client.GetAsync(url);
    }

    public string GetContentType(string url)
    {
        Log.Information("Crawler GetContentType to {url}", url);
        HttpResponseMessage response = _client.Send(new HttpRequestMessage(HttpMethod.Head, url));
        Log.Information("Response status code = {statusCode}", response.StatusCode);
        response.EnsureSuccessStatusCode();

        return response.Content.Headers.ContentType.ToString();
    }
}
