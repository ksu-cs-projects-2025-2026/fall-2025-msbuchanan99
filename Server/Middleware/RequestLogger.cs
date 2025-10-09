namespace Server.Middleware
{
    public class RequestLogger
    {
        private readonly ILogger<RequestLogger> _logger;
        private readonly RequestDelegate _next;

        public RequestLogger(ILogger<RequestLogger> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation(
                "Incoming Request: {Method} {Path} {QueryString} from {IP}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Connection.RemoteIpAddress
            );

            var startTime = DateTime.UtcNow;

            try
            {
                await _next(context);
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Completed Request: {Method} {Path} responded {StatusCode} in {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds
                );
            }
        }
    }

    //Extension method for easy registration
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLogger>();
        }
    }
}
