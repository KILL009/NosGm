using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System.Linq;

namespace Frostvein.GameObject.Helpers
{
    public static class TitleHelper
    {
        #region Methods

        public static void GetEffectFromTitle(this Character e)
        {
            e.EffectFromTitle.Clear();

            long tit = 0;
            if (e.Title.Find(s => s.Stat.Equals(5)) != null) tit = e.Title.Find(s => s.Stat.Equals(5)).TitleVnum;
            if (e.Title.Find(s => s.Stat.Equals(7)) != null) tit = e.Title.Find(s => s.Stat.Equals(7)).TitleVnum;

            var item = ServerManager.GetItem((short)tit);

            if (item == null) return;

            foreach (var bcard in item.BCards) e.EffectFromTitle.Add(bcard);
        }

        public static void GetTitleFromLevel(this Character e)
        {
            e.GetVnumAndLevel(9300, 10);
            e.GetVnumAndLevel(9301, 20);
            e.GetVnumAndLevel(9302, 30);
            e.GetVnumAndLevel(9303, 40);
            e.GetVnumAndLevel(9304, 50);
            e.GetVnumAndLevel(9305, 60);
            e.GetVnumAndLevel(9306, 70);
            e.GetVnumAndLevel(9307, 80);
            e.GetVnumAndLevel(9308, 90);
            e.GetVnumAndLevel(9309, 99);
        }

        public static void GetVnumAndLevel(this Character e, short vnum, int lvl)
        {
            if (e.Title.Any(s => s.TitleVnum == vnum)) return;

            if (e.Level < lvl) return;

            e.Title.Add(new CharacterTitleDTO
            {
                CharacterId = e.CharacterId,
                Stat = 1,
                TitleVnum = vnum
            });

            e.Session.SendPacket(e.GenerateTitle());
        }

        #endregion
    }
}