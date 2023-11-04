using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Sales.Payments.WebApi.Infrastructure
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSwaggerMiddleware(
            this IApplicationBuilder app
            //IApiVersionDescriptionProvider provider
            )
        {
            //enable swagger json generation
            app.UseSwagger();

            //enable Swagger Web Page
            app.UseSwaggerUI(
                setup =>
                {
                    setup.DocumentTitle = "Payments Point API";
                    setup.EnableDeepLinking();
                    setup.DisplayRequestDuration();
                    setup.SwaggerEndpoint(
                        $"/swagger/v1/swagger.json",
                        $"PaymentsPoint v1"
                        );

                    //var majorVersions = provider.ApiVersionDescriptions
                    //.Select(
                    //    a => a.ApiVersion.MajorVersion ??
                    //    throw new InvalidOperationException("Major Version is requied.")
                    //    )
                    //.Distinct();

                    //foreach (var version in majorVersions)
                    //{
                    //    setup.SwaggerEndpoint(
                    //        $"/swagger/v{version}/swagger.json",
                    //        $"PaymentsPoint v{version}"
                    //        );
                    //}
                }
                );
            return app;
        }

    }
}
