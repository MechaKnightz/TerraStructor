﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library;
using Lidgren.Network;
using MongoDB.Driver;
using ServerGUI.ServerLogger;

namespace ServerGUI.Commands
{
    public class SendPlayerHealthCommand : ICommand
    {
        public void Run(LoggerManager loggerManager, MongoClient mongoCLient, NetServer server, NetIncomingMessage inc, Player player, List<Player> allPlayers, List<GameRoom> gameRooms)
        {
            var outmsg = server.CreateMessage();

            outmsg.Write((byte)PacketTypes.PlayerHealth);

            outmsg.Write(player.Username);
            outmsg.Write(player.Health);

            server.SendToAll(outmsg, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
