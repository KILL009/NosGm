using Frostvein.Core.ArrayExtensions;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Frostvein.GameObject
{
    public class Map : IMapDTO
    {
        #region Instantiation

        public Map(short mapId, short gridMapId, byte[] data)
        {
            MapId = mapId;
            GridMapId = gridMapId;
            Data = data;
            loadZone();
            MapTypes = new List<MapTypeDTO>();
            foreach (var maptypemap in DAOFactory.MapTypeMapDAO.LoadByMapId(mapId).ToList())
            {
                var maptype = DAOFactory.MapTypeDAO.LoadById(maptypemap.MapTypeId);
                MapTypes.Add(maptype);
            }

            if (MapTypes.Count > 0 && MapTypes[0].RespawnMapTypeId != null)
            {
                var respawnMapTypeId = MapTypes[0].RespawnMapTypeId;
                var returnMapTypeId = MapTypes[0].ReturnMapTypeId;
                if (respawnMapTypeId != null)
                {
                    DefaultRespawn = DAOFactory.RespawnMapTypeDAO.LoadById((long)respawnMapTypeId);
                }

                if (returnMapTypeId != null)
                {
                    DefaultReturn = DAOFactory.RespawnMapTypeDAO.LoadById((long)returnMapTypeId);
                }
            }
        }

        #endregion

        //private readonly Random _random;

        #region Members

        //Function to get a random number
        private static readonly Random random = new Random();

        private static readonly object syncLock = new object();

        #endregion

        #region Properties

        public byte[] Data { get; set; }

        public RespawnMapTypeDTO DefaultRespawn { get; }

        public RespawnMapTypeDTO DefaultReturn { get; }

        public short GridMapId { get; set; }

        public GridPos[][] JaggedGrid { get; set; }

        public short MapId { get; set; }

        public List<MapTypeDTO> MapTypes { get; }

        public int Music { get; set; }

        public string Name { get; set; }

        public bool ShopAllowed { get; set; }

        public byte XpRate { get; set; }

        internal int XLength { get; set; }

        internal int YLength { get; set; }

        private ConcurrentBag<MapCell> Cells { get; set; }

        #endregion

        #region Methods

        public static int GetDistance(Character character1, Character character2) => GetDistance(new MapCell { X = character1.PositionX, Y = character1.PositionY },
                new MapCell { X = character2.PositionX, Y = character2.PositionY });

        public static int GetDistance(MapCell p, MapCell q) => (int)Heuristic.Octile(Math.Abs(p.X - q.X), Math.Abs(p.Y - q.Y));

        public static MapCell GetNextStep(MapCell start, MapCell end, double steps)
        {
            MapCell futurPoint;
            double newX = start.X;
            double newY = start.Y;

            if (start.X < end.X)
            {
                newX = start.X + steps;
                if (newX > end.X)
                {
                    newX = end.X;
                }
            }
            else if (start.X > end.X)
            {
                newX = start.X - steps;
                if (newX < end.X)
                {
                    newX = end.X;
                }
            }

            if (start.Y < end.Y)
            {
                newY = start.Y + steps;
                if (newY > end.Y)
                {
                    newY = end.Y;
                }
            }
            else if (start.Y > end.Y)
            {
                newY = start.Y - steps;
                if (newY < end.Y)
                {
                    newY = end.Y;
                }
            }

            futurPoint = new MapCell { X = (short)newX, Y = (short)newY };
            return futurPoint;
        }

        public static int RandomNumber(int min, int max)
        {
            lock (syncLock)
            {
                // synchronize
                return random.Next(min, max);
            }
        }

        public bool CanWalkAround(int x, int y)
        {
            for (var dX = -1; dX <= 1; dX++)
                for (var dY = -1; dY <= 1; dY++)
                {
                    if (dX == 0 && dY == 0)
                    {
                        continue;
                    }

                    if (!IsBlockedZone(x + dX, y + dY))
                    {
                        return true;
                    }
                }

            return false;
        }

        public IEnumerable<MonsterToSummon> GenerateMonsters(short vnum, short amount, bool move,
            List<EventContainer> deathEvents, bool isBonus = false, bool isHostile = true, bool isBoss = false)
        {
            var SummonParameters = new List<MonsterToSummon>();
            for (var i = 0; i < amount; i++)
            {
                var cell = GetRandomPosition();
                SummonParameters.Add(
                    new MonsterToSummon(vnum, cell, null, move, isBonus: isBonus, isHostile: isHostile, isBoss: isBoss)
                    { DeathEvents = deathEvents });
            }

            return SummonParameters;
        }

        public List<NpcToSummon> GenerateNpcs(short vnum, short amount, List<EventContainer> deathEvents, bool isMate,
            bool isProtected, bool move, bool isHostile)
        {
            var SummonParameters = new List<NpcToSummon>();
            for (var i = 0; i < amount; i++)
            {
                var cell = GetRandomPosition();
                SummonParameters.Add(new NpcToSummon(vnum, cell, -1, isProtected, isMate, move, isHostile)
                { DeathEvents = deathEvents });
            }

            return SummonParameters;
        }

        public MapCell GetRandomPosition()
        {
            var cells = new List<MapCell>();
            for (short y = 0; y < YLength; y++)
                for (short x = 0; x < XLength; x++)
                {
                    if (!IsBlockedZone(x, y))
                    {
                        cells.Add(new MapCell { X = x, Y = y });
                    }
                }

            return cells.OrderBy(s => RandomNumber(0, int.MaxValue)).FirstOrDefault();
        }

        public MapCell GetRandomPositionByDistance(short xPos, short yPos, short distance, bool randomInRange = false)
        {
            var cells = new List<MapCell>();
            for (short y = 0; y < YLength; y++)
                for (short x = 0; x < XLength; x++)
                {
                    if (!IsBlockedZone(x, y))
                    {
                        cells.Add(new MapCell { X = x, Y = y });
                    }
                }

            if (randomInRange)
            {
                return cells
                       .Where(s => GetDistance(new MapCell { X = xPos, Y = yPos }, new MapCell { X = s.X, Y = s.Y }) <=
                               distance && !isBlockedZone(xPos, yPos, s.X, s.Y))
                       .OrderBy(s => RandomNumber(0, int.MaxValue)).FirstOrDefault();
            }

            return cells
                   .Where(s => GetDistance(new MapCell { X = xPos, Y = yPos }, new MapCell { X = s.X, Y = s.Y }) <= distance &&
                               !isBlockedZone(xPos, yPos, s.X, s.Y)).OrderBy(s => RandomNumber(0, int.MaxValue))
                   .OrderByDescending(s => GetDistance(new MapCell { X = xPos, Y = yPos }, new MapCell { X = s.X, Y = s.Y }))
                   .FirstOrDefault();
        }

        public bool isBlockedZone(int firstX, int firstY, int mapX, int mapY)
        {
            if (IsBlockedZone(mapX, mapY) || !CanWalkAround(mapX, mapY))
            {
                return true;
            }

            for (var i = 1; i <= Math.Abs(mapX - firstX); i++)
            {
                if (IsBlockedZone(firstX + Math.Sign(mapX - firstX) * i, firstY))
                {
                    return true;
                }
            }

            for (var i = 1; i <= Math.Abs(mapY - firstY); i++)
            {
                if (IsBlockedZone(firstX, firstY + Math.Sign(mapY - firstY) * i))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsBlockedZone(int x, int y)
        {
            try
            {
                if (JaggedGrid == null
                    || MapId == 2552 && y > 38
                    || x < 0
                    || y < 0
                    || x >= JaggedGrid.Length
                    || JaggedGrid[x] == null
                    || y >= JaggedGrid[x].Length
                    || JaggedGrid[x][y] == null
                )
                {
                    return true;
                }

                return !JaggedGrid[x][y].IsWalkable();
            }
            catch
            {
                return true;
            }
        }

        internal bool GetFreePosition(ref short firstX, ref short firstY, byte xpoint, byte ypoint)
        {
            var MinX = (short)(-xpoint + firstX);
            var MaxX = (short)(xpoint + firstX);

            var MinY = (short)(-ypoint + firstY);
            var MaxY = (short)(ypoint + firstY);

            var cells = new List<MapCell>();
            for (var y = MinY; y <= MaxY; y++)
                for (var x = MinX; x <= MaxX; x++)
                {
                    if (x != firstX || y != firstY)
                    {
                        cells.Add(new MapCell { X = x, Y = y });
                    }
                }

            foreach (var cell in cells.OrderBy(s => RandomNumber(0, int.MaxValue)))
            {
                if (!isBlockedZone(firstX, firstY, cell.X, cell.Y))
                {
                    firstX = cell.X;
                    firstY = cell.Y;
                    return true;
                }
            }

            return false;
        }

        private void loadZone()
        {
            using (var reader = new BinaryReader(new MemoryStream(Data)))
            {
                XLength = reader.ReadInt16();
                YLength = reader.ReadInt16();

                JaggedGrid = JaggedArrayExtensions.CreateJaggedArray<GridPos>(XLength, YLength);
                for (short i = 0; i < YLength; ++i)
                    for (short t = 0; t < XLength; ++t)
                    {
                        JaggedGrid[t][i] = new GridPos
                        {
                            Value = reader.ReadByte(),
                            X = t,
                            Y = i
                        };
                    }
            }
        }

        #endregion
    }
}