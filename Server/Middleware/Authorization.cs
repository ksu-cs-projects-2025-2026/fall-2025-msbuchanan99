using System.Security.Claims;

namespace Server.Middleware
{
    public class Authorization
    {
        private readonly ILogger<Authorization> _logger;
        private readonly RequestDelegate _next;

        public Authorization(ILogger<Authorization> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
            var username = isAuthenticated ? context.User.Identity.Name : "Anonymous";
            var roles = context.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            _logger.LogInformation(
                "Authorization: User='{Username}', Roles=[{Roles}], Authenticated={IsAuthenticated}, Path={Path}",
                username,
                string.Join(", ", roles),
                isAuthenticated,
                context.Request.Path
            );

            await _next(context);

            // Log authorization failures
            if (context.Response.StatusCode == 401)
            {
                _logger.LogWarning(
                    "UNAUTHORIZED (401): Anonymous user attempted to access {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path
                );
            }
            else if (context.Response.StatusCode == 403)
            {
                _logger.LogWarning(
                    "FORBIDDEN (403): User '{Username}' with roles [{Roles}] denied access to {Method} {Path}",
                    username,
                    string.Join(", ", roles),
                    context.Request.Method,
                    context.Request.Path
                );
            }
            else if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                _logger.LogInformation(
                    "AUTHORIZED: User '{Username}' successfully accessed {Method} {Path}",
                    username,
                    context.Request.Method,
                    context.Request.Path
                );
            }
        }
    }

    public static class AuthorizationExtensions
    {
        public static IApplicationBuilder UseAuthorizationLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Authorization>();
        }
    }
}
