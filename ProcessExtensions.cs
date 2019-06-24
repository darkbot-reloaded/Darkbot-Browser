using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace DarkBotBrowser
{
    public static class ProcessExtensions
    {

        public static Process GetFlashProcess(this Process p)
        {
            var children = p.GetChildProcesses();

            foreach (var process in children)
            {
                var cmd = process.GetCommandLine();
                if (cmd.Contains("-type=ppapi"))
                {
                    return process;
                }
            }

            return null;
        }

        private static IEnumerable<Process> GetChildProcesses(this Process process)
        {
            var children = new List<Process>();
            var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={process.Id}");

            foreach (var mo in mos.Get())
            {
                children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
            }

            return children;
        }

        private static string GetCommandLine(this Process process)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (var objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }

        }
    }
}
