using Frostvein.Algorithm;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Items
{
    public static class VNum13006
    {
        public static async Task Execute(ClientSession Session)
        {
            DialogExtension.GenerateDialog(Session, 12007);
        }
    }
}
