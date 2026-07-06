using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class BazaarItemMapper
    {
        #region Methods

        public static bool ToBazaarItem(BazaarItemDTO input, BazaarItem output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.RegistrationIP = input.RegistrationIP;
            output.CurrentIp = input.CurrentIp;
            output.Amount = input.Amount;
            output.BazaarItemId = input.BazaarItemId;
            output.DateStart = input.DateStart;
            output.Duration = input.Duration;
            output.IsPackage = input.IsPackage;
            output.ItemInstanceId = input.ItemInstanceId;
            output.MedalUsed = input.MedalUsed;
            output.Price = input.Price;
            output.SellerId = input.SellerId;
            return true;
        }

        public static bool ToBazaarItemDTO(BazaarItem input, BazaarItemDTO output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.RegistrationIP = input.RegistrationIP;
            output.CurrentIp = input.CurrentIp;
            output.Amount = input.Amount;
            output.BazaarItemId = input.BazaarItemId;
            output.DateStart = input.DateStart;
            output.Duration = input.Duration;
            output.IsPackage = input.IsPackage;
            output.ItemInstanceId = input.ItemInstanceId;
            output.MedalUsed = input.MedalUsed;
            output.Price = input.Price;
            output.SellerId = input.SellerId;
            return true;
        }

        #endregion
    }
}