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
        private const string _collectionName = "site-parsers";

        public ArticleDefinitionFactory(IRepository _repository)
        {
            repository = _repository;

            repository.EnsureDatabaseExists(_collectionName, true);

            ArticleDefinitions = repository.GetAllDocuments<SiteArticleDefinition>(_collectionName);
        }

        public SiteArticleDefinition Get(string sitename)
        {
            return ArticleDefinitions.FirstOrDefault(q => q.SiteName == sitename);
        }
    }
}
