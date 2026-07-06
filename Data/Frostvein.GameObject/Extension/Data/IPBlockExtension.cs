using System.Linq;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Frostvein.GameObject.Extension
{
    public static class IPBlockExtension
    {
        public static bool BlockIp(ClientSession Session)
        {
            var List = new List<string>
            {
                "",
            };

            if (List.Any(s => Session.Character.CurrentIp.ToLower().Contains(s)) || List.Any(s => Session.Account.RegistrationIP.ToLower().Contains(s)))
            {
                return false;
            }

            return true;
        }
    }
}