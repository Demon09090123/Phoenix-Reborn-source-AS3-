﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using db;
using log4net;
using log4net.Core;
using wServer.networking.cliPackets;
using wServer.networking.svrPackets;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.networking
{
    public enum ProtocalStage
    {
        Connected,
        Handshaked,
        Ready,
        Disconnected
    }

    public class Client : IDisposable
    {
        public const int LOCKED_LIST_ID = 0;
        public const int IGNORED_LIST_ID = 1;

        private static readonly ILog log = LogManager.GetLogger(typeof (Client));

        private readonly Socket skt;
        public string Copyright = "6d450d6aee98e461f0280a196593eebbfb8227a7";
        public string Version = "3";
        private NetworkHandler handler;
        private bool reconnected;
        internal int targetWorld = -1;

        public Client(RealmManager manager, Socket skt)
        {
            this.skt = skt;
            Manager = manager;
            ReceiveKey =
                new RC4(new byte[] {0x31, 0x1f, 0x80, 0x69, 0x14, 0x51, 0xc7, 0x1b, 0x09, 0xa1, 0x3a, 0x2a, 0x6e});
            SendKey = new RC4(new byte[] {0x72, 0xc5, 0x58, 0x3c, 0xaf, 0xb6, 0x81, 0x89, 0x95, 0xcb, 0xd7, 0x4b, 0x80});
        }

        public RC4 ReceiveKey { get; private set; }
        public RC4 SendKey { get; private set; }

        public RealmManager Manager { get; private set; }

        public Socket Socket
        {
            get { return skt; }
        }

        public Char Character { get; internal set; }
        public Account Account { get; internal set; }
        public ProtocalStage Stage { get; internal set; }
        public Player Player { get; internal set; }
        public wRandom Random { get; internal set; }

        public void BeginProcess()
        {
            log.InfoFormat("Received client @ {0}.", skt.RemoteEndPoint);
            handler = new NetworkHandler(this, skt);
            handler.BeginHandling();
        }

        public void SendPacket(Packet pkt, Action callback=null)
        {
            handler?.SendPacket(pkt, callback);
        }

        public void SendPackets(IEnumerable<Packet> pkts)
        {
            handler?.SendPackets(pkts);
        }

        public bool IsReady()
        {
            if (Stage == ProtocalStage.Disconnected)
                return false;
            if (Stage == ProtocalStage.Ready &&
                (Player == null || Player != null && Player.Owner == null))
                return false;
            return true;
        }

        internal void ProcessPacket(Packet pkt)
        {
            try
            {
                log.Logger.Log(typeof (Client), Level.Verbose,
                    $"Handling packet '{pkt.ID}'...", null);
                if (pkt.ID == PacketID.Packet) return;
                IPacketHandler handler;
                if (!PacketHandlers.Handlers.TryGetValue(pkt.ID, out handler))
                    log.WarnFormat("Unhandled packet '{0}'.", pkt.ID);
                else
                    handler.Handle(this, (ClientPacket) pkt);
            }
            catch (Exception e)
            {
                log.Error($"Error when handling packet '{pkt}'...", e);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
            {
                if (Stage == ProtocalStage.Disconnected) return;
                Save();
                Stage = ProtocalStage.Disconnected;
                if (Account != null)
                    DisconnectFromRealm();
                skt.Close();
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        public void Save()
        {
            //Safe copy variables just in case someone sets them to null after we checked for "if (chr != null)"
            var character = Character;
            var account = Account;
            if (character == null) return;
            Player.SaveToCharacter();

            log.DebugFormat("Saving character...");
            Manager.Data.AddDatabaseOperation(db => db.SaveCharacter(account, character));
        }

        //Following must execute, network loop will discard disconnected client, so logic loop
        private void DisconnectFromRealm()
        {
            Manager.Logic.AddPendingAction(t =>
            {
                if (reconnected)
                    return;
                if (Player != null)
                {
                    if (Player.Party != null)
                    {
                        if (Player.Party.Leader == Player)
                            Player.Party.Disband();
                        else
                            Player.Party.RemoveMember(Player);
                    }
                    Player.SaveToCharacter();
                }
                Manager.Disconnect(this);
            }, PendingPriority.Destruction);
        }

        public void Reconnect(ReconnectPacket pkt)
        {
            log.InfoFormat("Reconnecting client @ {0} to {1}...", skt.RemoteEndPoint, pkt.Name);
            Player?.ApplyConditionEffect(new ConditionEffect
            {
                DurationMS = -1,
                Effect = ConditionEffectIndex.Invincible
            });
            Manager.Logic.AddPendingAction(t =>
            {
                reconnected = true;
                if (Player != null)
                {
                    World world = Manager.GetWorld(pkt.GameId);
                    if (Player.Party != null && Player.Party.World != world && pkt.GameId != World.VAULT_ID)
                    {
                        if (Player.Party.Leader == Player)
                            Player.Party.World = world;
                        else
                            Player.Party.RemoveMember(Player);
                    }
                    if(Player.Party != null && Player.Party.World == world && Player.Party.Leader != Player)
                        Player.Party.Members.Remove(Player);
                    Player.SaveToCharacter();
                }
                Save();
                SendPacket(pkt, () => Manager.Disconnect(this));
            }, PendingPriority.Destruction);
        }

        public void Dispose()
        {
            if (Stage != ProtocalStage.Disconnected) return;
            handler?.Dispose();
            Account = null;
            Character = null;
            handler = null;
        }
    }
}