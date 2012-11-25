using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ManyConsole;

namespace Eirikb.SharePoint.JSONList
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("JSONList version {0} by eirikb@eirikb.no", GetVersion());

            var commands = GetCommands();
            var consoleRunner = new ConsoleModeCommand(GetCommands);
            commands = commands.Concat(new[] {consoleRunner});
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        private static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof (Program));
        }

        private static string GetVersion()
        {
            // http://stackoverflow.com/a/909583
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.ProductVersion;
        }
    }
}