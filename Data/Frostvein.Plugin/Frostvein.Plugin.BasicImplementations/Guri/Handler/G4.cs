
using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Extension.Reputation;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.ThreadEnum;
using Frostvein.GameObject.TitanShield;
using Frostvein.GameObject.TitanShield.Thread;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G4 : IGuriHandler
    {
        public long GuriEffectId => 4;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 4)
            {
                const int speakerVNum = 2173;
                const int limitedSpeakerVNum = 10028;
                if (e.Argument == 1)
                {
                    if (e.Value.Contains("^"))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INVALID_NAME")));
                        return;
                    }
                    short[] listPetnameVNum = { 2157, 10023 };
                    short vnumToUse = -1;
                    foreach (var vnum in listPetnameVNum)
                    {
                        if (Session.Character.Inventory.CountItem(vnum) > 0)
                        {
                            vnumToUse = vnum;
                        }
                    }
                    var mate = Session.Character.Mates.Find(s => s.MateTransportId == e.Data);
                    if (mate != null && Session.Character.Inventory.CountItem(vnumToUse) > 0)
                    {
                        mate.Name = e.Value.Truncate(16);
                        Session.CurrentMapInstance?.Broadcast(mate.GenerateOut(), ReceiverType.AllExceptMe);
                        foreach (var sess in Session.CurrentMapInstance.Sessions.Where(s => s.Character != null))
                        {
                            if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == sess.Character.Faction)
                            {
                                sess.SendPacket(mate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                            }
                            else
                            {
                                sess.SendPacket(mate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, sess.Account.Authority));
                            }
                        }
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_NAME_PET")));
                        Session.SendPacket(Session.Character.GeneratePinit());
                        Session.SendPackets(Session.Character.GeneratePst());
                        Session.SendPackets(Session.Character.GenerateScP());
                        Session.Character.Inventory.RemoveItemAmount(vnumToUse);
                    }
                }

                // presentation message
                if (e.Argument == 2)
                {
                    if (Session.Character.LastSkillUse.AddSeconds(10) > DateTime.Now
                            || Session.Character.LastDefence.AddSeconds(10) > DateTime.Now)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                            string.Format(Language.Instance.GetMessageFromKey("PLAYER_IN_BATTLE"),
                                Session.Character.Name)));
                        return;
                    }

                    string message = "";
                    string[] valuesplit = e.Value?.Split(' ');
                    if (valuesplit == null)
                    {
                        return;
                    }
                    for (int i = 0; i < valuesplit.Length; i++)
                    {
                        message += valuesplit[i] + "^";
                    }
                    message = message.Substring(0, message.Length - 1); // Remove the last ^
                    message = message.Trim();
                    if (message.Length > 60)
                    {
                        message = message.Substring(0, 60);
                    }
                    if (message == null)
                    {
                        Session.SendPacket("info Field can't be empty.");
                        return;
                    }

                    Session.Character.SetStatus = true;
                    Session.Character.StatusMessage = message;
                    StatusExtension.GenerateStatus(Session, message);
                }

                // Speaker
                if (e.Argument == 3 && (Session.Character.Inventory.CountItem(speakerVNum) > 0 || Session.Character.Inventory.CountItem(limitedSpeakerVNum) > 0))
                {
                    string sayPacket = "";
                    string message =
                        $"<{Language.Instance.GetMessageFromKey("SPEAKER")} {Language.Instance.GetMessageFromKey("CH")}{ServerManager.Instance.ChannelId}> [{Session.Character.Name}]: ";
                    byte sayItemInventory = 0;
                    short sayItemSlot = 0;
                    int baseLength = message.Length;
                    string[] valuesplit = e.Value?.Split(' ');
                    if (valuesplit == null)
                    {
                        return;
                    }

                    if (e.Data == 999 && (valuesplit.Length < 3 || !byte.TryParse(valuesplit[0], out sayItemInventory) || !short.TryParse(valuesplit[1], out sayItemSlot)))
                    {
                        return;
                    }

                    for (int i = 0 + (e.Data == 999 ? 2 : 0); i < valuesplit.Length; i++)
                    {
                        message += valuesplit[i] + " ";
                    }

                    if (message.Length > 120 + baseLength)
                    {
                        message = message.Substring(0, 120 + baseLength);
                    }

                    message = message.Trim();

                    if (Session.Character.LastSpeaker.AddSeconds(30) > DateTime.Now)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo("Speaker in cooldown, please wait 30 seconds."));
                        return;
                    }

                    string[] wordsInMessage = message.Split(new char[] { ' ', '.', ',', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    bool containsWord = wordsInMessage.Any(word => FilterThread.FilteredWords.Contains(word, StringComparer.OrdinalIgnoreCase));

                    if (containsWord)
                    {
                        TitanShield.Response(Session, ActionType.Filter);
                        return;
                    }

                    
                    if (e.Data == 999)
                    {
                        //sayPacket = Session.Character.GenerateSayItem(message, 13, sayItemInventory, sayItemSlot);
                        CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                        {
                            DestinationCharacterId = null,
                            SourceCharacterId = Session.Character.CharacterId,
                            SourceWorldId = ServerManager.Instance.WorldId,
                            Message = Session.Character.GenerateSayItem(message, 13, sayItemInventory, sayItemSlot),
                            Type = MessageType.Broadcast

                        });
                        Session.Character.LastSpeaker = DateTime.Now;
                        //await //LOGGER($"[Speaker][{Session.Character.Name}]: {message}");
                    }
                    else
                    {
                        //sayPacket = Session.Character.GenerateSay(message, 13);    
                        CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                        {
                            DestinationCharacterId = null,
                            SourceCharacterId = Session.Character.CharacterId,
                            SourceWorldId = ServerManager.Instance.WorldId,
                            Message = Session.Character.GenerateSay(message, 13),
                            Type = MessageType.Broadcast
                        });
                        Session.Character.LastSpeaker = DateTime.Now;
                        //await //LOGGER($"[Speaker][{Session.Character.Name}]: {message}");
                    }

                    if (Session.Character.IsMuted())
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(
                                Language.Instance.GetMessageFromKey("SPEAKER_CANT_BE_USED"), 10));
                        return;
                    }

                    if (Session.Character.Inventory.CountItem(limitedSpeakerVNum) > 0)
                    {
                        Session.Character.Inventory.RemoveItemAmount(limitedSpeakerVNum);
                    }
                    else
                    {
                        Session.Character.Inventory.RemoveItemAmount(speakerVNum);
                    }

                    if (ServerManager.Instance.ChannelId == 51)
                    {
                        ServerManager.Instance.Broadcast(Session, sayPacket, ReceiverType.AllExceptMeAct4);
                        Session.SendPacket(sayPacket);
                    }
                    else
                    {
                        ServerManager.Instance.Broadcast(Session, sayPacket, ReceiverType.All);
                    }
                }

                // Bubble

                if (e.Argument == 4)
                {
                    var bubbleVNum = Session.Character.Inventory.CountItem(2174) > 0
                        ? 2174
                        : Session.Character.Inventory.CountItem(10029) > 0 ? 10029 : -1;
                    if (bubbleVNum != -1)
                    {
                        var message = "";
                        var valuesplit = e.Value?.Split(' ');
                        if (valuesplit == null)
                        {
                            return;
                        }

                        for (var i = 0; i < valuesplit.Length; i++)
                        {
                            message += valuesplit[i] + "^";
                        }

                        message = message.Substring(0, message.Length - 1); // Remove the last ^
                        message = message.Trim();
                        if (message.Length > 60)
                        {
                            message = message.Substring(0, 60);
                        }

                        Session.Character.BubbleMessage = message;
                        Session.Character.BubbleMessageEnd = DateTime.Now.AddMinutes(30);
                        Session.SendPacket($"csp_r {Session.Character.BubbleMessage}");
                        Session.Character.Inventory.RemoveItemAmount(bubbleVNum);
                    }
                }
            }
        }
    }
}