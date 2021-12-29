namespace RssFeeder.Console.Commands;

[Description("Test building the named feed")]
public class TestCommand : OaktonCommand<TestInput>
{
    private readonly IContainer _container;

    public CrawlerConfig CrawlerConfig { get; set; }

    public TestCommand(IContainer container)
    {
        _container = container;
    }

    public override bool Execute(TestInput input)
    {
        var webUtils = _container.Resolve<IWebUtils>();
        var parser = _container.ResolveNamed<ITagParser>("htmltag-parser");

        string url = "https://media.townhall.com/townhall/reu/o/2021/224/171327fc-9d62-429d-b6c3-ede53197355a-1110x740.jpg";
        //string url = "https://pjmedia.com/columns/ari-j-kaufman/2021/08/12/what-did-san-francisco-expect-when-it-elected-the-progeny-of-of-militant-marxist-terrorists-n1468973";
        var response = webUtils.SaveUrlToDisk(url, "hash", "test.jpg");

        Log.Information("Response = {response}", response);
        Log.CloseAndFlush();

        // Just telling the OS that the command
        // finished up okay
        return true;
    }
}
