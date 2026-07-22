namespace NosGm.Data
{
    public interface IMapDTO
    {
        #region Properties

        byte[] Data { get; set; }

        short GridMapId { get; set; }

        short MapId { get; set; }

        int Music { get; set; }

        string Name { get; set; }

        bool ShopAllowed { get; set; }

        #endregion
    }
}