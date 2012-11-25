using System;
using System.Diagnostics;
using System.Reflection;
using ManyConsole;
using NLog;
using NLog.Config;

namespace Eirikb.SharePoint.JSONList
{
    internal abstract class JsonListCommand : ConsoleCommand
    {
        protected JsonListCommand()
        {
            HasOption("v|version", "Print version", s =>
                {
                    // http://stackoverflow.com/a/909583
                    var assembly = Assembly.GetExecutingAssembly();
                    var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    Console.WriteLine("Version: {0}", fvi.ProductVersion);
                });

            HasOption("ll|logLevel=", "Set LogLevel for NLog (Warn, Info, Debug)", u =>
                {
                    try
                    {
                        Log.Level = LogLevel.FromString(u);
                        SimpleConfigurator.ConfigureForConsoleLogging(Log.Level);
                    }
                    catch
                    {
                        Console.WriteLine("Unable to parse {0} to a LogLevel", u);
                    }
                });

            HasOption("lf|logFile=", "File for NLog", u => SimpleConfigurator.ConfigureForFileLogging(u, Log.Level));
        }
    }
}