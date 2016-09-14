﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using db;
using wServer.networking;
using wServer.realm.entities;
using wServer.realm.terrain;

namespace wServer.realm.worlds
{
    public class Vault : World
    {
        private readonly Client client;

        private readonly ConcurrentDictionary<Tuple<Container, VaultChest>, int> vaultChests =
            new ConcurrentDictionary<Tuple<Container, VaultChest>, int>();

        public Vault(bool isLimbo, Client client = null)
        {
            Id = VAULT_ID;
            Identifier = "Vault";
            Name = "Vault";
            Background = 2;
            Difficulty = 0;
            IsLimbo = isLimbo;
            SetMusic("world/Vault");
            SetDisposeTime(5 * 1000);
            this.client = client;
        }

        protected override void Init()
        {
            if (!IsLimbo)
            {
                base.FromWorldMap(
                    typeof (RealmManager).Assembly.GetManifestResourceStream("wServer.realm.worlds.friendly.vault.wmap"));
                InitVault();
            }
        }

        private void InitVault()
        {
            var vaultChestPosition = new List<IntPoint>();
            var spawn = new IntPoint(0, 0);

            int w = Map.Width;
            int h = Map.Height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    WmapTile tile = Map[x, y];
                    if (tile.Region == TileRegion.Spawn)
                        spawn = new IntPoint(x, y);
                    else if (tile.Region == TileRegion.Vault)
                    {
                        vaultChestPosition.Add(new IntPoint(x, y));
                    }
                }
            vaultChestPosition.Sort((x, y) => Comparer<int>.Default.Compare(
                (x.X - spawn.X) * (x.X - spawn.X) + (x.Y - spawn.Y) * (x.Y - spawn.Y),
                (y.X - spawn.X) * (y.X - spawn.X) + (y.Y - spawn.Y) * (y.Y - spawn.Y)));

            List<VaultChest> chests = client.Account.Vault.Chests;
            for (int i = 0; i < chests.Count; i++)
            {
                var con = new Container(client.Manager, 0x0504, null, false);
                Item[] inv =
                    chests[i].Items.Select(_ => _ == 0xffff ? null : client.Manager.GameData.Items[_]).ToArray();
                ItemData[] dats = chests[i].Datas;
                for (int j = 0; j < 8; j++)
                {
                    con.Inventory[j] = inv[j];
                    con.Inventory.Data[j] = dats[j];
                }
                con.Move(vaultChestPosition[0].X + 0.5f, vaultChestPosition[0].Y + 0.5f);
                EnterWorld(con);
                vaultChestPosition.RemoveAt(0);

                vaultChests[new Tuple<Container, VaultChest>(con, chests[i])] = con.UpdateCount;
            }
            foreach (IntPoint i in vaultChestPosition)
            {
                var x = new SellableObject(client.Manager, 0x0505);
                x.Move(i.X + 0.5f, i.Y + 0.5f);
                EnterWorld(x);
            }
        }

        public void AddChest(VaultChest chest, Entity original)
        {
            var con = new Container(client.Manager, 0x0504, null, false);
            Item[] inv = chest.Items.Select(_ => _ == 0xffff ? null : Manager.GameData.Items[_]).ToArray();
            ItemData[] dats = chest.Datas;
            for (int j = 0; j < 8; j++)
            {
                con.Inventory[j] = inv[j];
                con.Inventory.Data[j] = dats[j];
            }
            con.Move(original.X, original.Y);
            LeaveWorld(original);
            EnterWorld(con);

            vaultChests[new Tuple<Container, VaultChest>(con, chest)] = con.UpdateCount;
        }

        public override World GetInstance(Client client)
        {
            return Manager.AddWorld(new Vault(false, client));
        }

        public override void Tick(RealmTime time)
        {
            base.Tick(time);

            foreach (var i in vaultChests)
            {
                if (i.Key.Item1.UpdateCount > i.Value)
                {
                    i.Key.Item2._Items =
                        Utils.GetCommaSepString(
                            i.Key.Item1.Inventory.Take(8).Select(_ => _?.ObjectType ?? 65535).ToArray());
                    i.Key.Item2._Datas =
                        ItemDataList.GetJson(
                            i.Key.Item1.Inventory.Data.Take(8).ToArray());
                    Manager.Data.AddDatabaseOperation(db =>
                    {
                        db.SaveChest(client.Account, i.Key.Item2);
                    });
                    vaultChests[i.Key] = i.Key.Item1.UpdateCount;
                }
            }
        }
    }
}