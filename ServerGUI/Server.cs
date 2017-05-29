﻿using System;
using System.Threading;
using Library;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using ServerGUI.Commands;
using ServerGUI.ServerLogger;

namespace ServerGUI
{
    public class Server
    {
        public World World { get; set; }
        public NetServer NetServer { get; private set; }
        public LoggerManager LoggerManager;
        public MongoClient MongoClient { get; set; }

        public Timer UpdateTimer;

        public Server(LoggerManager loggerManager, World world, string mongoUsername, string mongoPass)
        {
            World = world;
            LoggerManager = loggerManager;

            NetPeerConfiguration config = new NetPeerConfiguration("testGame")
            {
                MaximumConnections = 32,
                Port = 9911
            };

            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            NetServer = new NetServer(config);

            MongoClient = new MongoClient("mongodb://" + mongoUsername + 
                ":" + mongoPass +
                "@cluster0-shard-00-00-kp9r9.mongodb.net:27017,cluster0-shard-00-01-kp9r9.mongodb.net:27017,cluster0-shard-00-02-kp9r9.mongodb.net:27017/test?ssl=true&replicaSet=Cluster0-shard-0&authSource=admin");

        }

        public void Run()
        {
            LoggerManager.ServerMsg("Server started at IP: " + "Unknown" + " and port: " + NetServer.Port);
            
            LoggerManager.ServerMsg("Waiting for new connections and updating world state to current ones");

            UpdateTimer = new Timer(_ => Update(), null, 0, 16 + 2 / 3);

            NetServer.Start();
            LoggerManager.ServerMsg("Server started...");

            while (true)
            {
                NetIncomingMessage inc;
                if ((inc = NetServer.ReadMessage()) == null) continue;
                switch (inc.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        var connectionType = (PacketTypes)inc.ReadByte();
                        switch (connectionType)
                        {
                            case PacketTypes.Login:
                            {
                                var login = new LoginCommand();
                                login.Run(LoggerManager, MongoClient, NetServer, inc, null, World);
                                continue;
                            }
                            case PacketTypes.Register:
                            {
                                var login = new RegisterCommand();
                                login.Run(LoggerManager, MongoClient, NetServer, inc, null, World);
                                continue;
                            }
                        }

                        var deniedReason = "Faulty connection type";
                        inc.SenderConnection.Deny(deniedReason);
                        LoggerManager.ServerMsg(deniedReason);
                        break;
                    case NetIncomingMessageType.Data:
                        Data(inc);
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        StatusChanged(inc);
                        break;
                }
            }
        }

        private void Data(NetIncomingMessage inc)
        {
            var command = CommandHandler.GetCommand(inc);
            command.Run(LoggerManager, null, NetServer, inc, null, World);
        }

        private void StatusChanged(NetIncomingMessage inc)
        {
            LoggerManager.ServerMsg(inc.SenderConnection + " status changed: " + inc.SenderConnection.Status);
            if (inc.SenderConnection.Status == NetConnectionStatus.Disconnected || inc.SenderConnection.Status == NetConnectionStatus.Disconnecting)
            {
                foreach (var player in World.Players)
                {
                    if (player.Conn == inc.SenderConnection)
                    {
                        World.Players.Remove(player);
                        LoggerManager.ServerMsg("Removed player " + player.Username);
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            for (int j = 0; j < World.Players.Count; j++)
            {
                var outer = false;
                for (int i = 0; i < World.Shots.Count; i++)
                {
                    var playerCircle = new Circle(World.Players[j].Radius, World.Players[j].X, World.Players[j].Y);
                    var shotCircle = new Circle(World.Shots[i].Radius, World.Shots[i].X, World.Shots[i].Y);

                    if (playerCircle.Intersect(shotCircle) && World.Shots[i].ParentName != World.Players[j].Username)
                    {
                        LoggerManager.ServerMsg(World.Players[j] + " was hit at " + new Vector2(World.Shots[i].X, World.Shots[i].Y));
                        World.Players[j].Health -= World.Shots[i].Damage;
                        World.Shots.RemoveAt(i);
                        i--;
                        if (World.Players[j].Health <= 0)
                        {
                            World.Players.RemoveAt(j);
                            j--;
                            outer = true;
                            var command2 = new SendPlayerHealthCommand();
                            command2.Run(LoggerManager, null, NetServer, null, World.Players[j], World);
                            continue;

                        }
                        var command3 = new SendPlayerCommand();
                        command3.Run(LoggerManager, null, NetServer, null, World.Players[j], World);
                    }
                }
                if (outer) break;
            }
            for (int i = 0; i < World.Shots.Count; i++)
            {
                if (World.Shots[i].CreatedTime + TimeSpan.FromSeconds(World.Shots[i].Duration) < DateTime.Now)
                {
                    World.Shots.RemoveAt(i);
                    i--;
                    continue;
                }
                MoveShot(World.Shots[i]);
            }
            var command = new SendAllShotsCommand();
            command.Run(LoggerManager, null, NetServer, null, null, World);
        }

        private void MoveShot(Shot shot)
        {
            shot.X = Angle.MoveAngle(new Vector2(shot.X, shot.Y), shot.Rotation, shot.Speed).X;
            shot.Y = Angle.MoveAngle(new Vector2(shot.X, shot.Y), shot.Rotation, shot.Speed).Y;
        }
    }
}
