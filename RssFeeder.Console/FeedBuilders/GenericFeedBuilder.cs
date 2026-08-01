namespace RssFeeder.Console.FeedBuilders;

internal class GenericFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private List<FeedSection> _sections = new();

    public GenericFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
    {
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        _logLevel = Serilog.Events.LogEventLevel.Debug;
        _sections = feed.Sections ?? new List<FeedSection>();

        return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
    {
        Initialize(feedUrl, feedFilters, html);
        var items = GenerateRssFeedItemList();
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList()
    {
        var list = new List<RssFeedItem>();
        if (_sections.Count == 0)
        {
            return list;
        }

        foreach (var section in _sections)
        {
            if (section is null)
            {
                continue;
            }

            if (section.OnlyIfEmpty && list.Count > 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(section.ContainerSelector) || string.IsNullOrWhiteSpace(section.LinkSelector))
            {
                _log.Warning("Invalid section config in feed parser. SectionName={sectionName}, ContainerSelector={containerSelector}, LinkSelector={linkSelector}", section.SectionName, section.ContainerSelector, section.LinkSelector);
                continue;
            }

            if (section.SectionNames is { Count: > 0 })
            {
                ParseContainerMappedSection(section, list);
                continue;
            }

            GetNodeLinks(
                section.SectionName ?? string.Empty,
                section.ContainerSelector,
                section.TextSelector ?? string.Empty,
                section.LinkSelector,
                list,
                section.FilterDuplicates,
                section.StopHash ?? string.Empty,
                section.MaxItems);
        }

        return list;
    }

    private void ParseContainerMappedSection(FeedSection section, List<RssFeedItem> list)
    {
        var containers = _document.QuerySelectorAll(section.ContainerSelector);
        if (containers is null)
        {
            _log.Warning("Containers not found {containerSelector}", section.ContainerSelector);
            return;
        }

        for (int i = 0; i < containers.Length; i++)
        {
            string sectionName = i < section.SectionNames.Count && !string.IsNullOrWhiteSpace(section.SectionNames[i])
                ? section.SectionNames[i]
                : section.SectionName ?? string.Empty;
            int count = 1;

            GetNodeLinks(
                containers[i],
                sectionName,
                section.TextSelector ?? string.Empty,
                section.LinkSelector,
                list,
                section.FilterDuplicates,
                ref count,
                section.StopHash ?? string.Empty,
                section.MaxItems);
        }
    }
}