using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event;
using NosGm.GameObject.Plugin.Event.Handler;
using NosGm.GameObject.Plugin.Load;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
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

        Observable.Interval(TimeSpan.FromMinutes(2)).Subscribe(
            EventRuntimeGuard.Protect<long>("SaveEvent", _ => SaveEvent.Save()),
            EventRuntimeGuard.ObserveFailure("SaveEvent"));

        if (ServerManager.Instance.IsAct4Online())
        {
            // Make sure to only load Glacernon if it is online.
            Observable.Interval(TimeSpan.FromMinutes(1)).Subscribe(
                EventRuntimeGuard.Protect<long>("IceFlowerEvent", _ => IceFlowerEvent.Load()),
                EventRuntimeGuard.ObserveFailure("IceFlowerEvent"));
            Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(
                EventRuntimeGuard.Protect<long>("GlacernonEvent", _ => GlacernonEvent.Load()),
                EventRuntimeGuard.ObserveFailure("GlacernonEvent"));
        }

        Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(
            EventRuntimeGuard.Protect<long>("GroupEvent", _ => GroupEvent.Load()),
            EventRuntimeGuard.ObserveFailure("GroupEvent"));

        Parallel.ForEach(ServerManager.Instance.Schedules, schedule =>
        {
            TimeSpan dueTime = EventHelper.GetMilisecondsBeforeTime(schedule.Time);
            string operation = $"Schedule:{schedule.Event}:{schedule.Time}";
            Observable.Timer(dueTime, TimeSpan.FromDays(1)).Subscribe(
                EventRuntimeGuard.Protect<long>(operation, _ =>
                {
                    if (string.IsNullOrEmpty(schedule.DayOfWeek) ||
                        schedule.DayOfWeek == DateTime.Now.DayOfWeek.ToString())
                    {
                        GameEventHandler.GenerateEvent(schedule.Event, schedule.LvlBracket);
                    }
                }),
                EventRuntimeGuard.ObserveFailure(operation));
        });

        if (ServerManager.Instance.IsAct4Online())
        {
            GameEventHandler.GenerateEvent(EventType.GLACERNONSHIP);
        }

        Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(
            EventRuntimeGuard.Protect<long>("RemoveItemEvent", _ => RemoveItemEvent.Remove()),
            EventRuntimeGuard.ObserveFailure("RemoveItemEvent"));

        Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(
            EventRuntimeGuard.Protect<long>("StartMonster", _ => ServerManager.StartMonster()),
            EventRuntimeGuard.ObserveFailure("StartMonster"));

        // Let the Character load mail by request; otherwise load it when logging in.
        // Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(x => LoadMail.LoadMailAsync());

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
