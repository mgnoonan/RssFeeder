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

        string url = "https://nationalpost.com/pmn/entertainment-pmn/king-charles-vows-to-serve-his-nation-as-britain-mourns-late-queen";
        var response = webUtils.WebDriverUrlToDisk(url, "test.html");

        Log.Information("Response = {response}", response);
        Log.CloseAndFlush();

        // Just telling the OS that the command
        // finished up okay
        return true;
    }
}
