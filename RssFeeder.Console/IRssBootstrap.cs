using Autofac;
using RssFeeder.Models;
using StackExchange.Profiling;

namespace RssFeeder.Console
{
    public interface IRssBootstrap
    {
        void Start(IContainer container, MiniProfiler profiler, RssFeed feed);
        void Export(IContainer container, MiniProfiler profiler, RssFeed feed);
    }
}