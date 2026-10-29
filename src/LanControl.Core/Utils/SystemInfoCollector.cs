using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using LanControl.Core.Models;

namespace LanControl.Core.Utils
{
    public static class SystemInfoCollector
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private static PerformanceCounter _cpuCounter;

        static SystemInfoCollector()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpuCounter.NextValue();
            }
            catch { }
        }

        public static string GetLocalIpAddress()
        {
            try
            {
                // Prefer active IPv4 on operational Ethernet or Wi-Fi
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    IPInterfaceProperties props = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }

        public static string GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        PhysicalAddress mac = ni.GetPhysicalAddress();
                        if (mac != null && mac.GetAddressBytes().Length > 0)
                        {
                            return string.Join(":", System.Text.RegularExpressions.Regex.Split(mac.ToString(), "(?<=\\G.{2})(?!$)"));
                        }
                    }
                }
            }
            catch { }
            return "00:00:00:00:00:00";
        }

        public static void GetMemoryInfo(out long totalMb, out long availableMb)
        {
            totalMb = 4096;
            availableMb = 2048;

            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    totalMb = (long)(memStatus.ullTotalPhys / (1024 * 1024));
                    availableMb = (long)(memStatus.ullAvailPhys / (1024 * 1024));
                }
            }
            catch { }
        }

        public static int GetCpuUsagePercent()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    return (int)Math.Round(_cpuCounter.NextValue());
                }
            }
            catch { }
            return 0;
        }

        public static List<ProcessItem> GetRunningProcesses()
        {
            List<ProcessItem> list = new List<ProcessItem>();
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    try
                    {
                        long mem = 0;
                        try { mem = p.WorkingSet64; } catch { }

                        list.Add(new ProcessItem
                        {
                            Id = p.Id,
                            Name = p.ProcessName,
                            Title = string.IsNullOrEmpty(p.MainWindowTitle) ? "" : p.MainWindowTitle,
                            MemoryBytes = mem,
                            MemoryFormatted = string.Format("{0:F1} MB", mem / (1024.0 * 1024.0))
                        });
                    }
                    catch { }
                }

                list.Sort((a, b) => b.MemoryBytes.CompareTo(a.MemoryBytes));
            }
            catch { }
            return list;
        }

        public static bool KillProcess(int processId)
        {
            try
            {
                Process p = Process.GetProcessById(processId);
                p.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ExecutePowerAction(string action)
        {
            try
            {
                if (action.Equals("Reboot", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start("shutdown.exe", "/r /t 0 /f");
                }
                else if (action.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start("shutdown.exe", "/s /t 0 /f");
                }
            }
            catch { }
        }
    }
}
