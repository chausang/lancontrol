using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    public static class CommandHandler
    {
        private static Form _activeMessageForm;
        private static readonly object _msgFormLock = new object();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
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
                        ShowClientNotificationDialog(msgText);
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

        private static void ForceActivateWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            try
            {
                IntPtr fgWnd = GetForegroundWindow();
                uint dummy;
                uint fgThread = fgWnd != IntPtr.Zero ? GetWindowThreadProcessId(fgWnd, out dummy) : 0;
                uint curThread = GetCurrentThreadId();

                if (fgThread != 0 && fgThread != curThread)
                {
                    AttachThreadInput(curThread, fgThread, true);
                    BringWindowToTop(hWnd);
                    SetForegroundWindow(hWnd);
                    AttachThreadInput(curThread, fgThread, false);
                }
                else
                {
                    BringWindowToTop(hWnd);
                    SetForegroundWindow(hWnd);
                }
            }
            catch { }
        }

        private static void ShowClientNotificationDialog(string msgText)
        {
            lock (_msgFormLock)
            {
                if (_activeMessageForm != null && !_activeMessageForm.IsDisposed)
                {
                    try
                    {
                        Form oldForm = _activeMessageForm;
                        _activeMessageForm = null;
                        if (oldForm.IsHandleCreated)
                        {
                            oldForm.BeginInvoke(new Action(delegate
                            {
                                try { oldForm.Close(); } catch { }
                            }));
                        }
                    }
                    catch { }
                }
            }

            Thread msgThread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    ScreenCapture.AttachToActiveInputDesktop();

                    Form msgForm = new Form
                    {
                        Text = "Thông Báo Từ Quản Trị Viên (LAN Control)",
                        Size = new Size(460, 220),
                        StartPosition = FormStartPosition.CenterScreen,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        TopMost = true,
                        ShowInTaskbar = true,
                        KeyPreview = true,
                        BackColor = Color.FromArgb(24, 34, 48),
                        ForeColor = Color.White
                    };

                    Panel headerPanel = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 38,
                        BackColor = Color.FromArgb(16, 24, 36)
                    };

                    Label lblHeader = new Label
                    {
                        Text = "📢 THÔNG BÁO TỪ QUẢN TRỊ VIÊN (LAN CONTROL)",
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(56, 189, 248),
                        Location = new Point(14, 9),
                        AutoSize = true
                    };
                    headerPanel.Controls.Add(lblHeader);

                    TextBox txtBody = new TextBox
                    {
                        Text = msgText ?? "",
                        Multiline = true,
                        ReadOnly = true,
                        BorderStyle = BorderStyle.None,
                        BackColor = Color.FromArgb(24, 34, 48),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                        Location = new Point(20, 52),
                        Size = new Size(405, 72),
                        ScrollBars = ScrollBars.Vertical,
                        TabStop = false
                    };

                    Button btnOk = new Button
                    {
                        Text = "✓ Đã Hiểu / Đóng (OK)",
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                        Size = new Size(170, 36),
                        Location = new Point(140, 134),
                        BackColor = Color.FromArgb(37, 99, 235),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand
                    };
                    btnOk.FlatAppearance.BorderSize = 0;

                    Action closeDialog = delegate
                    {
                        try
                        {
                            if (!msgForm.IsDisposed) msgForm.Close();
                        }
                        catch { }
                    };

                    btnOk.Click += (s, e) => closeDialog();
                    btnOk.MouseDown += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left) closeDialog();
                    };

                    msgForm.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space)
                        {
                            e.Handled = true;
                            closeDialog();
                        }
                    };

                    msgForm.AcceptButton = btnOk;
                    msgForm.CancelButton = btnOk;

                    msgForm.Controls.Add(btnOk);
                    msgForm.Controls.Add(txtBody);
                    msgForm.Controls.Add(headerPanel);

                    msgForm.Shown += (s, e) =>
                    {
                        try
                        {
                            ForceActivateWindow(msgForm.Handle);
                            msgForm.BringToFront();
                            msgForm.Activate();
                            btnOk.Focus();
                        }
                        catch { }
                    };

                    msgForm.FormClosed += (s, e) =>
                    {
                        lock (_msgFormLock)
                        {
                            if (_activeMessageForm == msgForm)
                            {
                                _activeMessageForm = null;
                            }
                        }
                    };

                    lock (_msgFormLock)
                    {
                        _activeMessageForm = msgForm;
                    }

                    Application.Run(msgForm);
                }
                catch { }
            }));

            msgThread.IsBackground = true;
            msgThread.SetApartmentState(ApartmentState.STA);
            msgThread.Start();
        }
    }
}
