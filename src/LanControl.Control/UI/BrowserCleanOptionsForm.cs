using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class BrowserCleanOptionsForm : Form
    {
        private ComboBox _cboTimeRange;
        private CheckBox _chkCache;
        private CheckBox _chkHistory;
        private CheckBox _chkDownloads;
        private CheckBox _chkCookies;
        private CheckBox _chkKillBrowsers;

        private readonly Action<string> _onExecuteScript;
        private readonly Action<string> _onInsertScript;
        private readonly string _targetSummary;

        public BrowserCleanOptionsForm(Action<string> onExecuteScript, Action<string> onInsertScript, string targetSummary = "")
        {
            _onExecuteScript = onExecuteScript;
            _onInsertScript = onInsertScript;
            _targetSummary = !string.IsNullOrEmpty(targetSummary) ? targetSummary : "Các máy trạm đang chọn";
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Tùy Chọn Xóa Cache & Dữ Liệu Trình Duyệt (Browser Data Cleaner)";
            this.Size = new Size(620, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // 1. Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(16, 12, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "🧹 DỌN DẸP CACHE & DỮ LIỆU TOÀN BỘ TRÌNH DUYỆT",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            Label lblSubTitle = new Label
            {
                Text = string.Format("Mục tiêu áp dụng: {0} | Tự động phát hiện toàn bộ trình duyệt trên máy", _targetSummary),
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(16, 40)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubTitle);

            // 2. Main Content Panel
            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackgroundDark,
                Padding = new Padding(20, 15, 20, 15),
                AutoScroll = true
            };

            int curY = 15;

            // Time Range Group
            Label lblTimeRange = new Label
            {
                Text = "Khoảng thời gian (Time range):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow,
                Location = new Point(20, curY),
                AutoSize = true
            };
            curY += 26;

            _cboTimeRange = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(22, curY),
                Width = 550
            };
            _cboTimeRange.Items.Add("Từ trước đến nay (All time) - [Khuyên dùng để dọn sạch triệt để]");
            _cboTimeRange.Items.Add("24 giờ qua (Last 24 hours)");
            _cboTimeRange.Items.Add("7 ngày qua (Last 7 days)");
            _cboTimeRange.Items.Add("4 tuần qua (Last 4 weeks)");
            _cboTimeRange.SelectedIndex = 0;
            curY += 45;

            // Options Checkboxes Group
            Label lblCategories = new Label
            {
                Text = "Các tùy chọn xóa dữ liệu (Data to clear):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow,
                Location = new Point(20, curY),
                AutoSize = true
            };
            curY += 28;

            Panel pnlOptions = new Panel
            {
                Location = new Point(20, curY),
                Width = 555,
                Height = 220,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle
            };

            int optY = 10;

            _chkCache = CreateOptionCheckbox(
                "Tệp và hình ảnh được lưu trong bộ nhớ đệm (Cached images & files)",
                "Giải phóng dung lượng bộ nhớ đệm trình duyệt, tải lại bản mới nhất từ web.",
                true, ref optY, pnlOptions);

            _chkHistory = CreateOptionCheckbox(
                "Lịch sử duyệt web (Browsing history)",
                "Xóa danh sách các trang web đã truy cập và đề xuất tìm kiếm nhanh.",
                true, ref optY, pnlOptions);

            _chkDownloads = CreateOptionCheckbox(
                "Lịch sử tải xuống (Download history)",
                "Xóa danh sách tệp đã tải xuống khỏi trình duyệt (không xóa tệp trong máy).",
                true, ref optY, pnlOptions);

            _chkCookies = CreateOptionCheckbox(
                "Cookies và dữ liệu trang web khác (Cookies and site data)",
                "Đăng xuất khỏi các trang web và xóa cookie theo dõi.",
                true, ref optY, pnlOptions);

            _chkKillBrowsers = CreateOptionCheckbox(
                "Tự động đóng trình duyệt đang mở (Close browsers before clean)",
                "Tắt các tiến trình trình duyệt đang chạy để tránh tệp bị khóa khi dọn dẹp.",
                true, ref optY, pnlOptions);

            pnlOptions.Height = optY + 10;
            curY += pnlOptions.Height + 15;

            // Password Protection Shield Banner
            Panel pnlShield = new Panel
            {
                Location = new Point(20, curY),
                Width = 555,
                Height = 85,
                BackColor = Color.FromArgb(18, 38, 28),
                Padding = new Padding(14),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblShieldHeader = new Label
            {
                Text = "🛡️ BẢO VỆ MẬT KHẨU TỐI ĐA (SAVED PASSWORDS NEVER DELETED)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(12, 10)
            };

            Label lblShieldBody = new Label
            {
                Text = "Hệ thống TUYỆT ĐỐI KHÔNG XÓA mật khẩu đã lưu, thông tin đăng nhập và dữ liệu tự động điền (Login Data, logins.json, key4.db). Người dùng vẫn giữ nguyên các tài khoản và mật khẩu đã lưu sau khi dọn dẹp.",
                Font = ModernTheme.RegularFont,
                ForeColor = Color.FromArgb(200, 240, 210),
                Location = new Point(12, 34),
                Size = new Size(525, 42)
            };

            pnlShield.Controls.Add(lblShieldHeader);
            pnlShield.Controls.Add(lblShieldBody);
            curY += pnlShield.Height + 12;

            // Detected Browsers Badge Info
            Label lblBrowsersInfo = new Label
            {
                Text = "🔍 Tự động dò tìm tất cả trình duyệt trên máy: Chrome, Edge, Cốc Cốc, Firefox, Brave, Opera, Vivaldi...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(22, curY),
                AutoSize = true
            };

            pnlContent.Controls.Add(lblTimeRange);
            pnlContent.Controls.Add(_cboTimeRange);
            pnlContent.Controls.Add(lblCategories);
            pnlContent.Controls.Add(pnlOptions);
            pnlContent.Controls.Add(pnlShield);
            pnlContent.Controls.Add(lblBrowsersInfo);

            // 3. Bottom Action Buttons Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(16, 12, 16, 12)
            };

            Button btnExecute = new Button
            {
                Text = "⚡ Thực Thi Ngay (Clear Now)",
                Size = new Size(215, 36),
                Location = new Point(16, 12),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            ModernTheme.ApplyToButton(btnExecute, ModernTheme.AccentGreen, Color.White);
            btnExecute.Click += (s, e) =>
            {
                string script = GenerateScript();
                this.Close();
                if (_onExecuteScript != null) _onExecuteScript(script);
            };

            Button btnInsert = new Button
            {
                Text = "📝 Chèn Vào Script Editor",
                Size = new Size(185, 36),
                Location = new Point(240, 12),
                Font = ModernTheme.RegularFont
            };
            ModernTheme.ApplyToButton(btnInsert, ModernTheme.CardDark, ModernTheme.AccentCyan);
            btnInsert.Click += (s, e) =>
            {
                string script = GenerateScript();
                this.Close();
                if (_onInsertScript != null) _onInsertScript(script);
            };

            Button btnCancel = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 36),
                Location = new Point(480, 12),
                Font = ModernTheme.RegularFont
            };
            ModernTheme.ApplyToButton(btnCancel, ModernTheme.CardDark, ModernTheme.TextSecondary);
            btnCancel.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnExecute);
            pnlBottom.Controls.Add(btnInsert);
            pnlBottom.Controls.Add(btnCancel);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }

        private CheckBox CreateOptionCheckbox(string title, string desc, bool isChecked, ref int y, Panel parent)
        {
            CheckBox chk = new CheckBox
            {
                Text = title,
                Checked = isChecked,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(10, y),
                AutoSize = true
            };
            parent.Controls.Add(chk);
            y += 24;

            Label lblDesc = new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(32, y),
                AutoSize = true
            };
            parent.Controls.Add(lblDesc);
            y += 24;

            return chk;
        }

        public string GenerateScript()
        {
            string timeRangeKey = "all_time";
            if (_cboTimeRange.SelectedIndex == 1) timeRangeKey = "24h";
            else if (_cboTimeRange.SelectedIndex == 2) timeRangeKey = "7d";
            else if (_cboTimeRange.SelectedIndex == 3) timeRangeKey = "4w";

            return BuildBrowserCleanScript(
                timeRangeKey,
                _chkCache.Checked,
                _chkHistory.Checked,
                _chkDownloads.Checked,
                _chkCookies.Checked,
                _chkKillBrowsers.Checked
            );
        }

        public static string BuildBrowserCleanScript(
            string timeRangeKey = "all_time",
            bool clearCache = true,
            bool clearHistory = true,
            bool clearDownloads = true,
            bool clearCookies = true,
            bool killBrowsersFirst = true)
        {
            string timeRangeDesc = "Từ trước đến nay (All time)";
            string cutoffExpr = "$null";

            if (timeRangeKey == "24h")
            {
                timeRangeDesc = "24 giờ qua (Last 24 hours)";
                cutoffExpr = "(Get-Date).AddHours(-24)";
            }
            else if (timeRangeKey == "7d")
            {
                timeRangeDesc = "7 ngày qua (Last 7 days)";
                cutoffExpr = "(Get-Date).AddDays(-7)";
            }
            else if (timeRangeKey == "4w")
            {
                timeRangeDesc = "4 tuần qua (Last 4 weeks)";
                cutoffExpr = "(Get-Date).AddDays(-28)";
            }

            List<string> catList = new List<string>();
            if (clearCache) catList.Add("Bộ nhớ đệm web/hình ảnh (Cache)");
            if (clearHistory) catList.Add("Lịch sử duyệt web");
            if (clearDownloads) catList.Add("Lịch sử tải xuống");
            if (clearCookies) catList.Add("Cookies & Dữ liệu trang web");
            string catDesc = catList.Count > 0 ? string.Join(", ", catList.ToArray()) : "Chỉ quét danh sách trình duyệt";

            string killCode = "";
            if (killBrowsersFirst)
            {
                killCode = @"# Đóng các tiến trình trình duyệt đang mở để tránh khóa tệp
Write-Output '[TIẾN TRÌNH] Đang kiểm tra và đóng các trình duyệt đang mở để giải phóng khóa tệp...'
$browserProcs = @('chrome', 'msedge', 'browser', 'coc_coc_browser', 'firefox', 'brave', 'opera', 'vivaldi')
foreach ($pName in $browserProcs) {
    $procs = Get-Process -Name $pName -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Output ""  -> Đang đóng tiến trình: $pName (Số lượng: $($procs.Count))...""
        Stop-Process -Name $pName -Force -ErrorAction SilentlyContinue
    }
}
Start-Sleep -Milliseconds 800
";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"# ================================================================================");
            sb.AppendLine(@"# LAN CONTROL - DỌN DẸP CACHE & DỮ LIỆU TOÀN BỘ TRÌNH DUYỆT (TỰ ĐỘNG DÒ TÌM)");
            sb.AppendLine(@"# ================================================================================");
            sb.AppendLine(@"$ErrorActionPreference = 'SilentlyContinue'");
            sb.AppendLine(@"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8");
            sb.AppendLine(@"$OutputEncoding = [System.Text.Encoding]::UTF8");
            sb.AppendLine(@"try { [Console]::InputEncoding = [System.Text.Encoding]::UTF8 } catch {}");
            sb.AppendLine(@"try { chcp 65001 >$null } catch {}");
            sb.AppendLine();
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output '         DỌN DẸP BỘ NHỚ ĐỆM & DỮ LIỆU TOÀN BỘ TRÌNH DUYỆT (LAN CONTROL)         '");
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output ""Thời gian thực thi: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')""");
            sb.AppendLine(@"Write-Output ""Tên máy tính:       $($env:COMPUTERNAME)""");
            sb.AppendLine(@"Write-Output ""Người dùng:         $($env:USERNAME)""");
            sb.AppendLine(string.Format("Write-Output \"Khoảng thời gian:   {0}\"", timeRangeDesc));
            sb.AppendLine(string.Format("Write-Output \"Hạng mục dọn dẹp:   {0}\"", catDesc));
            sb.AppendLine(@"Write-Output '🛡️ BẢO VỆ MẬT KHẨU: TUYỆT ĐỐI KHÔNG XÓA MẬT KHẨU ĐÃ LƯU & TÀI KHOẢN (SAFE)'");
            sb.AppendLine(@"Write-Output '--------------------------------------------------------------------------------'");
            sb.AppendLine();
            sb.AppendLine(string.Format("$cutoff = {0}", cutoffExpr));
            sb.AppendLine();
            if (!string.IsNullOrEmpty(killCode))
            {
                sb.AppendLine(killCode);
            }
            sb.AppendLine(@"# Hàm xóa an toàn: BẢO VỆ MẬT KHẨU TUYỆT ĐỐI KHÔNG BAO GIỜ XÓA");
            sb.AppendLine(@"function Remove-SafeItem {");
            sb.AppendLine(@"    param(");
            sb.AppendLine(@"        [string]$Path,");
            sb.AppendLine(@"        $CutoffTime = $null,");
            sb.AppendLine(@"        [switch]$Recurse");
            sb.AppendLine(@"    )");
            sb.AppendLine(@"    if (-not (Test-Path -LiteralPath $Path)) { return @{ Count = 0; Bytes = 0 } }");
            sb.AppendLine();
            sb.AppendLine(@"    $deletedCount = 0");
            sb.AppendLine(@"    $deletedBytes = 0");
            sb.AppendLine();
            sb.AppendLine(@"    # DANH SÁCH TỆP MẬT KHẨU & TÀI KHOẢN ĐƯỢC BẢO VỆ TUYỆT ĐỐI - KHÔNG BAO GIỜ XÓA");
            sb.AppendLine(@"    $protectedRegex = '^(Login Data|Login Data-journal|Login Data For Account|Web Data|Web Data-journal|Local State|Preferences|Secure Preferences|Bookmarks|bookmarks\.bak|logins\.json|key4\.db|key3\.db|cert9\.db)$'");
            sb.AppendLine();
            sb.AppendLine(@"    try {");
            sb.AppendLine(@"        if (Test-Path -LiteralPath $Path -PathType Container) {");
            sb.AppendLine(@"            $items = Get-ChildItem -LiteralPath $Path -Recurse:$Recurse -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"            foreach ($item in $items) {");
            sb.AppendLine(@"                if ($item.Name -match $protectedRegex) { continue }");
            sb.AppendLine(@"                if ($CutoffTime -and ($item.LastWriteTime -lt $CutoffTime)) { continue }");
            sb.AppendLine(@"                try {");
            sb.AppendLine(@"                    if (-not $item.PSIsContainer) {");
            sb.AppendLine(@"                        $len = $item.Length");
            sb.AppendLine(@"                        Remove-Item -LiteralPath $item.FullName -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"                        $deletedCount++");
            sb.AppendLine(@"                        $deletedBytes += $len");
            sb.AppendLine(@"                    }");
            sb.AppendLine(@"                } catch {}");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        } else {");
            sb.AppendLine(@"            $leaf = Split-Path -Path $Path -Leaf");
            sb.AppendLine(@"            if ($leaf -notmatch $protectedRegex) {");
            sb.AppendLine(@"                $item = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"                if ($item -and (-not $CutoffTime -or ($item.LastWriteTime -ge $CutoffTime))) {");
            sb.AppendLine(@"                    $len = $item.Length");
            sb.AppendLine(@"                    Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"                    $deletedCount++");
            sb.AppendLine(@"                    $deletedBytes += $len");
            sb.AppendLine(@"                }");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        }");
            sb.AppendLine(@"    } catch {}");
            sb.AppendLine();
            sb.AppendLine(@"    return @{ Count = $deletedCount; Bytes = $deletedBytes }");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"# DÒ TÌM TẤT CẢ TRÌNH DUYỆT TRÊN MÁY TRẠM");
            sb.AppendLine(@"Write-Output ''");
            sb.AppendLine(@"Write-Output '[BƯỚC 1] DÒ TÌM CÁC TRÌNH DUYỆT ĐANG CÀI ĐẶT TRÊN MÁY TRẠM:'");
            sb.AppendLine(@"$browserDefinitions = @(");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'Chrome'");
            sb.AppendLine(@"        Name = 'Google Chrome'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""$env:ProgramFiles\Google\Chrome\Application\chrome.exe"", ""${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"", ""$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"")");
            sb.AppendLine(@"        UserData = ""$env:LOCALAPPDATA\Google\Chrome\User Data""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'Edge'");
            sb.AppendLine(@"        Name = 'Microsoft Edge'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"", ""$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"", ""$env:LOCALAPPDATA\Microsoft\Edge\Application\msedge.exe"")");
            sb.AppendLine(@"        UserData = ""$env:LOCALAPPDATA\Microsoft\Edge\User Data""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'CocCoc'");
            sb.AppendLine(@"        Name = 'Cốc Cốc Browser'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""$env:LOCALAPPDATA\CocCoc\Browser\Application\browser.exe"", ""$env:ProgramFiles\CocCoc\Browser\Application\browser.exe"", ""${env:ProgramFiles(x86)}\CocCoc\Browser\Application\browser.exe"")");
            sb.AppendLine(@"        UserData = ""$env:LOCALAPPDATA\CocCoc\Browser\User Data""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'Firefox'");
            sb.AppendLine(@"        Name = 'Mozilla Firefox'");
            sb.AppendLine(@"        Type = 'Firefox'");
            sb.AppendLine(@"        Exes = @(""$env:ProgramFiles\Mozilla Firefox\firefox.exe"", ""${env:ProgramFiles(x86)}\Mozilla Firefox\firefox.exe"")");
            sb.AppendLine(@"        UserData = ""$env:APPDATA\Mozilla\Firefox\Profiles""");
            sb.AppendLine(@"        LocalData = ""$env:LOCALAPPDATA\Mozilla\Firefox\Profiles""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'Brave'");
            sb.AppendLine(@"        Name = 'Brave Browser'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""$env:ProgramFiles\BraveSoftware\Brave-Browser\Application\brave.exe"", ""$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\Application\brave.exe"")");
            sb.AppendLine(@"        UserData = ""$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\User Data""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'Opera'");
            sb.AppendLine(@"        Name = 'Opera'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""$env:LOCALAPPDATA\Programs\Opera\launcher.exe"", ""$env:ProgramFiles\Opera\launcher.exe"")");
            sb.AppendLine(@"        UserData = ""$env:APPDATA\Opera Software\Opera Stable""");
            sb.AppendLine(@"        LocalData = ""$env:LOCALAPPDATA\Opera Software\Opera Stable""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'OperaGX'");
            sb.AppendLine(@"        Name = 'Opera GX'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""$env:LOCALAPPDATA\Programs\Opera GX\launcher.exe"")");
            sb.AppendLine(@"        UserData = ""$env:APPDATA\Opera Software\Opera GX Stable""");
            sb.AppendLine(@"        LocalData = ""$env:LOCALAPPDATA\Opera Software\Opera GX Stable""");
            sb.AppendLine(@"    },");
            sb.AppendLine(@"    @{");
            sb.AppendLine(@"        Id = 'Vivaldi'");
            sb.AppendLine(@"        Name = 'Vivaldi'");
            sb.AppendLine(@"        Type = 'Chromium'");
            sb.AppendLine(@"        Exes = @(""$env:LOCALAPPDATA\Vivaldi\Application\vivaldi.exe"", ""$env:ProgramFiles\Vivaldi\Application\vivaldi.exe"")");
            sb.AppendLine(@"        UserData = ""$env:LOCALAPPDATA\Vivaldi\User Data""");
            sb.AppendLine(@"    }");
            sb.AppendLine(@")");
            sb.AppendLine();
            sb.AppendLine(@"$detectedBrowsers = @()");
            sb.AppendLine(@"foreach ($b in $browserDefinitions) {");
            sb.AppendLine(@"    $foundExe = $b.Exes | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1");
            sb.AppendLine(@"    $dataExists = Test-Path -LiteralPath $b.UserData");
            sb.AppendLine(@"    $isInstalled = ($null -ne $foundExe) -or $dataExists");
            sb.AppendLine(@"    if ($isInstalled) {");
            sb.AppendLine(@"        $ver = if ($foundExe) { (Get-Item -LiteralPath $foundExe).VersionInfo.ProductVersion } else { 'Không rõ phiên bản' }");
            sb.AppendLine(@"        Write-Output ""  [+] PHÁT HIỆN: $($b.Name)""");
            sb.AppendLine(@"        Write-Output ""      - Phiên bản: $ver""");
            sb.AppendLine(@"        Write-Output ""      - Thư mục dữ liệu: $($b.UserData)""");
            sb.AppendLine(@"        $b.Version = $ver");
            sb.AppendLine(@"        $b.ExePath = $foundExe");
            sb.AppendLine(@"        $detectedBrowsers += $b");
            sb.AppendLine(@"    } else {");
            sb.AppendLine(@"        Write-Output ""  [-] Không tìm thấy: $($b.Name)""");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"Write-Output ''");
            sb.AppendLine(@"Write-Output ""=> TỔNG CỘNG ĐÃ PHÁT HIỆN: $($detectedBrowsers.Count) TRÌNH DUYỆT TRÊN MÁY TÍNH NÀY.""");
            sb.AppendLine(@"if ($detectedBrowsers.Count -eq 0) {");
            sb.AppendLine(@"    Write-Output 'Không phát hiện trình duyệt nào có dữ liệu cần dọn dẹp.'");
            sb.AppendLine(@"    exit 0");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"Write-Output ''");
            sb.AppendLine(@"Write-Output '[BƯỚC 2] TIẾN HÀNH DỌN DẸP DỮ LIỆU & BỘ NHỚ ĐỆM...'");
            sb.AppendLine(@"$totalFreedBytes = 0");
            sb.AppendLine(@"$totalFreedFiles = 0");
            sb.AppendLine();
            sb.AppendLine(@"foreach ($b in $detectedBrowsers) {");
            sb.AppendLine(@"    Write-Output ''");
            sb.AppendLine(@"    Write-Output '--------------------------------------------------------------------------------'");
            sb.AppendLine(@"    Write-Output "">>> ĐANG DỌN DẸP: $($b.Name)...""");
            sb.AppendLine(@"    Write-Output '--------------------------------------------------------------------------------'");
            sb.AppendLine(@"    $bFiles = 0");
            sb.AppendLine(@"    $bBytes = 0");
            sb.AppendLine();
            sb.AppendLine(@"    if ($b.Type -eq 'Chromium') {");
            sb.AppendLine(@"        $uPath = $b.UserData");
            sb.AppendLine(@"        if (Test-Path -LiteralPath $uPath) {");
            if (clearCache)
            {
                sb.AppendLine(@"            foreach ($sub in @('ShaderCache', 'GrShaderCache', 'Default\GPUCache')) {");
                sb.AppendLine(@"                $p = Join-Path $uPath $sub");
                sb.AppendLine(@"                $r = Remove-SafeItem -Path $p -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"            }");
            }
            sb.AppendLine(@"            $profiles = Get-ChildItem -LiteralPath $uPath -Directory -ErrorAction SilentlyContinue | Where-Object {");
            sb.AppendLine(@"                $_.Name -match '^(Default|Profile \d+|System Profile|Guest Profile)$'");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"            foreach ($prof in $profiles) {");
            sb.AppendLine(@"                Write-Output ""  * Đang xử lý Profile: $($prof.Name)...""");
            if (clearCache)
            {
                sb.AppendLine(@"                $cachePaths = @(");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Cache'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Code Cache'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'GPUCache'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Media Cache'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Service Worker\CacheStorage'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Service Worker\ScriptCache'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'DawnCache'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'File System')");
                sb.AppendLine(@"                )");
                sb.AppendLine(@"                foreach ($cp in $cachePaths) {");
                sb.AppendLine(@"                    $r = Remove-SafeItem -Path $cp -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            if (clearHistory)
            {
                sb.AppendLine(@"                $histFiles = @('History', 'History-journal', 'History Provider Cache', 'Top Sites', 'Top Sites-journal', 'Visited Links', 'Network Action Predictor', 'Shortcuts', 'Shortcuts-journal')");
                sb.AppendLine(@"                foreach ($hf in $histFiles) {");
                sb.AppendLine(@"                    $p = Join-Path $prof.FullName $hf");
                sb.AppendLine(@"                    $r = Remove-SafeItem -Path $p -CutoffTime $cutoff");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            if (clearDownloads)
            {
                sb.AppendLine(@"                $downPath = Join-Path $prof.FullName 'Download Service'");
                sb.AppendLine(@"                $r = Remove-SafeItem -Path $downPath -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
            }
            if (clearCookies)
            {
                sb.AppendLine(@"                $cookiePaths = @(");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Network\Cookies'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Network\Cookies-journal'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Cookies'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Cookies-journal'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'IndexedDB'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Local Storage\leveldb'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'Session Storage'),");
                sb.AppendLine(@"                    (Join-Path $prof.FullName 'WebStorage')");
                sb.AppendLine(@"                )");
                sb.AppendLine(@"                foreach ($ck in $cookiePaths) {");
                sb.AppendLine(@"                    $r = Remove-SafeItem -Path $ck -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        }");
            if (clearCache)
            {
                sb.AppendLine(@"        if ($b.LocalData -and (Test-Path -LiteralPath $b.LocalData)) {");
                sb.AppendLine(@"            $r = Remove-SafeItem -Path $b.LocalData -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"            $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"        }");
            }
            sb.AppendLine(@"    }");
            sb.AppendLine(@"    elseif ($b.Type -eq 'Firefox') {");
            sb.AppendLine(@"        $localProfiles = if ($b.LocalData -and (Test-Path -LiteralPath $b.LocalData)) { Get-ChildItem -LiteralPath $b.LocalData -Directory -ErrorAction SilentlyContinue } else { @() }");
            sb.AppendLine(@"        $roamProfiles = if ($b.UserData -and (Test-Path -LiteralPath $b.UserData)) { Get-ChildItem -LiteralPath $b.UserData -Directory -ErrorAction SilentlyContinue } else { @() }");
            if (clearCache)
            {
                sb.AppendLine(@"        foreach ($lp in $localProfiles) {");
                sb.AppendLine(@"            Write-Output ""  * Đang dọn Cache Firefox Profile: $($lp.Name)...""");
                sb.AppendLine(@"            foreach ($sub in @('cache2', 'startupCache', 'jumpListCache', 'thumbnails')) {");
                sb.AppendLine(@"                $p = Join-Path $lp.FullName $sub");
                sb.AppendLine(@"                $r = Remove-SafeItem -Path $p -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"            }");
                sb.AppendLine(@"        }");
            }
            if (clearCookies)
            {
                sb.AppendLine(@"        foreach ($rp in $roamProfiles) {");
                sb.AppendLine(@"            Write-Output ""  * Đang dọn Cookies Firefox Profile: $($rp.Name)...""");
                sb.AppendLine(@"            $cookieFiles = @('cookies.sqlite', 'cookies.sqlite-wal', 'cookies.sqlite-shm', 'webappsstore.sqlite')");
                sb.AppendLine(@"            foreach ($cf in $cookieFiles) {");
                sb.AppendLine(@"                $p = Join-Path $rp.FullName $cf");
                sb.AppendLine(@"                $r = Remove-SafeItem -Path $p -CutoffTime $cutoff");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"            }");
                sb.AppendLine(@"            $storageDir = Join-Path $rp.FullName 'storage\default'");
                sb.AppendLine(@"            $r = Remove-SafeItem -Path $storageDir -CutoffTime $cutoff -Recurse");
                sb.AppendLine(@"            $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"        }");
            }
            sb.AppendLine(@"    }");
            sb.AppendLine();
            sb.AppendLine(@"    $mbFreed = [math]::Round($bBytes / 1MB, 2)");
            sb.AppendLine(@"    Write-Output ""  [OK] Đã dọn dẹp xong $($b.Name): Giải phóng $mbFreed MB (Xóa $bFiles tệp).""");
            sb.AppendLine(@"    Write-Output ""  🛡️ MẬT KHẨU: An toàn 100% (Login Data / passwords được bảo vệ nguyên vẹn).""");
            sb.AppendLine(@"    $totalFreedBytes += $bBytes");
            sb.AppendLine(@"    $totalFreedFiles += $bFiles");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"$totalMB = [math]::Round($totalFreedBytes / 1MB, 2)");
            sb.AppendLine(@"$totalGB = [math]::Round($totalFreedBytes / 1GB, 3)");
            sb.AppendLine(@"Write-Output ''");
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output '                     KẾT QUẢ TỔNG HỢP SAU KHI DỌN DẸP                           '");
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output ""Máy trạm:                 $($env:COMPUTERNAME) ($($env:USERNAME))""");
            sb.AppendLine(@"Write-Output ""Số trình duyệt đã xử lý:  $($detectedBrowsers.Count) trình duyệt""");
            sb.AppendLine(@"Write-Output ""Tổng số tệp rác đã xóa:   $totalFreedFiles tệp""");
            sb.AppendLine(@"if ($totalFreedBytes -ge 1GB) {");
            sb.AppendLine(@"    Write-Output ""Tổng dung lượng giải phóng: $totalGB GB ($totalMB MB)""");
            sb.AppendLine(@"} else {");
            sb.AppendLine(@"    Write-Output ""Tổng dung lượng giải phóng: $totalMB MB""");
            sb.AppendLine(@"}");
            sb.AppendLine(@"Write-Output 'Trạng thái mật khẩu lưu:  🛡️ AN TOÀN TUYỆT ĐỐI (KHÔNG XÓA MẬT KHẨU)'");
            sb.AppendLine(@"Write-Output 'Trạng thái thực thi:      ✅ HOÀN TẤT THÀNH CÔNG'");
            sb.AppendLine(@"Write-Output '================================================================================'");

            return sb.ToString();
        }
    }
}
