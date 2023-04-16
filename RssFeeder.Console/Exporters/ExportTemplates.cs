namespace RssFeeder.Console.Exporters;

public class ExportTemplates
{
    public const string MetaDataTemplate = @"
<p>
    <small>The post <a href=""$item.Url$"">$item.Title$</a> captured from <a href=""$feed.Url$"">$feed.Title$</a> $item.LinkLocation$ on $item.DateAdded$ UTC.</small>
</p>
<p style=""text-align: center"">
    <a style=""background-color: #008CBA;border: none;color: white;padding: 8px;text-align: center;text-decoration: none;display: inline-block;font-size: 14px;margin: 4px 2px;cursor: pointer;border-radius: 8px;"" href=""https://rssfeedermvc.azurewebsites.net/"">Browse more feeds built by RssFeeder</a>
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

    public const string HtmlVideoTemplate = @"<iframe class=""{class}"" width=""$item.VideoWidth$"" height=""$item.VideoHeight$"" src=""$item.VideoUrl$"" frameborder=""0"" allow=""{allow}"" allowfullscreen></iframe>
<h3>$item.Subtitle$</h3>
$ArticleText$
" + MetaDataTemplate;

    public const string Mp4VideoTemplate = @"<video class=""{class}"" width=""$item.VideoWidth$"" height=""$item.VideoHeight$"" controls=""controls""><source src=""$item.VideoUrl$"" type=""video/mp4""></video>
<h3>$item.Subtitle$</h3>
$ArticleText$
" + MetaDataTemplate;

    public const string GraphicTemplate = @"<img src=""$item.ImageUrl$"" />
" + MetaDataTemplate;

    public const string BasicTemplate = @"<h3>$item.Title$</h3>
<p><a href=""$item.Url$"">Click here to read the full article</a></p>
" + MetaDataTemplate;

    public const string BasicPlusTemplate = @"<img src=""$item.ImageUrl$"" />
<h3>$item.Title$</h3>
<p><a href=""$item.Url$"">Click here to read the full article</a></p>
" + MetaDataTemplate;
}
