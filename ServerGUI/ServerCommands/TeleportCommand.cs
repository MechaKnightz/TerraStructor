﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library;
using Microsoft.Xna.Framework;
using ServerGUI.ServerLogger;

namespace ServerGUI.ServerCommands
{
    public class TeleportCommand : IServerCommand
    {
        public CommandType Type { get; set; } = CommandType.Parameters;
        public string CommandName { get; set; } = "teleport";
        public int ParameterCount { get; set; } = 2;

        public bool Run(LoggerManager loggerManager, World world, List<string> parameters, out string runMessage)
        {
            var firstPlayer = world.Players.FirstOrDefault(x => x.Username == parameters[0]);
            var secondPlayer = world.Players.FirstOrDefault(x => x.Username == parameters[1]);

            if(firstPlayer == null || secondPlayer == null)

                try
                {
                    firstPlayer.X = secondPlayer.X + firstPlayer.Radius + secondPlayer.Radius;
                    firstPlayer.Y = secondPlayer.Y + firstPlayer.Radius + secondPlayer.Radius;
                }
                catch (Exception e)
                {
                    runMessage = e.Message;
                    throw;
                    return false;
                }
            runMessage = firstPlayer.Username + " teleported to " + secondPlayer.Username + " at pos: " +
                         new Vector2(secondPlayer.X, secondPlayer.Y);
            return true;
        }
    }
}