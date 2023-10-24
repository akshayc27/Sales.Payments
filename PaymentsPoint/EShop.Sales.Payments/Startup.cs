using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Converters;
using Sales.Payments.WebApi.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Sales.Payments
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterMvc();
            services.RegisterAuthetication(_configuration);
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
            app.UseCors();
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }

    public static class IServiceCollectionExtensions
    {
        private const string ExpiredTokenHeader = "Token-Expired";


        public static IServiceCollection RegisterAuthetication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtConfigSection = configuration.GetSection("JwtSettings");
            var jwtSettings = new JwtSettings();
            jwtConfigSection.Bind(jwtSettings);

            services.Configure<JwtSettings>(jwtConfigSection);

            var authBuilder = services.AddAuthentication(
                options => { options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; }
                ).AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Audience = jwtSettings.Audience;
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        RequireSignedTokens = true,
                        ValidateLifetime = true
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if(context.Exception is SecurityTokenExpiredException)
                            {
                                context.Response.Headers.Add(ExpiredTokenHeader, "true");
                            }

                            return Task.CompletedTask;
                        }
                    };
                }
                );

            var authConfigSection = configuration.GetSection("Authentication");
            var authSettings = new AuthenticationSettings();
            authConfigSection.Bind(authSettings);

            services.AddOptions<AuthenticationSettings>()
                .Bind(authConfigSection)
                .ValidateDataAnnotations()
                .Validate(
                    s => s.ApiKeys.All(k => k.Keys.Any()),
                    "All api keys must have at least one key registered."
                    );
            var registeredApiKeys = authSettings.ApiKeys.ToDictionary(
                k => $"{nameof(Policies.ApiKey)}-{k.Name}",
                k => k.Keys
                );

            var apiKeyPolicies = typeof(Policies.ApiKey).GetFields()
                .Where(f => f.IsLiteral)
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();

            foreach (var name in apiKeyPolicies)
            {
                if (!registeredApiKeys.TryGetValue(name,out var keys))
                {
                    throw new InvalidOperationException($"Authentication Policy {name} does not have any api keys registered");

                }

                authBuilder.AddScheme<ApiKeyAuthenticationSchemeOptions,ApiKeyAuthenticationHandler>(
                    name,
                    options => { options.ApiKeys = keys; }
                    );
            }

            services.AddAuthorization(
                options =>
                {
                    options.AddPolicy(
                        Policies.AdminOrCustomerAccess,
                        policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole(CompanyClaims.Roles.Admin,
                                   CompanyClaims.Roles.Customer)
                        );

                    options.AddPolicy(
                        Policies.AdminAccess,
                        policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole(CompanyClaims.Roles.Admin)
                        );

                    options.AddPolicy(
                        Policies.CustomerAccess,
                        policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole(CompanyClaims.Roles.Customer)
                        );

                    foreach (var name in apiKeyPolicies)
                    {
                        options.AddPolicy(
                            name,
                            policy => policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme,
                            Policies.ApiKey.AdminKey
                            ).RequireAuthenticatedUser()
                            );
                    }

                    //Temporary
                    options.AddPolicy(
                            Policies.AdminOrCustomerAccess,
                            policy => policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme,
                            Policies.ApiKey.AdminKey
                            ).RequireAuthenticatedUser()
                            );

                }
                );

            return services;

        }

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
                            .WithExposedHeaders(ExpiredTokenHeader, "WWW-Authenticate");
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
