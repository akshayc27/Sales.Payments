namespace Sales.Payments.WebApi.Infrastructure
{
    public static class Policies
    {
        public const string CustomerAccess = nameof(CustomerAccess);
        public const string AdminAccess = nameof(AdminAccess);

        public const string AdminOrCustomerAccess = nameof(AdminOrCustomerAccess);

        public static class ApiKey
        {
            public const string AdminKey = nameof(ApiKey) + "-" + nameof(AdminAccess);
        }

    }
}
