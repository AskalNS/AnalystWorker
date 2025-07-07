using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientService.Models
{
    public class Code
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; }
        public string CodeValue { get; set; }
    }

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; }
        public string HashPassword { get; set; }
    }

    public class HalykCredential
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Login { get; set; }
        public string EncriptPassword { get; set; }
    }

    public class IntegratedUsers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double MaxPersent { get; set; }
        public DateTime TimeFlag { get; set; }
        public bool IsWorking { get; set; }

    }

    public class UserSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string MerchantProductCode { get; set; }
        public string ProductName { get; set; }
        public double fitstMarketPrice { get; set; }
        public double lastMarketPrice { get; set; }
        public double ActualPrice { get; set; }
        public string ImageUrl { get; set; }
        public string MarketUrl { get; set; }
        public int Remains { get; set; }
        public int Place { get; set; }
        public bool IsDump { get; set; }
        public double MaxPrice { get; set; }
        public double MinPrice { get; set; }
    }
    public class ProductPoint
    {
        public Guid Id { get; set; }
        public string MerchantProductCode { get; set; } = string.Empty;
        public int LoanPeriod { get; set; }
        public string CityCode { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string CityNameRu { get; set; } = string.Empty;
        public string PointCode { get; set; } = string.Empty;
        public string PointName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public double Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AdditionalUserInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string HalykToken { get; set; }
        public string TelegramId { get; set; }
        public DateTime TimeFlag { get; set; }

    }

    public class ProductArticuls
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string productName { get; set; }
        public string Articule { get; set; }

    }
}
