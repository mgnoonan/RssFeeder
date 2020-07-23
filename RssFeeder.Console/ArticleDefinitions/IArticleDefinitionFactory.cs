using System;
using System.Collections.Generic;
using System.Text;
using RssFeeder.Models;

namespace RssFeeder.Console.ArticleDefinitions
{
    public interface IArticleDefinitionFactory
    {
        SiteArticleDefinition Get(string sitename);
    }
}
