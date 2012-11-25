using System;
using System.Collections.Generic;
using System.Linq;
using ManyConsole;

namespace Eirikb.SharePoint.JSONList
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var commands = GetCommands();
            var consoleRunner = new ConsoleModeCommand(GetCommands);
            commands = commands.Concat(new[] {consoleRunner});
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        private static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof (Program));
        }
    }
}