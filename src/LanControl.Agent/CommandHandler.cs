using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    public static class CommandHandler
    {
        public static CommandResponse Execute(CommandRequest req)
        {
            CommandResponse resp = new CommandResponse
            {
                RequestId = req.RequestId,
                AgentId = req.AgentId,
                Success = true
            };

            try
            {
                switch (req.Command)
                {
                    case CommandType.ExecuteCmd:
                        string tempBat = Path.Combine(Path.GetTempPath(), "ns_cmd_" + Guid.NewGuid().ToString("N") + ".bat");
                        try
                        {
                            File.WriteAllText(tempBat, "@chcp 65001 >nul\r\n" + req.Payload, new System.Text.UTF8Encoding(false));
                            resp.Output = RunProcessCapture("cmd.exe", "/c \"" + tempBat + "\"");
                        }
                        finally
                        {
                            try { if (File.Exists(tempBat)) File.Delete(tempBat); } catch { }
                        }
                        break;

                    case CommandType.ExecutePs:
                        string tempPs = Path.Combine(Path.GetTempPath(), "ns_ps_" + Guid.NewGuid().ToString("N") + ".ps1");
                        try
                        {
                            string header = "$ErrorActionPreference = 'SilentlyContinue'\r\n" +
                                            "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8\r\n" +
                                            "$OutputEncoding = [System.Text.Encoding]::UTF8\r\n" +
                                            "try { [Console]::InputEncoding = [System.Text.Encoding]::UTF8 } catch {}\r\n" +
                                            "try { chcp 65001 >$null } catch {}\r\n";
                            File.WriteAllText(tempPs, header + req.Payload, new System.Text.UTF8Encoding(true));
                            resp.Output = RunProcessCapture("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + tempPs + "\"");
                        }
                        finally
                        {
                            try { if (File.Exists(tempPs)) File.Delete(tempPs); } catch { }
                        }
                        break;

                    case CommandType.GetProcessList:
                        var processes = SystemInfoCollector.GetRunningProcesses();
                        resp.Output = PacketSerializer.Serialize(processes);
                        break;

                    case CommandType.GetHardwareInventory:
                        var audit = HardwareInfoCollector.CollectFullAuditReport(req.AgentId, Environment.MachineName, SystemInfoCollector.GetLocalIpAddress(), "");
                        resp.Output = PacketSerializer.Serialize(audit);
                        break;

                    case CommandType.KillProcess:
                        int pid;
                        if (int.TryParse(req.Payload, out pid))
                        {
                            bool killed = SystemInfoCollector.KillProcess(pid);
                            resp.Success = killed;
                            resp.Output = killed ? "Process killed successfully." : "Failed to kill process.";
                        }
                        else
                        {
                            resp.Success = false;
                            resp.ErrorMessage = "Invalid PID: " + req.Payload;
                        }
                        break;

                    case CommandType.SendMessage:
                        string msgText = req.Payload;
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            MessageBox.Show(msgText, "Thông Báo Từ Quản Trị Viên (LAN Control)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                        resp.Output = "Message displayed to user.";
                        break;

                    case CommandType.Reboot:
                        SystemInfoCollector.ExecutePowerAction("Reboot");
                        resp.Output = "Reboot command initiated.";
                        break;

                    case CommandType.Shutdown:
                        SystemInfoCollector.ExecutePowerAction("Shutdown");
                        resp.Output = "Shutdown command initiated.";
                        break;

                    case CommandType.HideWallpaper:
                        WallpaperManager.HideWallpaper();
                        resp.Output = "Wallpaper hidden.";
                        break;

                    case CommandType.RestoreWallpaper:
                        WallpaperManager.RestoreWallpaper();
                        resp.Output = "Wallpaper restored.";
                        break;

                    case CommandType.EnableUacBypass:
                        try
                        {
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop", 0, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableUIPI", 0, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", 0, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorUser", 1, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableSecureUIAPaths", 0, Microsoft.Win32.RegistryValueKind.DWord);
                            resp.Output = "Đã bật cấu hình hiển thị UAC trực tiếp trên màn hình và mở chặn chuột phím UIPI thành công (PromptOnSecureDesktop=0, EnableUIPI=0, ConsentPromptBehaviorAdmin=0, ConsentPromptBehaviorUser=1).";
                        }
                        catch
                        {
                            resp.Output = RunProcessCapture("cmd.exe", "/c " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v PromptOnSecureDesktop /t REG_DWORD /d 0 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableUIPI /t REG_DWORD /d 0 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 0 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorUser /t REG_DWORD /d 1 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableSecureUIAPaths /t REG_DWORD /d 0 /f");
                        }

                        // Đóng tiến trình consent.exe bị kẹt trên Secure Desktop cũ (nếu có)
                        try
                        {
                            foreach (var p in Process.GetProcessesByName("consent"))
                            {
                                try { p.Kill(); } catch { }
                            }
                        }
                        catch { }
                        break;

                    case CommandType.DisableUacBypass:
                        try
                        {
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop", 1, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableUIPI", 1, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", 5, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorUser", 3, Microsoft.Win32.RegistryValueKind.DWord);
                            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableSecureUIAPaths", 1, Microsoft.Win32.RegistryValueKind.DWord);
                            resp.Output = "Đã khôi phục cấu hình bảo mật UAC mặc định của Windows.";
                        }
                        catch
                        {
                            resp.Output = RunProcessCapture("cmd.exe", "/c " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v PromptOnSecureDesktop /t REG_DWORD /d 1 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableUIPI /t REG_DWORD /d 1 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 5 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorUser /t REG_DWORD /d 3 /f & " +
                                "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableSecureUIAPaths /t REG_DWORD /d 1 /f");
                        }
                        break;

                    case CommandType.UacYes:
                        InputSimulator.SendUacResponse(true);
                        resp.Output = "Đã gửi lệnh xác nhận Đồng Ý UAC (Yes / Alt+Y / Enter).";
                        break;

                    case CommandType.UacNo:
                        InputSimulator.SendUacResponse(false);
                        resp.Output = "Đã gửi lệnh Từ Chối / Đóng hộp thoại UAC (No / Esc).";
                        break;

                    case CommandType.SendAdminPassword:
                        InputSimulator.SendAdminPassword(req.Payload);
                        resp.Output = "Đã gửi mật khẩu Administrator vào ô nhập UAC trên máy trạm thành công.";
                        break;

                    default:
                        resp.Success = false;
                        resp.ErrorMessage = "Unsupported command: " + req.Command;
                        break;
                }
            }
            catch (Exception ex)
            {
                resp.Success = false;
                resp.ErrorMessage = ex.Message;
            }

            if (!string.IsNullOrEmpty(resp.Output))
            {
                resp.Output = FixMojibake(resp.Output);
            }
            return resp;
        }

        public static string FixMojibake(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.Contains("Ã") || input.Contains("áº") || input.Contains("á»") || 
                input.Contains("Cáº") || input.Contains("Ä") || input.Contains("á»‹") || 
                input.Contains("áº£") || input.Contains("á»ƒ") || input.Contains("áº¡") ||
                input.Contains("á»™") || input.Contains("á»‘") || input.Contains("Æ°") ||
                input.Contains("á»—") || input.Contains("á»“") || input.Contains("á» "))
            {
                try
                {
                    byte[] bytes = System.Text.Encoding.GetEncoding(1252).GetBytes(input);
                    string candidate = System.Text.Encoding.UTF8.GetString(bytes);
                    return candidate;
                }
                catch { }
            }
            return input;
        }

        private static string RunProcessCapture(string fileName, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true
            };

            using (Process p = Process.Start(psi))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);

                if (!string.IsNullOrEmpty(stderr))
                {
                    return stdout + "\n[STDERR]:\n" + stderr;
                }
                return stdout;
            }
        }
    }
}
