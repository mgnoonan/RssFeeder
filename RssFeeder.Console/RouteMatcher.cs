﻿using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace RssFeeder.Console;

internal class RouteMatcher : IRouteMatcher
{
    public RouteValueDictionary Match(string routeTemplate, string requestPath)
    {
        var template = TemplateParser.Parse(routeTemplate);

        var matcher = new TemplateMatcher(template, GetDefaults(template));

        var values = new RouteValueDictionary();

        return matcher.TryMatch(requestPath, values) ? values : null;
    }

    private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
    {
        var result = new RouteValueDictionary();

        foreach (var parameter in parsedTemplate.Parameters)
        {
            if (parameter.DefaultValue != null)
            {
                result.Add(parameter.Name, parameter.DefaultValue);
            }
        }

        return result;
    }
}