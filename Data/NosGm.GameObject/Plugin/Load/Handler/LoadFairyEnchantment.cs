using NosGm.DAL;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.GameObject.Plugin.Load.Handler
{
    public static class LoadFairyEnchantment
    {
        public static void Load()
        {
            //ServerManager.Instance.FairyEnchantments = DAOFactory.FairyEnchantmentDAO.LoadAll().ToList();
        }
    }
}
