using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Gateway
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int port = 8443;
            bool consoleMode = false;
            bool startMinimized = false;

            if (args != null)
            {
                foreach (string rawArg in args)
                {
                    string a = rawArg.Trim().ToLowerInvariant();
                    if (a == "--console" || a == "-c" || a == "--nogui")
                    {
                        consoleMode = true;
                    }
                    else if (a == "--minimized" || a == "-m" || a == "--tray")
                    {
                        startMinimized = true;
                    }
                    else
                    {
                        int p;
                        if (int.TryParse(a, out p) && p > 0 && p <= 65535)
                        {
                            port = p;
                        }
                    }
                }
            }

            if (port == 8443)
            {
                try
                {
                    string cfgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gateway_config.json");
                    GatewayConfig cfg = GatewayConfig.Load(cfgPath);
                    if (cfg != null && cfg.Port > 0 && cfg.Port <= 65535)
                    {
                        port = cfg.Port;
                    }
                }
                catch { }
            }

            if (consoleMode)
            {
                RunConsole(port);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new GatewayMainForm(port, startMinimized));
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        private static void RunConsole(int port)
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                AllocConsole();
            }

            try
            {
                Console.Title = "LAN Control Central Gateway - Multi-VLAN Relay Server";
            }
            catch { }

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("==================================================================");
                Console.WriteLine("        LAN CONTROL CENTRAL GATEWAY - MULTI-VLAN RELAY HUB         ");
                Console.WriteLine("==================================================================");
                Console.ResetColor();
                Console.WriteLine("Reverse Connection Port: {0}", port);
                Console.WriteLine("Type 'help' for available management commands.\n");
            }
            catch { }

            GatewayServer server = new GatewayServer(port);
            server.Start();

            bool running = true;
            while (running)
            {
                Console.Write("Gateway> ");
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToLowerInvariant();

                switch (cmd)
                {
                    case "help":
                    case "?":
                        Console.WriteLine("\nAvailable Commands:");
                        Console.WriteLine("  status              - Show server status and connected stats");
                        Console.WriteLine("  clients             - List all active connected agents");
                        Console.WriteLine("  vlan                - Show agent count grouped by VLAN");
                        Console.WriteLine("  scan <start> <end>  - Quick subnet IP scanner (e.g. scan 192.168.1.1 192.168.1.50)");
                        Console.WriteLine("  clear               - Clear console screen");
                        Console.WriteLine("  exit / quit         - Stop server and exit\n");
                        break;

                    case "status":
                        List<AgentInfo> agents = server.GetActiveAgents();
                        Console.WriteLine("\n[Gateway Status]");
                        Console.WriteLine("Listening Port: {0}", server.Port);
                        Console.WriteLine("Total Online Agents: {0}", agents.Count);
                        Dictionary<string, int> stats = server.GetVlanStatistics();
                        foreach (KeyValuePair<string, int> kv in stats)
                        {
                            Console.WriteLine("  * VLAN [{0}]: {1} agents", kv.Key, kv.Value);
                        }
                        Console.WriteLine();
                        break;

                    case "clients":
                    case "list":
                        List<AgentInfo> clientList = server.GetActiveAgents();
                        Console.WriteLine("\n--- Active Agents ({0}) ---", clientList.Count);
                        Console.WriteLine("{0,-12} {1,-18} {2,-16} {3,-15} {4,-8} {5,-8}", "Agent ID", "Hostname", "IP Address", "VLAN", "CPU", "RAM (MB)");
                        Console.WriteLine(new string('-', 85));
                        foreach (AgentInfo a in clientList)
                        {
                            string shortId = a.AgentId.Length > 8 ? a.AgentId.Substring(0, 8) : a.AgentId;
                            Console.WriteLine("{0,-12} {1,-18} {2,-16} {3,-15} {4,-8} {5,-8}",
                                shortId, a.Hostname, a.IpAddress, a.VlanTag, a.CpuUsagePercent + "%", a.AvailableMemoryMb);
                        }
                        Console.WriteLine();
                        break;

                    case "vlan":
                        Dictionary<string, int> vlanStats = server.GetVlanStatistics();
                        Console.WriteLine("\n--- Agents By VLAN ---");
                        foreach (KeyValuePair<string, int> kv in vlanStats)
                        {
                            Console.WriteLine("  VLAN Tag '{0}': {1} agent(s)", kv.Key, kv.Value);
                        }
                        Console.WriteLine();
                        break;

                    case "scan":
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Usage: scan <start_ip> <end_ip> [ports]");
                            Console.WriteLine("Example: scan 192.168.1.1 192.168.1.50 8443,3389");
                            break;
                        }

                        ScanConfig cfg = new ScanConfig
                        {
                            StartIp = parts[1],
                            EndIp = parts[2],
                            PortList = parts.Length > 3 ? parts[3] : "8443, 3389, 22, 80"
                        };

                        Console.WriteLine("Scanning range {0} - {1}...", cfg.StartIp, cfg.EndIp);
                        SubnetScanner scanner = new SubnetScanner();
                        List<ScanHostResult> scanResults = scanner.Scan(cfg);
                        Console.WriteLine("\n--- Discovered {0} Active Hosts ---", scanResults.Count);
                        foreach (ScanHostResult r in scanResults)
                        {
                            Console.WriteLine("  {0,-16} | Host: {1,-20} | Ports: {2,-15} | Ping: {3}ms | Agent: {4}",
                                r.IpAddress, r.Hostname, r.OpenPorts, r.PingMs, r.HasAgent ? "YES" : "No");
                        }
                        Console.WriteLine();
                        break;

                    case "clear":
                    case "cls":
                        Console.Clear();
                        break;

                    case "exit":
                    case "quit":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Unknown command. Type 'help' for options.");
                        break;
                }
            }

            server.Stop();
        }
    }
}
