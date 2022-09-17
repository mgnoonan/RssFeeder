namespace RssFeeder.Console.Commands;

public class DownloadCommand : OaktonCommand<DownloadInput>
{
    private readonly IContainer _container;
    private readonly ILogger _log;

    public DownloadCommand(IContainer container, ILogger log)
    {
        _container = container;
        _log = log;

        // The usage pattern definition here is completely
        // optional
        Usage("Download URL using Web Driver").Arguments(x => x.Url);
    }

    public override bool Execute(DownloadInput input)
    {
        _log.Information("DOWNLOAD_START: Machine: {machineName}", Environment.MachineName);

        var utils = _container.Resolve<IUtils>();
        var webUtils = _container.Resolve<IWebUtils>();

        // Create the working folder for the collection if it doesn't exist
        string workingFolder = Path.Combine(utils.GetAssemblyDirectory(), "test-download");
        if (!Directory.Exists(workingFolder))
        {
            Log.Information("Creating folder '{workingFolder}'", workingFolder);
            Directory.CreateDirectory(workingFolder);
        }

        // Create the target filename
        string hash = utils.CreateMD5Hash(input.Url.ToLower());
        string filename = $"{workingFolder}\\{DateTime.Now:yyyyMMddhhmmss}_{hash}";

        // Download the URL contents using the web driver to the target filename
        webUtils.WebDriverUrlToDisk(input.Url, filename + ".html");

        // Optionally capture a screenshot to the target filename
        if (input.CaptureFlag)
            webUtils.SaveThumbnailToDisk(input.Url, filename + ".png");

        _log.Information("DOWNLOAD_END: Completed successfully");

        // Just telling the OS that the command
        // finished up okay
        return true;
    }
}
