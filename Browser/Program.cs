using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace Browser
{
    internal static class Program
    {
        public static readonly string PATH_RESOURCES = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        private static readonly string PATH_CEF = Path.Combine(PATH_RESOURCES, "Cef");
        private static readonly string PATH_LIB = Path.Combine(PATH_RESOURCES, "Lib");

        [STAThread]
        private static void Main()
        {
            HookAssemblyResolve(PATH_CEF, PATH_LIB);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += ApplicationOnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

            var libraryLoader = new CefLibraryHandle(Path.Combine(PATH_CEF, "libcef.dll"));
            Logger.GetLogger().Info($"Loaded libcef.dll -> {!libraryLoader.IsInvalid}");
            libraryLoader.Dispose();

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
                    var assembly = new[] {"*.dll", "*.exe"}.SelectMany(g => Directory.EnumerateFiles(dir, g))
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
            Logger.GetLogger().Error("[ApplicationOnThreadException] ", (Exception) e.ExceptionObject);
        }

        private static void LaunchBrowser()
        {
            Cef.EnableHighDPISupport();

            var cefSettings = new CefSettings
            {
                CachePath = Path.Combine(PATH_CEF, "cache"),
                LogFile = Path.Combine(PATH_CEF, "debug.log"),
                BrowserSubprocessPath = Path.Combine(PATH_CEF, "CefSharp.BrowserSubprocess.exe"),
                LocalesDirPath = Path.Combine(PATH_CEF, "locales"),
                ResourcesDirPath = Path.Combine(PATH_CEF),
                MultiThreadedMessageLoop = true,
                UserAgent = "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
                CommandLineArgsDisabled = false,
                LogSeverity = LogSeverity.Verbose
            };
            cefSettings.CefCommandLineArgs.Remove("enable-system-flash");
            cefSettings.CefCommandLineArgs.Add("enable-system-flash", "0");
            cefSettings.CefCommandLineArgs.Add("ppapi-flash-path", Path.Combine(PATH_LIB, "pepflashplayer64_32_0_0_207.dll"));
            cefSettings.CefCommandLineArgs.Add("ppapi-flash-version", "32.0.0.207");
            cefSettings.CefCommandLineArgs.Add("force-device-scale-factor", "1");
            cefSettings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            cefSettings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            cefSettings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache", "1");
            Cef.Initialize(cefSettings, false, browserProcessHandler: null);
            Logger.GetLogger().Info("Initialized Cef... Launching browser...");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}