namespace ClientService.Models
{
    public class CodeDTO
    {
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }

    public class UserDTO
    {
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class HalykCredentialDTO
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class UserSettingsDTO
    {
        public double MaxPersent { get; set; }
        public List<Setting> settings { get; set; } = new();
    }


    public class Setting
    {
        public string MerchantProductCode { get; set; }
        public string? ProductName { get; set; }
        public double? ActualPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? MarketUrl { get; set; }
        public int? Remains { get; set; }
        public bool? IsDump { get; set; }
        public double? MaxPrice { get; set; }
        public double? MinPrice { get; set; }
    }

}
