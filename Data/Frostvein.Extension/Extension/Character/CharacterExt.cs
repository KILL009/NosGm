using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Frostvein.Extension.GameExtension.Character
{
    public static class CharacterExt
    {
        #region Methods

        public static bool CanCreateCharacter(this ClientSession Session, byte slot, string characterName)
        {
            if (slot > 3 || DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, slot) != null)
            {
                return false;
            }

            if (characterName.Length <= 3 || characterName.Length >= 15)
            {
                return false;
            }

            var rg = new Regex(@"^[A-Za-z0-9_äÄöÖüÜß~*<>°+-.!_-Ð™¤£±†‡×ßø^\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$");
            //@"^[\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$");

            if (rg.Matches(characterName).Count != 1)
            {
                Session.SendPacketFormat($"info {Language.Instance.GetMessageFromKey("INVALID_CHARNAME")}");
                return false;
            }

            if (DAOFactory.CharacterDAO.LoadByName(characterName) != null)
            {
                Session.SendPacketFormat($"info {Language.Instance.GetMessageFromKey("ALREADY_TAKEN")}");
                return false;
            }

            //Use Titan Shield here
            var BlackListed = new List<string>
            {
                "[",
                "]",
                "[gm]",
                "[supporter]",
                "bitch",
                "ass",
                "Dupe",
                "Exploit",
            };

            if (BlackListed.Any(s => characterName.ToLower().Contains(s)))
            {
                Session.SendPacketFormat($"info {Language.Instance.GetMessageFromKey("BLACKLIST")}");
                return false;
            }

            if (slot > 3)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}