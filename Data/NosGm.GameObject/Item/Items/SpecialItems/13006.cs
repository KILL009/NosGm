using NosGm.Algorithm;
using NosGm.Domain;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Items
{
    public static class VNum13006
    {
        public static async Task Execute(ClientSession Session)
        {
            DialogExtension.GenerateDialog(Session, 12007);
        }
    }
}
