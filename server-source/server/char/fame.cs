﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using db;
using MySql.Data.MySqlClient;

namespace server.@char
{
    [HttpUrlRequest("/char/fame")]
    internal class fame : RequestHandler
    {
        protected override void HandleRequest()
        {
            using (var db = new Database(Program.Settings.GetValue("conn")))
            {
                Account acc = db.GetAccount(int.Parse(Query["accountId"]));
                Char chr = db.LoadCharacter(acc, int.Parse(Query["charId"]));

                MySqlCommand cmd = db.CreateQuery();
                cmd.CommandText = @"SELECT time, killer, firstBorn FROM death WHERE accId=@accId AND chrId=@charId;";
                cmd.Parameters.AddWithValue("@accId", Query["accountId"]);
                cmd.Parameters.AddWithValue("@charId", Query["charId"]);
                int time;
                string killer;
                bool firstBorn;
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    time = Database.DateTimeToUnixTimestamp(rdr.GetDateTime("time"));
                    killer = rdr.GetString("killer");
                    firstBorn = rdr.GetBoolean("firstBorn");
                }

                using (var wtr = new StreamWriter(Context.Response.OutputStream))
                    wtr.Write(chr.FameStats.Serialize(Program.GameData, acc, chr, time, killer, firstBorn));
            }
        }
    }
}