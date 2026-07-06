using Frostvein.DAL;
using Frostvein.GameObject.Networking;
using System.Linq;

namespace Frostvein.GameObject.Plugin.Load.Handler
{
    public static class LoadFairyEnchantment
    {
        public static void Load()
        {
            //ServerManager.Instance.FairyEnchantments = DAOFactory.FairyEnchantmentDAO.LoadAll().ToList();
        }
    }
}
