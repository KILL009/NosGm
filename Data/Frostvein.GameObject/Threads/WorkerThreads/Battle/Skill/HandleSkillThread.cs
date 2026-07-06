using Frostvein.Domain;
using System.Threading.Tasks;
using System;

using Frostvein.GameObject.Extension.Message;

namespace Frostvein.GameObject.Battle.Thread
{
    public static class HandleSkillThread
    {
        public static void Handle(ClientSession Session)
        {
            //var skills = Session.Character.GetSkills();
            //if (skills != null)
            //{

            //}
            //else
            //{
            //    await HandleError(Session, "your Skills", $"Name: {Session.Character.Name}", "There was an issue regarding Skills. 'var skills was null' in HandleSkillThread.cs", "s");
            //}
            HandleError(Session, "your Skills", $"Name: {Session.Character.Name}", "There was an issue regarding Skills. 'var skills was null' in HandleSkillThread.cs", "s");
        }

        public static void HandleError(ClientSession Session, string Source, string FirstContext, string SecondContext, string Description)
        {
            try
            {
                TitanShield.TitanShield.ReponseWithId(Session, Source, FirstContext, SecondContext, Description);
            }
            catch (Exception ex)
            {
                MessageExtension.SendInfo(Session, "Something went wrong while Handling the Error");
                //LOGGER await Log.LogAsync(ex.ToString());
            }
        }
    }
}
