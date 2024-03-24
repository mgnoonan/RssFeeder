namespace RssFeeder.Mvc.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class RssController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger _log;

    public RssController(IMediator mediator, ILogger log)
    {
        _mediator = mediator;
        _log = log;
    }

    [HttpGet, HttpHead, Route("{id}"), ResponseCache(Duration = 60 * 60), Produces("application/rss+xml")]
    public async Task<IActionResult> Get(string id)
    {
        try
        {
            var query = new GetFeedQuery(id, new Agent
            {
                BrowserAgent = HttpContext.Request.Headers[HeaderNames.UserAgent].ToString() ?? "",
                IpAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                Referrer = HttpContext.Request.Headers[HeaderNames.Referer].ToString() ?? "",
                QueryString = HttpContext.Request.QueryString,
                Timestamp = DateTime.Now
            });
            var result = await _mediator.Send(query);

            if (string.IsNullOrEmpty(result))
                return NotFound();

            if (Request.Method.Equals("HEAD"))
            {
                _log.Information("HEAD request found {bytes} bytes", result.Length);
                Response.ContentLength = result.Length;
                return Ok();
            }
            else
            {
                return new ContentResult
                {
                    Content = result,
                    ContentType = "application/rss+xml",
                    StatusCode = StatusCodes.Status200OK
                };
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error building syndication items");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
