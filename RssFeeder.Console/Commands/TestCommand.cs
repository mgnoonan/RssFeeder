namespace RssFeeder.Console.Commands;

[Description("Test building the named feed")]
public class TestCommand : OaktonCommand<TestInput>
{
    private readonly IContainer _container;
    private readonly IUnlaunchClient _client;
    private readonly ILogger _log;

    public TestCommand(IContainer container, IUnlaunchClient client, ILogger log)
    {
        _container = container;
        _client = client;
        _log = log;
    }

    public override bool Execute(TestInput input)
    {
        var webUtils = _container.Resolve<IWebUtils>();
        var parser = _container.ResolveNamed<ITagParser>("htmltag-parser");

        string key = "crawler-logic";
        string identity = "test";
        string variation = _client.GetVariation(key, identity);
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        string url = "https://media.townhall.com/townhall/reu/o/2021/224/171327fc-9d62-429d-b6c3-ede53197355a-1110x740.jpg";
        //string url = "https://pjmedia.com/columns/ari-j-kaufman/2021/08/12/what-did-san-francisco-expect-when-it-elected-the-progeny-of-of-militant-marxist-terrorists-n1468973";
        var response = webUtils.TrySaveUrlToDisk(url, "hash", "test.jpg");

        _log.Information("Response = {response}", response);

        // Just telling the OS that the command
        // finished up okay
        return true;
    }
}
