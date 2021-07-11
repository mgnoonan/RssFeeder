﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RssFeeder.Console.Database;
using RssFeeder.Models;

namespace RssFeeder.Console.ArticleDefinitions
{
    public class ArticleDefinitionFactory : IArticleDefinitionFactory
    {
        private readonly IRepository repository;
        private bool isInitialized = false;
        private List<SiteArticleDefinition> ArticleDefinitions;
        private const string _collectionName = "site-parsers";

        public ArticleDefinitionFactory(IRepository _repository)
        {
            repository = _repository;
        }

        public void Initialize()
        {
            repository.EnsureDatabaseExists(_collectionName, true);

            ArticleDefinitions = repository.GetAllDocuments<SiteArticleDefinition>(_collectionName);
            isInitialized = true;
        }

        public SiteArticleDefinition Get(string sitename)
        {
            if (!isInitialized) Initialize();

            return ArticleDefinitions.FirstOrDefault(q => q.SiteName == sitename);
        }
    }
}
