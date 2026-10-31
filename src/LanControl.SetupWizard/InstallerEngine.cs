using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LanControl.SetupWizard
{
    public enum InstallEdition
    {
        AgentClient,
        ServerMaster,
        FullSuite
    }

    public class InstallConfig
    {
        public InstallEdition Edition { get; set; }
        public string TargetDirectory { get; set; }

        // Agent Options
        public string GatewayHost { get; set; }
        public int GatewayPort { get; set; }
        public string VlanTag { get; set; }
        public bool AgentAutoStart { get; set; }
        public bool EnableUacRemoteSupport { get; set; }

        // Server Options
        public int ServerPort { get; set; }
        public bool OpenFirewall { get; set; }
        public bool ServerAutoStart { get; set; }

        // Shortcuts & Launch
        public bool CreateDesktopShortcuts { get; set; }
        public bool LaunchAfterInstall { get; set; }

        public InstallConfig()
        {
            Edition = InstallEdition.FullSuite;
            TargetDirectory = @"C:\Program Files\lancontrol";
            GatewayHost = "127.0.0.1";
            GatewayPort = 8443;
            VlanTag = "VLAN 10 - Phòng Kỹ Thuật";
            AgentAutoStart = true;
            EnableUacRemoteSupport = true;
            ServerPort = 8443;
            OpenFirewall = true;
            ServerAutoStart = true;
            CreateDesktopShortcuts = true;
            LaunchAfterInstall = true;
        }
    }

    public class InstallerEngine
    {
        public event Action<int, string> ProgressChanged;

        public static string ResolveSourceFile(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            List<string> searchPaths = new List<string>
            {
                Path.Combine(baseDir, fileName),
                Path.Combine(baseDir, "bin", fileName),
                Path.Combine(baseDir, "setup", fileName),
                Path.Combine(baseDir, "..", fileName),
                Path.Combine(baseDir, "..", "bin", fileName),
                Path.Combine(baseDir, "..", "setup", fileName),
                Path.Combine(baseDir, "files", fileName),
                Path.Combine(@"D:\Antigravity\LanControl\bin", fileName),
                Path.Combine(@"D:\Antigravity\LanControl\setup", fileName),
                Path.Combine(@"D:\Antigravity\LanControl", fileName),
                Path.Combine(@"D:\lancontrol\bin", fileName),
                Path.Combine(@"D:\lancontrol\setup", fileName),
                Path.Combine(@"D:\lancontrol", fileName)
            };

            foreach (string path in searchPaths)
            {
                try
                {
                    if (File.Exists(path)) return Path.GetFullPath(path);
                }
                catch { }
            }
            return null;
        }

        public static List<string> GetLocalLanIps()
        {
            List<string> ipList = new List<string>();
            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                foreach (IPAddress ip in addresses)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        if (!ipStr.StartsWith("127.") && !ipList.Contains(ipStr))
                        {
                            ipList.Add(ipStr);
                        }
                    }
                }
            }
            catch { }

            if (ipList.Count == 0)
            {
                ipList.Add("127.0.0.1");
            }
            return ipList;
        }

        public static void LoadExistingConfig(string targetDir, InstallConfig config)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string[] dirsToCheck = new string[]
                {
                    targetDir,
                    baseDir,
                    Path.Combine(baseDir, "..")
                };

                foreach (string dir in dirsToCheck)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                    string agentCfgPath = Path.Combine(dir, "agent_config.json");
                    if (File.Exists(agentCfgPath))
                    {
                        string json = File.ReadAllText(agentCfgPath, Encoding.UTF8);
                        string host = ExtractJsonString(json, "GatewayHost");
                        int port = ExtractJsonInt(json, "GatewayPort", 8443);
                        string vlan = ExtractJsonString(json, "VlanTag");

                        if (!string.IsNullOrEmpty(host)) config.GatewayHost = host;
                        if (port > 0 && port <= 65535) config.GatewayPort = port;
                        if (!string.IsNullOrEmpty(vlan)) config.VlanTag = vlan;
                        break;
                    }
                }

                foreach (string dir in dirsToCheck)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                    string gwCfgPath = Path.Combine(dir, "gateway_config.json");
                    if (File.Exists(gwCfgPath))
                    {
                        string json = File.ReadAllText(gwCfgPath, Encoding.UTF8);
                        int port = ExtractJsonInt(json, "Port", ExtractJsonInt(json, "ListenPort", 8443));
                        if (port > 0 && port <= 65535) config.ServerPort = port;
                        break;
                    }
                }
            }
            catch { }
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static int ExtractJsonInt(string json, string key, int defaultValue)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
            int val;
            if (m.Success && int.TryParse(m.Groups[1].Value, out val))
            {
                return val;
            }
            return defaultValue;
        }

        public bool ExecuteInstallation(InstallConfig config, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                Report(5, "Đang khởi tạo tiến trình cài đặt 1-Click...");

                string targetDir = EnsureLanControlDirectory(config.TargetDirectory);
                config.TargetDirectory = targetDir;

                Report(10, "Đang chuẩn bị thư mục đích: " + targetDir);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                bool installAgent = (config.Edition == InstallEdition.AgentClient || config.Edition == InstallEdition.FullSuite);
                bool installServer = (config.Edition == InstallEdition.ServerMaster || config.Edition == InstallEdition.FullSuite);

                // Stop running processes before copying
                if (installAgent)
                {
                    Report(14, "Đang dừng tiến trình Agent cũ (nếu đang chạy)...");
                    TryKillRunningProcess("LanControl.Agent");
                }
                if (installServer)
                {
                    Report(18, "Đang dừng tiến trình Gateway cũ (nếu đang chạy)...");
                    TryKillRunningProcess("LanControl.Gateway");
                }
                System.Threading.Thread.Sleep(250);

                // Identify required files
                List<string> requiredFiles = new List<string> { "LanControl.Core.dll", "icon.ico", "about.md" };
                if (installAgent)
                {
                    requiredFiles.Add("LanControl.Agent.exe");
                }
                if (installServer)
                {
                    requiredFiles.Add("LanControl.Gateway.exe");
                    requiredFiles.Add("LanControl.Control.exe");
                }

                // Copy files
                int fileIndex = 0;
                foreach (string fileName in requiredFiles)
                {
                    fileIndex++;
                    int pct = 20 + (int)((fileIndex / (float)requiredFiles.Count) * 35);
                    string src = ResolveSourceFile(fileName);
                    string dest = Path.Combine(targetDir, fileName);

                    if (string.IsNullOrEmpty(src) || !File.Exists(src))
                    {
                        if (!File.Exists(dest))
                        {
                            // Optional docs/icons shouldn't fail the whole install if missing, only .exe/.dll
                            if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                errorMessage = string.Format("Không tìm thấy tệp nguồn '{0}'. Vui lòng kiểm tra lại bộ cài (hoặc chạy build.bat)!", fileName);
                                return false;
                            }
                            continue;
                        }
                        Report(pct, string.Format("Giữ nguyên tệp sẵn có: {0}", fileName));
                        continue;
                    }

                    Report(pct, string.Format("Đang sao chép: {0}...", fileName));
                    if (!string.Equals(Path.GetFullPath(src), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Copy(src, dest, true);
                        }
                        catch (IOException)
                        {
                            TryKillRunningProcess(fileName);
                            System.Threading.Thread.Sleep(350);
                            File.Copy(src, dest, true);
                        }
                    }
                }

                // Also copy HUONG_DAN_UPDATE.md if present
                string updateDocSrc = ResolveSourceFile("HUONG_DAN_UPDATE.md");
                if (!string.IsNullOrEmpty(updateDocSrc) && File.Exists(updateDocSrc))
                {
                    try
                    {
                        string updateDocDest = Path.Combine(targetDir, "HUONG_DAN_UPDATE.md");
                        if (!string.Equals(Path.GetFullPath(updateDocSrc), Path.GetFullPath(updateDocDest), StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(updateDocSrc, updateDocDest, true);
                        }
                    }
                    catch { }
                }

                // Save configurations, Registry, Firewall, UAC, Shortcuts
                if (!ApplySystemConfigurations(config, targetDir, out errorMessage))
                {
                    return false;
                }

                // Launch applications if requested
                if (config.LaunchAfterInstall)
                {
                    Report(92, "Đang khởi chạy các thành phần đã cài đặt...");
                    LaunchInstalledComponents(config, targetDir);
                }

                Report(100, "🎉 CÀI ĐẶT & KHỞI CHẠY 1-CLICK HOÀN TẤT THÀNH CÔNG!");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool SaveConfigurationOnly(InstallConfig config, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                string targetDir = EnsureLanControlDirectory(config.TargetDirectory);
                config.TargetDirectory = targetDir;
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                if (!ApplySystemConfigurations(config, targetDir, out errorMessage))
                {
                    return false;
                }

                Report(100, "💾 Đã lưu cấu hình hệ thống thành công!");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private bool ApplySystemConfigurations(InstallConfig config, string targetDir, out string errorMessage)
        {
            errorMessage = "";
            bool installAgent = (config.Edition == InstallEdition.AgentClient || config.Edition == InstallEdition.FullSuite);
            bool installServer = (config.Edition == InstallEdition.ServerMaster || config.Edition == InstallEdition.FullSuite);
            string installedIcon = Path.Combine(targetDir, "icon.ico");
            if (!File.Exists(installedIcon))
            {
                string srcIcon = ResolveSourceFile("icon.ico");
                if (!string.IsNullOrEmpty(srcIcon)) installedIcon = srcIcon;
            }

            if (installAgent)
            {
                Report(62, "Đang lưu cấu hình Máy Trạm (agent_config.json)...");
                string agentCfgPath = Path.Combine(targetDir, "agent_config.json");
                string existingAgentId = null;
                if (File.Exists(agentCfgPath))
                {
                    try
                    {
                        existingAgentId = ExtractJsonString(File.ReadAllText(agentCfgPath, Encoding.UTF8), "AgentId");
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(existingAgentId))
                {
                    existingAgentId = Guid.NewGuid().ToString("N");
                }

                string agentJson = string.Format(
                    "{{\r\n  \"AgentId\": \"{0}\",\r\n  \"GatewayHost\": \"{1}\",\r\n  \"GatewayPort\": {2},\r\n  \"VlanTag\": \"{3}\",\r\n  \"ThumbnailIntervalMs\": 1500,\r\n  \"LiveStreamIntervalMs\": 66\r\n}}",
                    existingAgentId,
                    (config.GatewayHost ?? "127.0.0.1").Replace("\\", "\\\\").Replace("\"", "\\\""),
                    config.GatewayPort,
                    (config.VlanTag ?? "Default").Replace("\\", "\\\\").Replace("\"", "\\\"")
                );
                File.WriteAllText(agentCfgPath, agentJson, Encoding.UTF8);

                string agentExe = Path.Combine(targetDir, "LanControl.Agent.exe");
                Report(68, "Đang thiết lập chế độ tự khởi động Agent cùng Windows...");
                SetAgentAutoStart(agentExe, config.AgentAutoStart);

                if (config.EnableUacRemoteSupport)
                {
                    Report(72, "Đang cấu hình quyền điều khiển xuyên qua UAC (Tắt UIPI & Auto-Elevate)...");
                    ConfigureUacPolicies(true);
                }
            }

            if (installServer)
            {
                Report(76, "Đang lưu cấu hình Máy Chủ (gateway_config.json)...");
                string gwCfgPath = Path.Combine(targetDir, "gateway_config.json");
                string escapedDir = targetDir.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string gwJson = string.Format(
                    "{{\r\n  \"Port\": {0},\r\n  \"ListenPort\": {0},\r\n  \"AutoStart\": {1},\r\n  \"ConfigureFirewall\": {2},\r\n  \"CreateDesktopShortcuts\": {3},\r\n  \"InstallPath\": \"{4}\"\r\n}}",
                    config.ServerPort,
                    config.ServerAutoStart ? "true" : "false",
                    config.OpenFirewall ? "true" : "false",
                    config.CreateDesktopShortcuts ? "true" : "false",
                    escapedDir
                );
                File.WriteAllText(gwCfgPath, gwJson, Encoding.UTF8);

                if (config.OpenFirewall)
                {
                    Report(80, "Đang mở cổng tường lửa Windows Defender Firewall TCP " + config.ServerPort + "...");
                    OpenFirewallRule(config.ServerPort);
                }

                string gwExe = Path.Combine(targetDir, "LanControl.Gateway.exe");
                Report(84, "Đang thiết lập tự khởi động Gateway cùng Windows...");
                SetGatewayAutoStart(gwExe, config.ServerPort, config.ServerAutoStart);
            }

            if (config.CreateDesktopShortcuts)
            {
                Report(88, "Đang tạo biểu tượng lối tắt ngoài Desktop...");
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                if (installAgent)
                {
                    string target = Path.Combine(targetDir, "LanControl.Agent.exe");
                    string lnk = Path.Combine(desktop, "LAN Control Agent.lnk");
                    CreateShortcut(lnk, target, "", installedIcon, "LAN Control Workstation Client Agent");
                }

                if (installServer)
                {
                    string ctrlTarget = Path.Combine(targetDir, "LanControl.Control.exe");
                    string ctrlLnk = Path.Combine(desktop, "LAN Control Master Control.lnk");
                    CreateShortcut(ctrlLnk, ctrlTarget, "", installedIcon, "LAN Control Master Management Console");

                    string gwTarget = Path.Combine(targetDir, "LanControl.Gateway.exe");
                    string gwLnk = Path.Combine(desktop, "LAN Control Central Gateway.lnk");
                    CreateShortcut(gwLnk, gwTarget, config.ServerPort.ToString(), installedIcon, "LAN Control Central Multi-VLAN Relay Gateway");
                }
            }

            return true;
        }

        public static void LaunchInstalledComponents(InstallConfig config, string targetDir)
        {
            bool launchAgent = (config.Edition == InstallEdition.AgentClient || config.Edition == InstallEdition.FullSuite);
            bool launchServer = (config.Edition == InstallEdition.ServerMaster || config.Edition == InstallEdition.FullSuite);

            if (launchServer)
            {
                string gwExe = Path.Combine(targetDir, "LanControl.Gateway.exe");
                if (File.Exists(gwExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = gwExe,
                        Arguments = config.ServerPort.ToString(),
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                    System.Threading.Thread.Sleep(350);
                }
            }

            if (launchAgent)
            {
                string agentExe = Path.Combine(targetDir, "LanControl.Agent.exe");
                if (File.Exists(agentExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = agentExe,
                        Arguments = string.Format("\"{0}\" {1} \"{2}\"", config.GatewayHost, config.GatewayPort, config.VlanTag),
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                    System.Threading.Thread.Sleep(250);
                }
            }

            if (launchServer)
            {
                string ctrlExe = Path.Combine(targetDir, "LanControl.Control.exe");
                if (File.Exists(ctrlExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ctrlExe,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }
            }
        }

        public bool ExecuteUninstall(InstallConfig config, out string summaryMessage, out string errorMessage)
        {
            summaryMessage = "";
            errorMessage = "";
            try
            {
                string targetDir = EnsureLanControlDirectory(config.TargetDirectory);
                bool uninstallAgent = (config.Edition == InstallEdition.AgentClient || config.Edition == InstallEdition.FullSuite);
                bool uninstallServer = (config.Edition == InstallEdition.ServerMaster || config.Edition == InstallEdition.FullSuite);

                List<string> actions = new List<string>();

                if (uninstallAgent)
                {
                    Report(20, "Đang dừng tiến trình LAN Control Agent...");
                    TryKillRunningProcess("LanControl.Agent");

                    Report(35, "Đang xóa tự khởi động Agent khỏi Registry, Startup & Task Scheduler...");
                    SetAgentAutoStart("", false);

                    string agentCfg = Path.Combine(targetDir, "agent_config.json");
                    if (File.Exists(agentCfg))
                    {
                        try { File.Delete(agentCfg); } catch { }
                    }

                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string agentLnk = Path.Combine(desktop, "LAN Control Agent.lnk");
                    if (File.Exists(agentLnk))
                    {
                        try { File.Delete(agentLnk); } catch { }
                    }

                    actions.Add("• Đã dừng Agent, xóa cấu hình agent_config.json và hủy tự khởi động cùng Windows.");
                }

                if (uninstallServer)
                {
                    Report(55, "Đang dừng tiến trình LAN Control Gateway...");
                    TryKillRunningProcess("LanControl.Gateway");

                    Report(70, "Đang xóa tự khởi động Gateway và quy tắc tường lửa Firewall...");
                    SetGatewayAutoStart("", 0, false);
                    RemoveFirewallRule(config.ServerPort);

                    string gwCfg = Path.Combine(targetDir, "gateway_config.json");
                    if (File.Exists(gwCfg))
                    {
                        try { File.Delete(gwCfg); } catch { }
                    }

                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string[] serverShortcuts = new string[]
                    {
                        Path.Combine(desktop, "LAN Control Master Control.lnk"),
                        Path.Combine(desktop, "LAN Control Master Console.lnk"),
                        Path.Combine(desktop, "LAN Control Central Gateway.lnk"),
                        Path.Combine(desktop, "LAN Control Gateway Server.lnk")
                    };
                    foreach (string lnk in serverShortcuts)
                    {
                        if (File.Exists(lnk))
                        {
                            try { File.Delete(lnk); } catch { }
                        }
                    }

                    actions.Add("• Đã dừng Gateway, xóa cấu hình gateway_config.json, xóa luật Firewall và lối tắt Desktop.");
                }

                Report(100, "🗑️ Đã gỡ cài đặt thành công!");
                summaryMessage = string.Join("\r\n", actions.ToArray()) + "\r\n• Các tệp thực thi (.exe, .dll) vẫn được giữ nguyên để có thể cài lại bất cứ lúc nào.";
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static string EnsureLanControlDirectory(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) rawPath = @"C:\Program Files\lancontrol";
            rawPath = rawPath.Trim().TrimEnd('\\', '/');

            string folderName = Path.GetFileName(rawPath);
            if (string.Equals(folderName, "lancontrol", StringComparison.OrdinalIgnoreCase))
            {
                return rawPath;
            }

            // If the directory already contains LAN Control executables or configs directly, use it as-is
            try
            {
                if (File.Exists(Path.Combine(rawPath, "LanControl.Agent.exe")) ||
                    File.Exists(Path.Combine(rawPath, "LanControl.Gateway.exe")) ||
                    File.Exists(Path.Combine(rawPath, "agent_config.json")) ||
                    File.Exists(Path.Combine(rawPath, "gateway_config.json")))
                {
                    return rawPath;
                }
            }
            catch { }

            return Path.Combine(rawPath, "lancontrol");
        }

        private void Report(int pct, string msg)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(pct, msg);
            }
        }

        public static void TryKillRunningProcess(string fileOrProcName)
        {
            string procName = Path.GetFileNameWithoutExtension(fileOrProcName);
            try
            {
                foreach (Process p in Process.GetProcessesByName(procName))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }
            }
            catch { }
        }

        private static void SetAgentAutoStart(string exePath, bool enable)
        {
            // 1. HKCU Run
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable) key.SetValue("LanControlAgent", "\"" + exePath + "\"");
                        else key.DeleteValue("LanControlAgent", false);
                    }
                }
            }
            catch { }

            // 2. HKLM Run
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable) key.SetValue("LanControlAgent", "\"" + exePath + "\"");
                        else key.DeleteValue("LanControlAgent", false);
                    }
                }
            }
            catch { }

            // 3. User Startup Folder
            try
            {
                string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!string.IsNullOrEmpty(startupDir) && Directory.Exists(startupDir))
                {
                    string batPath = Path.Combine(startupDir, "LanControlAgent_AutoStart.bat");
                    if (enable) File.WriteAllText(batPath, string.Format("@echo off\r\nstart \"\" \"{0}\"\r\n", exePath));
                    else if (File.Exists(batPath)) File.Delete(batPath);
                }
            }
            catch { }

            // 4. Common Startup Folder
            try
            {
                string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                if (!string.IsNullOrEmpty(commonStartup) && Directory.Exists(commonStartup))
                {
                    string batPath = Path.Combine(commonStartup, "LanControlAgent_AutoStart.bat");
                    if (enable) File.WriteAllText(batPath, string.Format("@echo off\r\nstart \"\" \"{0}\"\r\n", exePath));
                    else if (File.Exists(batPath)) File.Delete(batPath);
                }
            }
            catch { }

            // 5. Scheduled Task (HIGHEST privileges for UAC support)
            try
            {
                string args = enable
                    ? string.Format("/create /tn \"LanControlAgent\" /tr \"\\\"{0}\\\"\" /sc onlogon /rl HIGHEST /f", exePath)
                    : "/delete /tn \"LanControlAgent\" /f";
                ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(3000);
                }
            }
            catch { }
        }

        private static void SetGatewayAutoStart(string exePath, int port, bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable) key.SetValue("LanControlGateway", string.Format("\"{0}\" {1}", exePath, port));
                        else key.DeleteValue("LanControlGateway", false);
                    }
                }
            }
            catch { }

            try
            {
                string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!string.IsNullOrEmpty(startupDir) && Directory.Exists(startupDir))
                {
                    string batPath = Path.Combine(startupDir, "LanControlGateway_AutoStart.bat");
                    if (enable) File.WriteAllText(batPath, string.Format("@echo off\r\nstart \"\" \"{0}\" {1}\r\n", exePath, port));
                    else if (File.Exists(batPath)) File.Delete(batPath);
                }
            }
            catch { }
        }

        private static void ConfigureUacPolicies(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        int val = enable ? 0 : 1;
                        int adminVal = enable ? 0 : 5;
                        int userVal = enable ? 1 : 3;
                        key.SetValue("PromptOnSecureDesktop", val, RegistryValueKind.DWord);
                        key.SetValue("EnableUIPI", val, RegistryValueKind.DWord);
                        key.SetValue("ConsentPromptBehaviorAdmin", adminVal, RegistryValueKind.DWord);
                        key.SetValue("ConsentPromptBehaviorUser", userVal, RegistryValueKind.DWord);
                        key.SetValue("EnableSecureUIAPaths", val, RegistryValueKind.DWord);
                    }
                }
            }
            catch
            {
                try
                {
                    string cmd = string.Format(
                        "/c reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v PromptOnSecureDesktop /t REG_DWORD /d {0} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableUIPI /t REG_DWORD /d {1} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d {2} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorUser /t REG_DWORD /d {3} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableSecureUIAPaths /t REG_DWORD /d {4} /f",
                        enable ? 0 : 1,
                        enable ? 0 : 1,
                        enable ? 0 : 5,
                        enable ? 1 : 3,
                        enable ? 0 : 1
                    );
                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        if (p != null) p.WaitForExit(3000);
                    }
                }
                catch { }
            }
        }

        private static void OpenFirewallRule(int port)
        {
            try
            {
                RemoveFirewallRule(port);
                string addCmd = string.Format("advfirewall firewall add rule name=\"LAN Control Gateway\" dir=in action=allow protocol=TCP localport={0}", port);
                ProcessStartInfo psi = new ProcessStartInfo("netsh", addCmd)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(3000);
                }
            }
            catch { }
        }

        private static void RemoveFirewallRule(int port)
        {
            try
            {
                string[] ruleNames = new string[]
                {
                    "LAN Control Gateway",
                    string.Format("LAN Control Gateway (Port {0})", port)
                };
                foreach (string rule in ruleNames)
                {
                    ProcessStartInfo psi = new ProcessStartInfo("netsh", string.Format("advfirewall firewall delete rule name=\"{0}\"", rule))
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        if (p != null) p.WaitForExit(2000);
                    }
                }
            }
            catch { }
        }

        public static void CreateShortcut(string shortcutPath, string targetExe, string arguments, string iconPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetExe;
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        shortcut.Arguments = arguments;
                    }
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
                    if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    {
                        shortcut.IconLocation = iconPath + ",0";
                    }
                    shortcut.Description = description;
                    shortcut.Save();
                    return;
                }
            }
            catch { }

            try
            {
                string iconArg = (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    ? string.Format("$s.IconLocation = '{0},0';", iconPath.Replace("'", "''"))
                    : "";
                string argsLine = !string.IsNullOrEmpty(arguments)
                    ? string.Format("$s.Arguments = '{0}';", arguments.Replace("'", "''"))
                    : "";

                string script = string.Format(
                    "$w = New-Object -ComObject WScript.Shell; $s = $w.CreateShortcut('{0}'); $s.TargetPath = '{1}'; {2} $s.WorkingDirectory = '{3}'; {4} $s.Description = '{5}'; $s.Save();",
                    shortcutPath.Replace("'", "''"),
                    targetExe.Replace("'", "''"),
                    argsLine,
                    Path.GetDirectoryName(targetExe).Replace("'", "''"),
                    iconArg,
                    description.Replace("'", "''")
                );

                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-NoProfile -NonInteractive -Command \"" + script + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(3000);
                }
            }
            catch { }
        }
    }
}
