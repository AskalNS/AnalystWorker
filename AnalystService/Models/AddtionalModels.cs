namespace ClientService.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
    }
    public class ProductPointsDTO
    {
        public string MerchantProductCode { get; set; }
        public int LoanPeriod { get; set; }
        public List<object> PointByCity { get; set; } // или уточни тип, если знаешь
    }
}
