using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            TryEnsureUacReady();

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
            AgentConfig config = AgentConfig.Load(configPath);

            // Parse optional CLI overrides: Agent.exe [gateway_host] [port] [vlan_tag]
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                config.GatewayHost = args[0];
            }
            if (args.Length > 1)
            {
                int port;
                if (int.TryParse(args[1], out port) && port > 0)
                {
                    config.GatewayPort = port;
                }
            }
            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
            {
                config.VlanTag = args[2];
            }

            config.Save(configPath);

            string mutexName = "Global\\LanControl_Agent_SingleInstance_" + (string.IsNullOrEmpty(config.VlanTag) ? "Default" : config.VlanTag.Replace(" ", "_").Replace(":", "_").Replace("\\", "_"));
            bool createdNew;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, mutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Instance for this VLAN is already running in background. Exit cleanly to prevent duplicates.
                    return;
                }

                Application.Run(new AgentTrayApp(config));
            }
        }

        private static void TryEnsureUacReady()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        object pVal = key.GetValue("PromptOnSecureDesktop");
                        if (pVal != null && Convert.ToInt32(pVal) != 0)
                        {
                            key.SetValue("PromptOnSecureDesktop", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        object uipiVal = key.GetValue("EnableUIPI");
                        if (uipiVal != null && Convert.ToInt32(uipiVal) != 0)
                        {
                            key.SetValue("EnableUIPI", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        object uVal = key.GetValue("ConsentPromptBehaviorUser");
                        if (uVal == null || Convert.ToInt32(uVal) != 1)
                        {
                            key.SetValue("ConsentPromptBehaviorUser", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
