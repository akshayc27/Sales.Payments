using Destructurama.Attributed;

namespace Sales.Payments.WebApi.Infrastructure
{
    public sealed class JwtSettings
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }

        [LogMasked]
        public string Key { get; set; }

        public TimeSpan TokenDuration { get; set; }
    }
}
