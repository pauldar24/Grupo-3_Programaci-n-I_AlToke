using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GRUPAL.Filters;

/// <summary>
/// Marks an action to skip admin authentication check.
/// Apply to Login actions that should be accessible without session.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SkipAdminAuthAttribute : Attribute { }

/// <summary>
/// Action filter that requires an active admin session.
/// Redirects to Admin/Login if no active session exists.
/// Apply [TypeFilter(typeof(AdminAuthFilter))] to the AdminController class.
/// </summary>
public class AdminAuthFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Allow actions marked with [SkipAdminAuth]
        var hasSkip = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is SkipAdminAuthAttribute);
        if (hasSkip) return;

        var isAdmin = context.HttpContext.Session.GetString("IsAdmin");
        if (isAdmin != "true")
        {
            context.Result = new RedirectToActionResult("Login", "Admin", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
