using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Sales.Payments.WebApi.Infrastructure
{
    public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
    {
        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
            ) : base(options,logger, encoder, clock) 
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKeyHeaders))
            {
                return Task.FromResult(AuthenticateResult.Fail("An Api Key is required."));
            }

            var hasValidKey = apiKeyHeaders.Any(apikey => Options.ApiKeys.Contains(apikey.Trim()));
            if (!hasValidKey)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Api Key"));
            }

            var claims = new List<Claim>();

            if (Request.Headers.TryGetValue("UserId",out var userIdHeaders))
            {
                var userId = userIdHeaders.First().Trim();
                claims.Add(new Claim(ClaimTypes.Name, userId));
                claims.Add(new Claim(CompanyClaims.UserId, userId));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
            
        }
    }
}
