namespace RssFeeder.Console.Commands;

[Description("Test building the named feed")]
public class TestCommand : OaktonCommand<TestInput>
{
    private readonly IContainer _container;
    private readonly ILogger _log;

    public TestCommand(IContainer container, ILogger log)
    {
        _container = container;
        _log = log;
    }

    public override bool Execute(TestInput input)
    {
        var webUtils = _container.Resolve<IWebUtils>();

        string url = "https://media.townhall.com/townhall/reu/o/2021/224/171327fc-9d62-429d-b6c3-ede53197355a-1110x740.jpg";
        var response = webUtils.TrySaveUrlToDisk(url, "hash", "test.jpg");

        _log.Information("Response = {response}", response);

        // Just telling the OS that the command
        // finished up okay
        return true;
    }
}
