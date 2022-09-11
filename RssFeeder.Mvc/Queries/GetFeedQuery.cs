using MediatR;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Queries;

public class GetFeedQuery : IRequest<string>
{
    public string Id { get; init; }
    public Agent Agent { get; init; }

    public GetFeedQuery(string id, Agent agent)
    {
        Id = id;
        Agent = agent;
    }
}
