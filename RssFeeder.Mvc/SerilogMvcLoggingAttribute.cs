﻿using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SerilogMvcLoggingAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var diagnosticContext = context.HttpContext.RequestServices.GetService<IDiagnosticContext>();
        diagnosticContext.Set("ActionName", context.ActionDescriptor.DisplayName);
        diagnosticContext.Set("ActionId", context.ActionDescriptor.Id);
    }
}