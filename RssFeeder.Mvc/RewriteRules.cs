using System.Net;

namespace RssFeeder.Mvc;
public class RewriteRules
{
    public static void RedirectPhpFileRequests(RewriteContext context)
    {
        var request = context.HttpContext.Request;

        if (request.Path.Value.EndsWith(".php", StringComparison.OrdinalIgnoreCase))
        {
            var response = context.HttpContext.Response;
            response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Result = RuleResult.EndResponse;
            response.Headers[HeaderNames.Location] = "/";
        }
    }

    public static void RedirectWordpressRequests(RewriteContext context)
    {
        var request = context.HttpContext.Request;

        if (request.Path.Value.Contains("wp-admin", StringComparison.OrdinalIgnoreCase) ||
            request.Path.Value.Contains("wp-content", StringComparison.OrdinalIgnoreCase) ||
            request.Path.Value.Contains("wp-includes", StringComparison.OrdinalIgnoreCase) ||
            request.Path.Value.Contains("wlwmanifest.xml", StringComparison.OrdinalIgnoreCase))
        {
            var response = context.HttpContext.Response;
            response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Result = RuleResult.EndResponse;
            response.Headers[HeaderNames.Location] = "/";
        }
    }
}
