using Microsoft.AspNetCore.Routing;

namespace RssFeeder.Console;

internal interface IRouteMatcher
{
    RouteValueDictionary Match(string routeTemplate, string requestPath);
}