using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;

namespace RssFeeder.Mvc
{
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
}
