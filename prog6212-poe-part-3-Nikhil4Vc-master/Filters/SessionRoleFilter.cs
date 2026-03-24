using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MonthlyClaims_System.Filters
{
    public class SessionRoleFilter : ActionFilterAttribute
    {
        // Class handles sessions and further security to role based login
        private readonly string _allowedRole;

        public SessionRoleFilter(string allowedRole)
        {
            _allowedRole = allowedRole;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userRole = session.GetString("UserRole");

            if (string.IsNullOrEmpty(userRole) || userRole != _allowedRole)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

