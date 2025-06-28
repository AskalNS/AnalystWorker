using ClientService.Models;

namespace ClientService.Utils
{
    public static class Mapper
    {

        public static Setting ToSetting(this UserSettings userSettings)
        {
            if (userSettings == null) return null;

            return new Setting
            {
                MerchantProductCode = userSettings.MerchantProductCode,
                ProductName = userSettings.ProductName,
                ActualPrice = userSettings.ActualPrice,
                ImageUrl = userSettings.ImageUrl,
                Remains = userSettings.Remains,
                IsDump = userSettings.IsDump,
                MaxPrice = userSettings.MaxPrice,
                MinPrice = userSettings.MinPrice
            };
        }

        public static UserSettings ToUserSettings(this Setting setting, Guid userId)
        {
            if (setting == null) return null;

            return new UserSettings
            {
                Id = Guid.NewGuid(), // создается новый Id
                UserId = userId,
                MerchantProductCode = setting.MerchantProductCode,
                ProductName = setting.ProductName,
                ActualPrice = setting.ActualPrice.Value,
                ImageUrl = setting.ImageUrl,
                Remains = setting.Remains.Value,
                IsDump = setting.IsDump.Value,
                MaxPrice = setting.MaxPrice.Value,
                MinPrice = setting.MinPrice.Value
            };
        }

    
    }
}
