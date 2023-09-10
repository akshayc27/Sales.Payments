using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Newtonsoft.Json.Converters;
using Sales.Payments.WebApi.Infrastructure;

namespace Sales.Payments
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterMvc();
            //services.AddControllers();
            services.AddApiVersioning();
            //services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        //Called automatically to set up the ASP.NET core request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            //IApiVersionDescriptionProvider versionProvider,
            IServiceProvider provider
            )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseSwagger();
                //app.UseSwaggerUI();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseSwaggerMiddleware();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }

    public static class IServiceCollectionExtensions
    {
        private const string ExpiredTokenHandler = "Token-Expired";

        public static IServiceCollection RegisterMvc(this IServiceCollection services)
        {
            services.AddCors(

                options =>
                {
                    options.AddDefaultPolicy(

                        builder =>
                        {
                            builder.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders(ExpiredTokenHandler, "WWW-Authenticate");
                        });
                });

            services.AddControllers(
                options =>
                {

                    //Prevent validation attributes on odel from affecting model state.
                    //Doest not affect model binding errors (e.g. malformed json)
                    options.ModelValidatorProviders.Clear();

                    //When the modl binding source is request body, treat  an empty
                    //body as an error
                    options.AllowEmptyInputInBodyModelBinding = false;
                }
                )
                .AddNewtonsoftJson(
                options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                }
                )
                ;

            services.AddApiVersioning(
                options =>
                {
                    options.AssumeDefaultVersionWhenUnspecified = false;
                    options.RegisterMiddleware = true;
                    options.UseApiBehavior = true;
                    //options.ErrorResponses = new();
                }
                );

            return services;

        }
    }

}
