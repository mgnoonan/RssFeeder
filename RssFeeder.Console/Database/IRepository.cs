﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RssFeeder.Console.Database
{
    public interface IRepository
    {
        List<T> GetDocuments<T>(string collectionName, Func<T, bool> predicate);
        void CreateDocument<T>(string collectionName, T item);
        bool DocumentExists<T>(string collectionName, Expression<Func<T, bool>> predicate);
        bool DocumentExists(string collectionName, string urlHash);
        void DeleteDocument<T>(string collectionName, string documentID, string partitionKey);
    }
}
