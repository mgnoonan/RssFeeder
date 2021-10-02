using System;
using Autofac;
using RssFeeder.Models;

namespace RssFeeder.Console
{
    public interface IRssBootstrap
    {
        void Initialize();
        void Start(IContainer container, RssFeed feed);
        void Export(IContainer container, RssFeed feed, DateTime startDate);
        void Purge(IContainer container, RssFeed feed);
    }
}