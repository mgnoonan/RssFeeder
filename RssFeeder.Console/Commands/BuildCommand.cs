namespace RssFeeder.Console.Commands;

[Description("Build the RSS Feeds using the config file")]
public class BuildCommand : OaktonCommand<BuildInput>
{
    private readonly IContainer _container;

    public BuildCommand(IContainer container)
    {
        _container = container;

        // The usage pattern definition here is completely
        // optional
        Usage("Configuration File").Arguments(x => x.ConfigFile);
    }

    public override bool Execute(BuildInput input)
    {
        // Zero return value means everything processed normally
        int returnCode = 0;

        try
        {
            List<RssFeed> feedList;
            var repository = _container.Resolve<IRepository>();
            var crawler = _container.Resolve<IWebCrawler>();
            var utils = _container.Resolve<IUtils>();
            var webUtils = _container.Resolve<IWebUtils>();

            if (string.IsNullOrWhiteSpace(input.ConfigFile))
            {
                input.ConfigFile = "feed-test.json";
            }

            // Initialze the bootstrap driver
            crawler.Initialize(_container, "feed-items", "drudge-report");

            // Get the directory of the current executable, all config 
            // files should be in this path
            string configFile = Path.Combine(utils.GetAssemblyDirectory(), input.ConfigFile);
            Log.Logger.Information("Reading from config file: {configFile}", configFile);

            // Read the options in JSON format
            using StreamReader sr = new StreamReader(configFile);
            string json = sr.ReadToEnd();
            Log.Logger.Information("Options: {@options}", json);

            // Deserialize into our options class
            feedList = JsonConvert.DeserializeObject<List<RssFeed>>(json);
            var startDate = DateTime.UtcNow;
            
            var runID = Guid.NewGuid();
            Log.Information("Run ID = {runID}", runID);

            foreach (var feed in feedList)
            {
                try
                {
                    using (LogContext.PushProperty("collectionName", feed.CollectionName))
                    {
                        if (feed.Enabled)
                        {
                            crawler.Crawl(runID, feed);
                            crawler.Export(runID, feed, startDate);
                        }

                        crawler.Purge(feed);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "ERROR: Unable to process feed '{feedTitle}' from '{feedUrl}'", feed.Title, feed.Url);
                }
            }

            Log.Information("END: Completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during processing '{message}'", ex.Message);
            returnCode = 250;
        }
        finally
        {
            Log.CloseAndFlush();
        }

        return returnCode == 0;
    }
}
