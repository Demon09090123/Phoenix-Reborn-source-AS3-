﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;
using wServer.networking;
using wServer.networking.svrPackets;
using wServer.realm.entities;
using wServer.realm.terrain;
using wServer.realm.worlds;

namespace wServer.realm
{
    public abstract class World : IDisposable
    {
        public const int TUT_ID = -1;
        public const int NEXUS_ID = -2;
        public const int RAND_REALM = -3;
        public const int NEXUS_LIMBO = -4;
        public const int VAULT_ID = -5;
        public const int TEST_ID = -6;
        public const int GUILD_ID = -7;
        public const int SHOP_ID = -8;
        
        private static readonly ILog log = LogManager.GetLogger(typeof(World));
        public bool dungeon = false;
        private int entityInc;
        private RealmManager manager;

        public bool Connecting = false;

        protected World()
        {
            ConMessage = "";
            Players = new ConcurrentDictionary<int, Player>();
            Enemies = new ConcurrentDictionary<int, Enemy>();
            Quests = new ConcurrentDictionary<int, Enemy>();
            Pets = new ConcurrentDictionary<int, Entity>();
            Projectiles = new ConcurrentDictionary<Tuple<int, byte>, Projectile>();
            StaticObjects = new ConcurrentDictionary<int, StaticObject>();
            ItemEntities = new ConcurrentDictionary<int, ItemEntity>();
            Timers = new List<WorldTimer>();
            ExtraXML = Empty<string>.Array;
            AllowTeleport = true;
            AllowNexus = true;
            AllowAbilityTeleport = true;
            ShowDisplays = true;
            SetMusic("Menu");
        }

        public bool IsLimbo { get; protected set; }

        public RealmManager Manager
        {
            get { return manager; }
            internal set
            {
                manager = value;
                if (manager == null) return;
                //Seed = manager.Random.NextUInt32();
                Seed = (uint)((long)Environment.TickCount * CryptoRandom.Next(-90000, 90000)) % uint.MaxValue;
                Init();
            }
        }

        public int Id { get; internal set; }
        public string Identifier { get; protected set; }
        public string Name { get; protected set; }
        public string ConMessage { get; protected set; }
        public string[] Music { get; set; }
        public string[] DefaultMusic { get; set; }

        public ConcurrentDictionary<int, Player> Players { get; private set; }
        public ConcurrentDictionary<int, Enemy> Enemies { get; private set; }
        public ConcurrentDictionary<int, Entity> Pets { get; private set; }
        public ConcurrentDictionary<Tuple<int, byte>, Projectile> Projectiles { get; private set; }
        public ConcurrentDictionary<int, StaticObject> StaticObjects { get; private set; }
        public ConcurrentDictionary<int, ItemEntity> ItemEntities { get; private set; }
        public List<WorldTimer> Timers { get; private set; }
        public int Background { get; protected set; }
        public int Difficulty { get; protected set; }

        public CollisionMap<Entity> EnemiesCollision { get; private set; }
        public CollisionMap<Entity> PlayersCollision { get; private set; }

        public bool AllowTeleport { get; protected set; }
        public bool ShowDisplays { get; protected set; }
        public string[] ExtraXML { get; protected set; }
        public bool AllowNexus { get; protected set; }
        public bool AllowAbilityTeleport { get; protected set; }

        public Wmap Map { get; private set; }
        public ConcurrentDictionary<int, Enemy> Quests { get; private set; }
        public bool Deleted { get; private set; }

        public uint Seed { get; private set; }
        public int WorldDisposeMS { get; private set; }
        public bool DisposingWorld { get; set; }

        public virtual World GetInstance(Client client)
        {
            return null;
        }

        public bool IsPassable(int x, int y)
        {
            WmapTile tile = Map[x, y];
            ObjectDesc desc;
            if (Manager.GameData.Tiles[tile.TileId].NoWalk)
                return false;
            if (Manager.GameData.ObjectDescs.TryGetValue(tile.ObjType, out desc))
            {
                if (!desc.Static)
                    return false;
                if (desc.OccupySquare || desc.EnemyOccupySquare || desc.FullOccupy)
                    return false;
            }
            return true;
        }

