using JetBrains.Annotations;
using Sales.Payments.WebApi.Infrastructure;
using System.Text;

namespace Sales.Payments.WebApi.Middleware
{
    public class RequestResponseLogging
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<RequestResponseLogging> _logger;

        public RequestResponseLogging(RequestDelegate next,ILogger<RequestResponseLogging> logger, IHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        [UsedImplicitly]
        public async Task InvokeAsync([NotNull] HttpContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.Request;
            var requestPath = request.Path.ToUriComponent();
            var requestUrl = new StringBuilder(128)
                .Append(request.Scheme)
                .Append("://")
                .Append(request.Host.ToUriComponent())
                .Append(request.PathBase.ToUriComponent())
                .Append(request.Path.ToUriComponent())
                .Append(request.QueryString.Value)
                .ToString();

            // Capture request contect info that serillog doesnot include automatically

            using var _ = _logger.BeginScope(
                new Dictionary<string, object>
                {
                    { "RequestId", context.TraceIdentifier },
                    { "RequestMethod", request.Method },
                    { "RequestPath", requestPath },
                    { "RequestUrl", requestPath },
                    { "RequestClientIp", context.Connection?.RemoteIpAddress?.ToString() },
                    { "RequestUserAgent", request.Headers["User-Agent"].ToString() },

                }
                );

            _logger.LogInformation("Start {RequestMethod:l} {RequestPath:l}", request.Method, requestPath);

            if (_environment.IsStaging() && !(request.Body is null))
            {
                request.EnableBuffering();

                await using var memoryStream = new MemoryStream();
                await request.Body.CopyToAsync(memoryStream);
                request.Body.Seek(0,SeekOrigin.Begin);
                var body = Encoding.UTF8.GetString(memoryStream.ToArray());

                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogTrace("Request body: {RequestBody}", body);
                }


            }

            void RequestEnd(object s, OnBlockEndEventArgs e) =>
                _logger.LogInformation(
                    "End {RequestMethod:l} {RequestPath:l} with status {RequestStatusCode} in {RequestDuration:F2} ms",
                    request.Method,
                    requestPath,
                    context.Response.StatusCode,
                    e.Elapsed.TotalMilliseconds
                    );

            using (new BlockTimer(RequestEnd))
            {
                await _next(context);
            }
                
        }

    }
}
