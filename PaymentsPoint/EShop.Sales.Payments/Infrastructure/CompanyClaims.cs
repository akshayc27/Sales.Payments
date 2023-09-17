namespace Sales.Payments.WebApi.Infrastructure
{
    public static class CompanyClaims
    {
        public const string CustomerId = "Company_Customer_Id";
        public const string AdminId = "Company_Admin_Id";
        public const string SessionId = "Company_Session_Id";
        public const string UserId = "Company_User_Id";

        public static class Roles
        {
            public const string Customer = nameof(Customer);
            public const string Admin = nameof(Admin);
        }

    }
}
