using System.IO;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace Browser.Core
{
    public class Logger
    {
        private static string PATH_LOG = Path.Combine("Resources", "debug.log");
        private static ILog _log;

        public static ILog GetLogger()
        {
            if (_log == null)
            {
                ConfigureFileAppender(PATH_LOG);
                _log = LogManager.GetLogger("FileLogger");
            }

            return _log;
        }

        private static void ConfigureFileAppender(string logFile)
        {
            var fileAppender = GetFileAppender(logFile);
            BasicConfigurator.Configure(fileAppender);
            ((Hierarchy) LogManager.GetRepository()).Root.Level = Level.All;
        }

        private static IAppender GetFileAppender(string logFile)
        {
            var layout = new PatternLayout("%date{dd.mm.yyyy HH:mm:ss} [%level] - %message%newline");
            layout.ActivateOptions();

            var appender = new FileAppender
            {
                Name = "FileLogger",
                File = logFile,
                AppendToFile = true,
                Encoding = Encoding.UTF8,
                Threshold = Level.All,
                Layout = layout
            };

            appender.ActivateOptions();

            return appender;
        }
    }
}