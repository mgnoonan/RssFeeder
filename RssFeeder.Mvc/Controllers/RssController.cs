using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RssFeeder.Mvc.Models;
using RssFeeder.Mvc.Queries;
using Serilog;

namespace RssFeeder.Mvc.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class RssController : ControllerBase
{
    private readonly IMediator _mediator;

    public RssController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet, HttpHead, Route("{id}"), ResponseCache(Duration = 60 * 60), Produces("text/xml")]
    public async Task<IActionResult> Get(string id)
    {
        try
        {
            var query = new GetFeedQuery(id, new Agent
            {
                BrowserAgent = HttpContext.Request.Headers[HeaderNames.UserAgent].ToString() ?? "",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Referrer = HttpContext.Request.Headers[HeaderNames.Referer].ToString() ?? "",
                Timestamp = DateTime.Now
            });
            var result = await _mediator.Send(query);

            if (string.IsNullOrEmpty(result))
                return NotFound();

            if (Request.Method.Equals("HEAD"))
            {
                Log.Information("HEAD request found {bytes} bytes", result.Length);
                Response.ContentLength = result.Length;
                return Ok();
            }
            else
            {
                return new ContentResult
                {
                    Content = result,
                    ContentType = "text/xml",
                    StatusCode = StatusCodes.Status200OK
                };
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error building syndication items");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
