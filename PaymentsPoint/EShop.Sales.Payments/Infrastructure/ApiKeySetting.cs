using Destructurama.Attributed;
using System.ComponentModel.DataAnnotations;

namespace Sales.Payments.WebApi.Infrastructure
{
    public sealed class ApiKeySetting
    {
        [Required]
        [NotLogged]
        public List<string> Keys { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
