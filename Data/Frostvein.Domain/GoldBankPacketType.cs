namespace Frostvein.Domain
{
    public enum GoldBankPacketType : byte
    {
        BankMoney = 0,
        Deposit = 1,
        Withdraw = 2,
        OpenBank = 3
    }
}