using Microsoft.AspNetCore.Authentication;

namespace Sales.Payments.WebApi.Infrastructure
{
    public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public List<string> ApiKeys { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (!(ApiKeys?.Any() ?? false))
            {
                throw new InvalidOperationException(nameof(ApiKeys) + " may not be empty.");
            }
        }
    }
}
