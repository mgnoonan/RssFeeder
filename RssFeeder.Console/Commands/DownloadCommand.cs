namespace RssFeeder.Console.Commands;

public class DownloadCommand : OaktonCommand<DownloadInput>
{
    private readonly ILogger _log;
    private readonly IWebUtils _webUtils;
    private readonly IUtils _utils;

    public DownloadCommand(IWebUtils webUtils, IUtils utils, ILogger log)
    {
        _webUtils = webUtils;
        _utils = utils;
        _log = log;

        // The usage pattern definition here is completely
        // optional
        Usage("Download URL using Web Driver").Arguments(x => x.Url);
    }

    public override bool Execute(DownloadInput input)
    {
        _log.Information("DOWNLOAD_START: Machine: {machineName}", Environment.MachineName);

        // Create the working folder for the collection if it doesn't exist
        string workingFolder = Path.Combine(_utils.GetAssemblyDirectory(), "test-download");
        if (!Directory.Exists(workingFolder))
        {
            Log.Information("Creating folder '{workingFolder}'", workingFolder);
            Directory.CreateDirectory(workingFolder);
        }

        // Create the target filename
        string hash = _utils.CreateMD5Hash(input.Url.ToLower());
        string filename = $"{workingFolder}\\{DateTime.Now:yyyyMMddhhmmss}_{hash}";

        // Download the URL contents using the web driver to the target filename
        (HttpStatusCode statusCode, string content, _, _) = _webUtils.DriverGetString(input.Url);

        if (statusCode == HttpStatusCode.OK)
            _webUtils.SaveContentToDisk(filename + ".html", false, content);

        // Optionally capture a screenshot to the target filename
        if (input.CaptureFlag)
            _webUtils.SaveThumbnailToDisk(input.Url, filename + ".png");

        _log.Information("DOWNLOAD_END: Completed successfully");

        // Just telling the OS that the command
        // finished up okay
        return true;
    }
}
