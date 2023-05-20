using System.Reflection;

namespace RssFeeder.Console.Commands;

[Description("Build the RSS Feeds using the config file")]
public class BuildCommand : OaktonCommand<BuildInput>
{
    private readonly IContainer _container;
    private readonly ILogger _log;

    public BuildCommand(IContainer container, ILogger log)
    {
        _container = container;
        _log = log;

        // The usage pattern definition here is completely
        // optional
        Usage("Configuration File").Arguments(x => x.ConfigFile);
    }

    public override bool Execute(BuildInput input)
    {
        // Zero return value means everything processed normally
        int returnCode = 0;

        // Grab the current assembly name
        var assemblyName = Assembly.GetExecutingAssembly().Location;
        _log.Information("CRAWLER_START: Machine: {machineName} Assembly: {assembly}", Environment.MachineName, assemblyName);

        try
        {
            //var repository = _container.Resolve<IRepository>();
            var crawler = _container.Resolve<IWebCrawler>();
            var utils = _container.Resolve<IUtils>();
            //var webUtils = _container.Resolve<IWebUtils>();

            if (string.IsNullOrWhiteSpace(input.ConfigFile))
            {
                input.ConfigFile = "feed-test.json";
            }

            // Initialize the bootstrap driver
            crawler.Initialize(_container, "feed-items", "drudge-report");

            // Get the directory of the current executable, all config 
            // files should be in this path
            string configFile = Path.Combine(utils.GetAssemblyDirectory(), input.ConfigFile);
            _log.Information("Reading from config file: {configFile}", configFile);

            // Read the options in JSON format
            using StreamReader sr = new StreamReader(configFile);
            string json = sr.ReadToEnd();
            _log.Debug("Options: {@options}", json);

            // Deserialize into our options class
            var feedList = JsonConvert.DeserializeObject<List<RssFeed>>(json);
            var startDate = DateTime.UtcNow;

            var runID = Guid.NewGuid();
            _log.Information("Run ID = {runID}", runID);

            foreach (var feed in feedList)
            {
                using (LogContext.PushProperty("collectionName", feed.CollectionName))
                using (LogContext.PushProperty("runID", runID))
                {
                    try
                    {
                        if (feed.Enabled)
                        {
                            crawler.Crawl(runID, feed);
                            crawler.Export(runID, feed, startDate);
                        }

                        crawler.Purge(feed);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "ERROR: Unable to process feed '{feedTitle}' from '{feedUrl}'", feed.Title, feed.Url);
                    }
                }
            }

            _log.Information("CRAWLER_END: Completed successfully");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error during processing '{message}'", ex.Message);
            returnCode = 250;
        }

        return returnCode == 0;
    }
}