        public int GetNextEntityId()
        {
            return Interlocked.Increment(ref entityInc);
        }

        public void SwitchMusic(params string[] music)
        {
            if (music.Length == 0)
                Music = DefaultMusic;
            else
                Music = music;
            BroadcastPacket(new SwitchMusicPacket
            {
                Music = Music[new wRandom().Next(0, Music.Length)]
            }, null);
        }

        public void SetMusic(params string[] music)
        {
            Music = music;
            DefaultMusic = music;
        }

        public void SetDisposeTime(int time)
        {
            WorldDisposeMS = time;
        }

        public string GetMusic(wRandom rand = null)
        {
            if (Music.Length == 0)
                return "null";
            if (rand == null)
                rand = new wRandom();
            return Music[rand.Next(0, Music.Length)];
        }

        public bool Delete()
        {
            lock (this)
            {
                if (Players.Count > 0) return false;
                Id = 0;
            }
            Deleted = true;
            Map = null;
            Players = null;
            Enemies = null;
            Projectiles = null;
            StaticObjects = null;
            ItemEntities = null;
            return true;
        }

        protected virtual void Init()
        {
            //this does nothing >:(
        }

        public virtual void TileEvent(Player player, WmapTile tile)
        {
        }

        protected void FromWorldMap(Stream dat)
        {
            log.InfoFormat("Loading map for world {0}({1})...", Id, Name);

            Map = new Wmap(Manager.GameData);
            entityInc = 0;
            entityInc += Map.Load(dat, 0);

            int w = Map.Width, h = Map.Height;
            EnemiesCollision = new CollisionMap<Entity>(0, w, h);
            PlayersCollision = new CollisionMap<Entity>(1, w, h);

            Projectiles.Clear();
            StaticObjects.Clear();
            Enemies.Clear();
            Players.Clear();
            ItemEntities.Clear();
            foreach (Entity i in Map.InstantiateEntities(Manager))
            {
                EnterWorld(i);
            }
        }

        public virtual int EnterWorld(Entity entity)
        {
            if (entity is Player)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                Players.TryAdd(entity.Id, entity as Player);
                PlayersCollision.Insert(entity);
                SpawnEntity(entity);
            }
            else if (entity is Enemy)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                Enemies.TryAdd(entity.Id, entity as Enemy);
                EnemiesCollision.Insert(entity);
                if (entity.ObjectDesc.Quest)
                    Quests.TryAdd(entity.Id, entity as Enemy);

