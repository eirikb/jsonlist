using NLog;
using NLog.Config;

namespace Eirikb.SharePoint.JSONList
{
    public static class Log
    {
        public static LogLevel Level = LogLevel.Info;

        private static Logger _logger;

        public static Logger Current()
        {
            SimpleConfigurator.ConfigureForConsoleLogging(Level);
            _logger = LogManager.GetLogger("JSONList");
            return _logger;
        }
    }
}