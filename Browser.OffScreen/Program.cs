using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Browser.Core;
using Browser.OffScreen;
using CefSharp;
using CefSharp.OffScreen;

namespace Browser.OffScreen
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            HookAssemblyResolve(BrowserInitializer.PATH_CEF, BrowserInitializer.PATH_LIB);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += ApplicationOnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

            var stage1 = BrowserInitializer.InitStage1();

            Logger.GetLogger().Info($"Loaded libcef.dll -> {stage1}");


            LaunchBrowser();
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            Cef.ShutdownWithoutChecks();
        }

        private static void HookAssemblyResolve(params string[] folders)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var loadedAssembly =
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
                if (loadedAssembly != null)
                    return loadedAssembly;


                var n = new AssemblyName(args.Name);

                foreach (var dir in folders)
                {
                    var assembly = new[] { "*.dll", "*.exe" }.SelectMany(g => Directory.EnumerateFiles(dir, g))
                        .FirstOrDefault(f =>
                        {
                            try
                            {
                                return n.Name.Equals(AssemblyName.GetAssemblyName(f).Name,
                                    StringComparison.OrdinalIgnoreCase);
                            }
                            catch (BadImageFormatException)
                            {
                                return false;
                            }
                            catch (Exception ex)
                            {
                                throw new ApplicationException("Error loading assembly " + f, ex);
                            }
                        });

                    if (assembly != null)
                    {
                        Logger.GetLogger().Info($"Loading assembly {args.Name}...");
                        return Assembly.LoadFrom(assembly);
                    }
                }

                throw new ApplicationException("Assembly " + args.Name + " not found");
            };
        }

        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Logger.GetLogger().Error("[ApplicationOnThreadException] ", e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.GetLogger().Error("[ApplicationOnThreadException] ", (Exception)e.ExceptionObject);
        }

        private static void LaunchBrowser()
        {
            Cef.EnableHighDPISupport();

            var cefSettings = new CefSettings
            {
                CachePath = Path.Combine(BrowserInitializer.PATH_CEF, "cache"),
                LogFile = Path.Combine(BrowserInitializer.PATH_CEF, "debug.log"),
                BrowserSubprocessPath = Path.Combine(BrowserInitializer.PATH_CEF, "CefSharp.BrowserSubprocess.exe"),
                LocalesDirPath = Path.Combine(BrowserInitializer.PATH_CEF, "locales"),
                ResourcesDirPath = Path.Combine(BrowserInitializer.PATH_CEF),
                MultiThreadedMessageLoop = true,
                UserAgent = "BigpointClient/1.4.6",
                CommandLineArgsDisabled = false,
                LogSeverity = LogSeverity.Verbose
            };
            cefSettings.CefCommandLineArgs.Remove("enable-system-flash");
            cefSettings.CefCommandLineArgs.Add("enable-system-flash", "0");
            cefSettings.CefCommandLineArgs.Add("ppapi-flash-path", Path.Combine(BrowserInitializer.PATH_LIB, "pepflashplayer64_32_0_0_207.dll"));
            cefSettings.CefCommandLineArgs.Add("ppapi-flash-version", "32.0.0.207");
            cefSettings.CefCommandLineArgs.Add("force-device-scale-factor", "1");
            cefSettings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            cefSettings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            cefSettings.CefCommandLineArgs.Add("disable-direct-write", "1");
            cefSettings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache", "1");

            cefSettings.CefCommandLineArgs.Add("--js-flags", "--max_old_space_size=300");

            cefSettings.SetOffScreenRenderingBestPerformanceArgs();

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

            Cef.Initialize(cefSettings, false, browserProcessHandler: null);

            Logger.GetLogger().Info("Initialized Cef... Launching browser...");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
    }
}
