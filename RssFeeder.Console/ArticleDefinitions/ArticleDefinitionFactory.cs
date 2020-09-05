using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RssFeeder.Console.Database;
using RssFeeder.Models;

namespace RssFeeder.Console.ArticleDefinitions
{
    public class ArticleDefinitionFactory : IArticleDefinitionFactory
    {
        readonly IRepository repository;
        readonly List<SiteArticleDefinition> ArticleDefinitions;

        public ArticleDefinitionFactory(IRepository _repository)
        {
            repository = _repository;

            ArticleDefinitions = repository.GetAllDocuments<SiteArticleDefinition>("site-parsers");
        }

        public SiteArticleDefinition Get(string sitename)
        {
            return ArticleDefinitions.FirstOrDefault(q => q.SiteName == sitename);
        }
    }
}
