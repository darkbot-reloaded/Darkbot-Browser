using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace DarkBotBrowser
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var libraryLoader = new CefLibraryHandle(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "libcef.dll"));
            libraryLoader.Dispose();

            HookAssemblyResolve("\\resources");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += ApplicationOnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
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
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
                if (loadedAssembly != null)
                    return loadedAssembly;


                var n = new AssemblyName(args.Name);

                foreach (var dir in folders)
                {
                    var assembly = new[] { "*.dll", "*.exe" }.SelectMany(g => Directory.EnumerateFiles(dir, g)).FirstOrDefault(f =>
                    {
                        try { return n.Name.Equals(AssemblyName.GetAssemblyName(f).Name, StringComparison.OrdinalIgnoreCase); }
                        catch (BadImageFormatException) { return false; }
                        catch (Exception ex) { throw new ApplicationException("Error loading assembly " + f, ex); }
                    });

                    if (assembly != null)
                        return Assembly.LoadFrom(assembly);
                }

                throw new ApplicationException("Assembly " + args.Name + " not found");
            };
        }

        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }

        static void LaunchBrowser()
        {
            Cef.EnableHighDPISupport();
            
            var cefSettings = new CefSettings()
            {
                CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources\\cache"),
                LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources\\debug.log"),
                BrowserSubprocessPath =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources\\CefSharp.BrowserSubprocess.exe"),
                LocalesDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources\\locales\\"),
                ResourcesDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources"),
                MultiThreadedMessageLoop = true,
                UserAgent =
                    "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36",
                CefCommandLineArgs =
                {
                },
                CommandLineArgsDisabled = false,
            };
            cefSettings.LogSeverity = LogSeverity.Verbose;
            cefSettings.CefCommandLineArgs.Remove("enable-system-flash");
            cefSettings.CefCommandLineArgs.Add("enable-system-flash", "1");
            cefSettings.CefCommandLineArgs.Add("force-device-scale-factor", "1");
            cefSettings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            cefSettings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            cefSettings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache", "1");
            Cef.Initialize(cefSettings, performDependencyCheck: false, browserProcessHandler: null);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmBrowser());
        }
    }
}