                if (entity.isPet)
                {
                    Pets.TryAdd(entity.Id, entity);
                }
            }
            else if (entity is Projectile)
            {
                entity.Init(this);
                var prj = entity as Projectile;
                Projectiles[new Tuple<int, byte>(prj.ProjectileOwner.Self.Id, prj.ProjectileId)] = prj;
            }
            else if (entity is StaticObject)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                StaticObjects.TryAdd(entity.Id, entity as StaticObject);
                if (entity is Decoy)
                    PlayersCollision.Insert(entity);
                else
                    EnemiesCollision.Insert(entity);
            }
            else if (entity is ItemEntity)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                ItemEntities.TryAdd(entity.Id, entity as ItemEntity);
                EnemiesCollision.Insert(entity);
            }
            return entity.Id;
        }

        public virtual void LeaveWorld(Entity entity)
        {
            if (entity is Player)
            {
                Player dummy;
                Players.TryRemove(entity.Id, out dummy);
                PlayersCollision.Remove(entity);
            }
            else if (entity is Enemy)
            {
                Enemy dummy;
                Enemies.TryRemove(entity.Id, out dummy);
                EnemiesCollision.Remove(entity);
                if (entity.ObjectDesc.Quest)
                    Quests.TryRemove(entity.Id, out dummy);
                if (entity.isPet)
                {
                    Entity dummy2;
                    Pets.TryRemove(entity.Id, out dummy2);
                }
            }
            else if (entity is Projectile)
            {
                var p = entity as Projectile;
                Projectiles.TryRemove(new Tuple<int, byte>(p.ProjectileOwner.Self.Id, p.ProjectileId), out p);
            }
            else if (entity is StaticObject)
            {
                StaticObject dummy;
                StaticObjects.TryRemove(entity.Id, out dummy);
                if (entity is Decoy)
                    PlayersCollision.Remove(entity);
                else
                    EnemiesCollision.Remove(entity);
            }
            else if (entity is ItemEntity)
            {
                ItemEntity dummy;
                EnemiesCollision.Remove(entity);
                ItemEntities.TryRemove(entity.Id, out dummy);
            }
            entity.Dispose();
            entity = null;
        }

        public void Dispose()
        {
            Players.Clear();
            Enemies.Clear();
            Quests.Clear();
            Pets.Clear();
            Projectiles.Clear();
            StaticObjects.Clear();
            ItemEntities.Clear();
            Timers.Clear();
        }

        public Entity GetEntity(int id)
        {
            Player ret1;
            if (Players.TryGetValue(id, out ret1)) return ret1;
            Enemy ret2;
            if (Enemies.TryGetValue(id, out ret2)) return ret2;
            StaticObject ret3;
            if (StaticObjects.TryGetValue(id, out ret3)) return ret3;
            ItemEntity ret4;
            if (ItemEntities.TryGetValue(id, out ret4)) return ret4;
            return null;
        }

        public IntPoint GetRandomTile(TileRegion region)
        {
            if (!Map.Regions.ContainsKey(region))
                return new IntPoint(0, 0);
            var rand = new Random();
            List<IntPoint> tiles = Map.Regions[region];
            return tiles[rand.Next(0, tiles.Count)];
        }

        public virtual void SpawnEntity(Entity entity)
        {
            IntPoint tile = GetRandomTile(TileRegion.Spawn);
            entity.Move(tile.X + 0.5f, tile.Y + 0.5f);
        }

        public Player GetUniqueNamedPlayer(string name)
        {
            foreach (var i in Players)
            {
                if (i.Value.NameChosen && i.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return i.Value;
            }
            return null;
        }

        public Player GetUniqueNamedPlayerRough(string name)
        {
            return (from i in Players where i.Value.CompareName(name) select i.Value).FirstOrDefault();
        }

        public void BroadcastPacket(Packet pkt, Player exclude)
        {
            foreach (var i in Players.Where(i => i.Value != exclude))
                i.Value.Client.SendPacket(pkt);
        }

        public void BroadcastPackets(IEnumerable<Packet> pkts, Player exclude)
        {
            foreach (var i in Players.Where(i => i.Value != exclude))
                i.Value.Client.SendPackets(pkts);
        }

        public void BroadcastPacketCondition(Packet pkt, Predicate<Player> exclude)
        {
            foreach (var i in Players.Where(i => !exclude(i.Value)))
                i.Value.Client.SendPacket(pkt);
        }

        public void BroadcastPacketsCondition(IEnumerable<Packet> pkts, Predicate<Player> exclude)
        {
            foreach (var i in Players.Where(i => !exclude(i.Value)))
                i.Value.Client.SendPackets(pkts);
        }

        public void BroadcastPacketWithIgnores(Packet pkt, Player from)
        {
            if (from.Client.Account.Rank >= 4)
            {
                foreach (var i in Players)
                    i.Value.Client.SendPacket(pkt);
            }
            else
            {
                foreach (var i in Players.Where(i => !i.Value.Ignored.Contains(from.AccountId)))
                    i.Value.Client.SendPacket(pkt);
            }
        }

        public void BroadcastPacketsWithIgnores(IEnumerable<Packet> pkts, Player from)
        {
            if (from.Client.Account.Rank >= 4)
            {
                foreach (var i in Players)
                    i.Value.Client.SendPackets(pkts);
            }
            else
            {
                foreach (var i in Players.Where(i => !i.Value.Ignored.Contains(from.AccountId)))
                    i.Value.Client.SendPackets(pkts);
            }
        }

        public void TileTeleport(Player player, float X, float Y)
        {
            BroadcastPacket(new GotoPacket
            {
                ObjectId = player.Id,
                Position = new Position
                {
                    X = X,
                    Y = Y
                }
            }, null);
            BroadcastPacket(new ShowEffectPacket
            {
                EffectType = EffectType.Trail,
                TargetId = player.Id,
                PosA = new Position { X = player.X, Y = player.Y },
                Color = new ARGB(0xffffffff)
            }, null);
            BroadcastPacket(new ShowEffectPacket
            {
                EffectType = EffectType.AreaBlast,
                Color = new ARGB(0xff416B2B),
                TargetId = player.Id,
                PosA = new Position { X = 2 }
            }, null);
        }

        public static bool IsWorldStatic(World world) //make sure to add any important non-disposing worlds to this list
        {
            if (world.Identifier == "Realm") return true;
            switch (world.Id)
            {
                case TUT_ID:
                case NEXUS_ID:
                case RAND_REALM:
                case NEXUS_LIMBO:
                case VAULT_ID:
                case TEST_ID:
                case GUILD_ID:
                case SHOP_ID:
                    return true;
                default:
                    return false; //if it returns false, it'll act as a dungeon and dispose upon worlddisposetime
            }
        }

        public static void RemoveFromTimer(World world)
        {
            if (world.DisposingWorld)
            {
                var manager = world.Manager;
                //if (world.Identifier == "Realm")
                //{
                //    manager.Monitor.WorldRemoved(world);
                //    manager.RemoveWorld(world);

                //    GameWorld newWorld = GameWorld.AutoName(1, true);
                //    manager.AddWorld(newWorld);
                //    manager.Monitor.WorldAdded(newWorld);
                //    return;
                //}
                manager.RemoveWorld(world);
                return;
            }
        }

        public virtual void Tick(RealmTime time)
        {
            if (dungeon)
            {
                if (Players.Count < 1 && !DisposingWorld)
                {
                    DisposingWorld = true;
                    log.InfoFormat("World {0}, ID:{1} has no players and is beginning the removal countdown of {2}MS.", Name, Id, WorldDisposeMS);
                    WorldTimer timer = new WorldTimer(WorldDisposeMS, (w, t) =>
                    {
                        RemoveFromTimer(w);
                    });
                    Timers.Add(timer);
                }
                else if (DisposingWorld && Players.Count > 0)
                {
                    DisposingWorld = false;
                    log.InfoFormat("World {0}, ID:{1} has cancelled removal.", Name, Id);
                }
            }

            try
            {
                if (IsLimbo) return;

                if (Timers != null)
                {
                    for (int i = 0; i < Timers.Count; i++)
                        if (Timers[i].Tick(this, time) && Timers.Count > 0)
                        {
                            Timers.RemoveAt(i);
                            i--;
                        }
                }

                foreach (var i in Players)
                    i.Value.Tick(time);

                //if (EnemiesCollision != null)
                //{
                //    foreach (Entity i in EnemiesCollision.GetActiveChunks(PlayersCollision))
                //        i.Tick(time);
                //    foreach (var i in StaticObjects.Where(x => x.Value is Decoy))
                //        i.Value.Tick(time);
                //}
                //else
                //{
                //    foreach (var i in Enemies)
                //        i.Value.Tick(time);
                //    foreach (var i in StaticObjects)
                //        i.Value.Tick(time);
                //}
                foreach (var i in Projectiles)
                    i.Value.Tick(time);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        protected void LoadMap(string json)
        {
            FromWorldMap(new MemoryStream(Json2Wmap.Convert(Manager, json)));
        }

        public void TickLogic(RealmTime time)
        {
            lock (Players)
            {
                if (Deleted)
                    return;

                if (EnemiesCollision != null)
                {
                    foreach (var i in EnemiesCollision.GetActiveChunks(PlayersCollision))
                    {
                        if (!(i is ItemEntity))
                        {
                            i.Tick(time);
                        }
                    }
                    foreach (var i in StaticObjects.Where(x => x.Value is Decoy))
                        i.Value.Tick(time);
                    foreach (var i in ItemEntities)
                        i.Value.Tick(time);
                }
                else
                {
                    foreach (var i in Enemies)
                        i.Value.Tick(time);
                    foreach (var i in StaticObjects)
                        i.Value.Tick(time);
                    foreach (var i in ItemEntities)
                        i.Value.Tick(time);

                }
                

                foreach (var i in Pets)
                    i.Value.Tick(time);

                
            }
        }
    }
}