using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Plugin.Event;
using Frostvein.GameObject.Plugin.Event.Handler;
using Frostvein.GameObject.Plugin.Load;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;


public static class EventServiceHandler
{
    public static void Initialize()
    {
        LoadService.Load();
        ServerManager.Instance.WorldId = Guid.NewGuid();
    }

    public static void LaunchEvents()
    {
        #region Launch

        ServerManager.Instance.ThreadSafeGroupList = new ThreadSafeSortedList<long, Group>();

        Observable.Interval(TimeSpan.FromMinutes(2)).Subscribe(x => SaveEvent.Save());

        if (ServerManager.Instance.IsAct4Online())
        {
            //Make sure to only load Glacernon if it's even online. 
            Observable.Interval(TimeSpan.FromMinutes(1)).Subscribe(x => IceFlowerEvent.Load());
            Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(x => GlacernonEvent.Load());
        }

        Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(x => GroupEvent.Load());

        Parallel.ForEach(ServerManager.Instance.Schedules, schedule => Observable
            .Timer(TimeSpan.FromSeconds(EventHelper.GetMilisecondsBeforeTime(schedule.Time).TotalSeconds),
                TimeSpan.FromDays(1)).Subscribe(e =>
                {
                    if (schedule.DayOfWeek == "" || schedule.DayOfWeek == DateTime.Now.DayOfWeek.ToString())
                        GameEventHandler.GenerateEvent(schedule.Event, schedule.LvlBracket);
                }));

        if (ServerManager.Instance.IsAct4Online())
        {
            GameEventHandler.GenerateEvent(EventType.GLACERNONSHIP);
        }

        Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x => RemoveItemEvent.Remove());

        Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(x =>
        {
            ServerManager.StartMonster();
        });

        //Let the Character load it by wish, other than that, load when logging in
        //Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(x => LoadMail.LoadMailAsync());

        CommunicationServiceClient.Instance.SessionKickedEvent += ServerManager.Instance.OnSessionKicked;
        CommunicationServiceClient.Instance.MessageSentToCharacter += ServerManager.Instance.OnMessageSentToCharacter;
        CommunicationServiceClient.Instance.FamilyRefresh += ServerManager.Instance.OnFamilyRefresh;
        CommunicationServiceClient.Instance.RelationRefresh += ServerManager.Instance.OnRelationRefresh;
        CommunicationServiceClient.Instance.StaticBonusRefresh += ServerManager.Instance.OnStaticBonusRefresh;
        CommunicationServiceClient.Instance.PenaltyLogRefresh += ServerManager.Instance.OnPenaltyLogRefresh;
        CommunicationServiceClient.Instance.GlobalEvent += ServerManager.OnGlobalEvent;
        CommunicationServiceClient.Instance.ShutdownEvent += ServerManager.OnShutdown;
        CommunicationServiceClient.Instance.RestartEvent += ServerManager.OnRestart;
        ConfigurationServiceClient.Instance.ConfigurationUpdate += ServerManager.Instance.OnConfiguratinEvent;
        MailServiceClient.Instance.MailSent += ServerManager.Instance.OnMailSent;
        ServerManager.Instance._lastGroupId = 1;

        #endregion
    }
}
