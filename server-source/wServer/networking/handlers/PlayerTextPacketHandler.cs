﻿using wServer.networking.cliPackets;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.networking.handlers
{
    internal class PlayerTextPacketHandler : PacketHandlerBase<PlayerTextPacket>
    {
        public override PacketID ID
        {
            get { return PacketID.PlayerText; }
        }

        protected override void HandlePacket(Client client, PlayerTextPacket packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet.Text));
        }

        private void Handle(Player player, RealmTime time, string text)
        {
            if (player.Owner == null) return;
            if (!player.NameChosen)
            {
                player.SendInfo("Choose a name in order to use the player chat.");
                return;
            }

            if (text[0] == '/')
            {
                player.Manager.Commands.Execute(player, time, text);
            }
            else if (text[0] == '!') //chatbot commands directive
            {
                var index = text.IndexOf(' ');
                var cmd = text.Substring(1, index == -1 ? text.Length - 1 : index - 1);

                if (cmd == "" || cmd == null) player.Manager.Chat.Say(player, text);
                else
                {
                    player.Manager.Chat.Say(player, text);
                    player.Manager.ChatBotCommands.Execute(player, time, text);
                }
            }
            else player.Manager.Chat.Say(player, text);
        }
    }
}