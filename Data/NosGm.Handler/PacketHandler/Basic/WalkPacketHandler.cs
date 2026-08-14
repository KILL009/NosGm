using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class WalkPacketHandler : IPacketHandler
    {
        #region Instantiation

        public WalkPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Walk(WalkPacket walkPacket)
        {
            if (!Session.Character.CanMove())
            {
                Session.SendPacket(Session.Character.GenerateTp());
                return;
            }

            if (Session.Character.MeditationDictionary.Count != 0)
            {
                Session.Character.MeditationDictionary.Clear();
            }

            if (Session.HasCurrentMapInstance
                && !Session.CurrentMapInstance.Map.IsBlockedZone(walkPacket.XCoordinate, walkPacket.YCoordinate)
                && !Session.Character.IsChangingMapInstance && !Session.Character.HasShopOpened)
            {
                Session.Character.PyjamaDead = false;
                if (!Session.Character.InvisibleGm)
                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.Move(UserType.Player,
                        Session.Character.CharacterId, walkPacket.XCoordinate, walkPacket.YCoordinate,
                        Session.Character.Speed));
                Session.SendPacket(Session.Character.GenerateCond());
                Session.Character.WalkDisposable?.Dispose();
                Session.Character.WalkDisposable = null;
                walk();

                if (Session.Character.PositionX == walkPacket.XCoordinate &&
                    Session.Character.PositionY == walkPacket.YCoordinate)
                {
                    return;
                }

                var interval = 100 - Session.Character.Speed * 5 + 100 > 0
                    ? 100 - Session.Character.Speed * 5 + 100
                    : 0;
                Session.Character.WalkDisposable = Observable.Interval(TimeSpan.FromMilliseconds(interval))
                    .Subscribe(obs => { walk(); });

                void walk()
                {
                    var nextCell =  Map.GetNextStep(
                            new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY },
                            new MapCell { X = walkPacket.XCoordinate, Y = walkPacket.YCoordinate }, 1);

                    Session.Character.GetDir(Session.Character.PositionX, Session.Character.PositionY, nextCell.X,
                        nextCell.Y);

                    if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                    {
                        Session.Character.MapX = nextCell.X;
                        Session.Character.MapY = nextCell.Y;
                    }

                    Session.Character.PositionX = nextCell.X;
                    Session.Character.PositionY = nextCell.Y;

                    Session.Character.LastMove = DateTime.Now;

                    if (Session.Character.IsVehicled)
                    {
                        Session.Character.Mates.Where(s => s.IsTeamMember || s.IsTemporalMate).ToList().ForEach(s =>
                        {
                            s.PositionX = Session.Character.PositionX;
                            s.PositionY = Session.Character.PositionY;
                        });
                    }

                    if (Session.Character.LastMonsterAggro.AddSeconds(5) > DateTime.Now)
                    {
                        Session.Character.UpdateBushFire();
                    }

                    Session.CurrentMapInstance?.OnAreaEntryEvents
                        ?.Where(s => s.InZone(Session.Character.PositionX, Session.Character.PositionY)).ToList()
                        .ForEach(e => e.Events.ForEach(evt => EventHelper.Instance.RunEvent(evt)));
                    Session.CurrentMapInstance?.OnAreaEntryEvents?.RemoveAll(s =>
                        s.InZone(Session.Character.PositionX, Session.Character.PositionY));

                    Session.CurrentMapInstance?.OnMoveOnMapEvents?.ForEach(e => EventHelper.Instance.RunEvent(e));
                    Session.CurrentMapInstance?.OnMoveOnMapEvents?.RemoveAll(s => s != null);

                    if (Session.Character.PositionX == walkPacket.XCoordinate && Session.Character.PositionY == walkPacket.YCoordinate)
                    {
                        Session.Character.WalkDisposable?.Dispose();
                        Session.Character.WalkDisposable = null;
                    }
                }
            }
        }

        #endregion
    }
}