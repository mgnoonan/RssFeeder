namespace RssFeeder.Console.Exporters;

public class ExportTemplates
{
    public const string MetaDataTemplate = @"
<p>
    The post <a href=""$item.Url$"">$item.Title$</a> captured from <a href=""$feed.Url$"">$feed.Title$</a> $item.LinkLocation$ on $item.DateAdded$ UTC.
</p>
<hr />
<p>
    <small>
    <ul>
        <li><strong>site_name:</strong> $item.SiteName$</li>
        <li><strong>host:</strong> $item.HostName$</li>
        <li><strong>url:</strong> <a href=""$item.Url$"">$item.Url$</a></li>
        <li><strong>captured:</strong> $item.DateAdded$ UTC</li>
        <li><strong>hash:</strong> $item.UrlHash$</li>
        <li><strong>location:</strong> $item.LinkLocation$</li>
    </ul>
    </small>
</p>
";

    public const string ExtendedTemplate = @"<img src=""$item.ImageUrl$"" />
<h3>$item.Subtitle$</h3>
$ArticleText$
" + MetaDataTemplate;

    public const string VideoTemplate = @"<iframe class=""{class}"" width=""$item.VideoWidth$"" height=""$item.VideoHeight$"" src=""$item.VideoUrl$"" frameborder=""0"" allow=""{allow}"" allowfullscreen></iframe>
<h3>$item.Subtitle$</h3>
$ArticleText$
" + MetaDataTemplate;

    public const string GraphicTemplate = @"<img src=""$item.ImageUrl$"" />
" + MetaDataTemplate;

    public const string BasicTemplate = @"<h3>$item.Title$</h3>
<p><a href=""$item.Url$"">Click here to read the full article</a></p>
" + MetaDataTemplate;
}
