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
            this.Text = "LAN Control - Xóa Dữ Liệu Duyệt Web & Cache Trình Duyệt (Delete Browsing Data)";
            this.Size = new Size(630, 690);
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
                Text = "🧹 XÓA CACHE & DỮ LIỆU DUYỆT WEB TOÀN BỘ TRÌNH DUYỆT",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            Label lblSubTitle = new Label
            {
                Text = string.Format("Mục tiêu: {0} | Chỉ xóa dữ liệu duyệt web, giữ nguyên cấu hình & thư mục khác", _targetSummary),
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
                Width = 560
            };
            _cboTimeRange.Items.Add("Từ trước đến nay (All time) - [Khuyên dùng để xóa sạch triệt để]");
            _cboTimeRange.Items.Add("24 giờ qua (Last 24 hours)");
            _cboTimeRange.Items.Add("7 ngày qua (Last 7 days)");
            _cboTimeRange.Items.Add("4 tuần qua (Last 4 weeks)");
            _cboTimeRange.SelectedIndex = 0;
            curY += 45;

            // Options Checkboxes Group (Matches Chrome/Edge "Delete browsing data" dialog)
            Label lblCategories = new Label
            {
                Text = "Các hạng mục dữ liệu duyệt web cần xóa (Delete browsing data):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow,
                Location = new Point(20, curY),
                AutoSize = true
            };
            curY += 28;

            Panel pnlOptions = new Panel
            {
                Location = new Point(20, curY),
                Width = 560,
                Height = 220,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle
            };

            int optY = 10;

            _chkHistory = CreateOptionCheckbox(
                "Lịch sử duyệt web (Browsing history)",
                "Xóa lịch sử trang web đã truy cập, bộ nhớ phiên (Sessions), gợi ý thanh địa chỉ.",
                true, ref optY, pnlOptions);

            _chkCookies = CreateOptionCheckbox(
                "Cookies và dữ liệu trang web khác (Cookies and other site data)",
                "Xóa Cookies, Local/Session Storage, IndexedDB, Service Worker, DIPS của các trang web.",
                true, ref optY, pnlOptions);

            _chkCache = CreateOptionCheckbox(
                "Tệp và hình ảnh được lưu trong bộ nhớ đệm (Cached images and files)",
                "Xóa bộ nhớ đệm trang web (Cache, Code Cache, GPUCache, DawnCache, ShaderCache).",
                true, ref optY, pnlOptions);

            _chkDownloads = CreateOptionCheckbox(
                "Lịch sử tải xuống (Download history)",
                "Chỉ xóa danh sách lịch sử tải trên trình duyệt (TUYỆT ĐỐI KHÔNG xóa tệp đã tải trong máy).",
                true, ref optY, pnlOptions);

            _chkKillBrowsers = CreateOptionCheckbox(
                "Tự động đóng trình duyệt trước khi xóa (Bắt buộc để mở khóa tệp History/Cookies)",
                "Đóng hoàn toàn tiến trình trình duyệt & chạy ngầm để giải phóng khóa SQLite trước khi xóa.",
                true, ref optY, pnlOptions);

            pnlOptions.Height = optY + 10;
            curY += pnlOptions.Height + 15;

            // Protection Shield Banner
            Panel pnlShield = new Panel
            {
                Location = new Point(20, curY),
                Width = 560,
                Height = 88,
                BackColor = Color.FromArgb(18, 38, 28),
                Padding = new Padding(14),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblShieldHeader = new Label
            {
                Text = "🛡️ AN TOÀN DỮ LIỆU: CHỈ XÓA DỮ LIỆU DUYỆT WEB - GIỮ NGUYÊN THƯ MỤC KHÁC",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(12, 10)
            };

            Label lblShieldBody = new Label
            {
                Text = "Chỉ xóa đúng 4 mục dữ liệu thao tác duyệt web như hộp thoại 'Delete browsing data'. TUYỆT ĐỐI KHÔNG XÓA: Mật khẩu đã lưu (Login Data), Dữ liệu biểu mẫu (Autofill/Web Data), Dấu trang (Bookmarks), Tiện ích mở rộng (Extensions), Cài đặt trang (Preferences) hay bất kỳ thư mục nào khác.",
                Font = ModernTheme.SmallFont,
                ForeColor = Color.FromArgb(200, 240, 210),
                Location = new Point(12, 32),
                Size = new Size(532, 48)
            };

            pnlShield.Controls.Add(lblShieldHeader);
            pnlShield.Controls.Add(lblShieldBody);
            curY += pnlShield.Height + 12;

            Label lblBrowsersInfo = new Label
            {
                Text = "🔍 Áp dụng cho tất cả trình duyệt: Google Chrome, Microsoft Edge, Cốc Cốc, Firefox, Brave, Opera, Vivaldi...",
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
                Text = "⚡ Thực Thi Xóa Ngay (Delete Data)",
                Size = new Size(235, 36),
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
                Location = new Point(260, 12),
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
                Location = new Point(490, 12),
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
            if (clearHistory) catList.Add("Browsing history (Lịch sử duyệt web)");
            if (clearCookies) catList.Add("Cookies & site data (Cookies & dữ liệu trang web)");
            if (clearCache) catList.Add("Cached images & files (Bộ nhớ đệm tệp & hình ảnh)");
            if (clearDownloads) catList.Add("Download history (Lịch sử tải xuống)");
            string catDesc = catList.Count > 0 ? string.Join(", ", catList.ToArray()) : "Không có mục nào được chọn";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"# ================================================================================");
            sb.AppendLine(@"# LAN CONTROL - XÓA DỮ LIỆU DUYỆT WEB & CACHE TẤT CẢ TRÌNH DUYỆT (DELETE BROWSING DATA)");
            sb.AppendLine(@"# Chỉ xóa đúng dữ liệu thao tác duyệt web - Giữ nguyên mật khẩu, autofill, tiện ích & thư mục khác");
            sb.AppendLine(@"# ================================================================================");
            sb.AppendLine(@"$ErrorActionPreference = 'SilentlyContinue'");
            sb.AppendLine(@"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8");
            sb.AppendLine(@"$OutputEncoding = [System.Text.Encoding]::UTF8");
            sb.AppendLine(@"try { [Console]::InputEncoding = [System.Text.Encoding]::UTF8 } catch {}");
            sb.AppendLine(@"try { chcp 65001 >$null } catch {}");
            sb.AppendLine();
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output '       XÓA DỮ LIỆU DUYỆT WEB & CACHE TRÌNH DUYỆT (DELETE BROWSING DATA)         '");
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output ""Thời gian thực thi: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')""");
            sb.AppendLine(@"Write-Output ""Tên máy tính:       $($env:COMPUTERNAME)""");
            sb.AppendLine(string.Format("Write-Output \"Khoảng thời gian:   {0}\"", timeRangeDesc));
            sb.AppendLine(string.Format("Write-Output \"Hạng mục xóa:       {0}\"", catDesc));
            sb.AppendLine(@"Write-Output '🛡️ CAM KẾT AN TOÀN: Chỉ xóa dữ liệu duyệt web; KHÔNG xóa Mật khẩu, Autofill, Bookmarks, Extensions hay thư mục khác.'");
            sb.AppendLine(@"Write-Output '--------------------------------------------------------------------------------'");
            sb.AppendLine();
            sb.AppendLine(string.Format("$cutoff = {0}", cutoffExpr));
            sb.AppendLine();

            if (killBrowsersFirst)
            {
                sb.AppendLine(@"# [TIẾN TRÌNH] Đóng hoàn toàn các trình duyệt đang mở để giải phóng khóa SQLite (History, Cookies, Cache)");
                sb.AppendLine(@"Write-Output '[BƯỚC 1] ĐANG ĐÓNG CÁC TIẾN TRÌNH TRÌNH DUYỆT ĐỂ MỞ KHÓA DỮ LIỆU DUYỆT WEB...'");
                sb.AppendLine(@"$browserProcs = @('chrome', 'msedge', 'msedgewebview2', 'browser', 'coc_coc_browser', 'firefox', 'brave', 'opera', 'vivaldi')");
                sb.AppendLine(@"foreach ($pName in $browserProcs) {");
                sb.AppendLine(@"    $procs = @(Get-Process -Name $pName -ErrorAction SilentlyContinue)");
                sb.AppendLine(@"    if ($procs.Count -gt 0) {");
                sb.AppendLine(@"        Write-Output ""  -> Đang đóng trình duyệt: $pName ($($procs.Count) tiến trình)...""");
                sb.AppendLine(@"        & taskkill.exe /F /IM ""$pName.exe"" /T >$null 2>&1");
                sb.AppendLine(@"        Stop-Process -Name $pName -Force -ErrorAction SilentlyContinue");
                sb.AppendLine(@"    }");
                sb.AppendLine(@"}");
                sb.AppendLine(@"# Chờ tối đa 4 giây cho đến khi toàn bộ tiến trình trình duyệt giải phóng hoàn toàn khóa tệp SQLite");
                sb.AppendLine(@"for ($wait = 0; $wait -lt 8; $wait++) {");
                sb.AppendLine(@"    $stillRunning = @(Get-Process -Name @('chrome', 'msedge', 'browser', 'coc_coc_browser', 'firefox', 'brave', 'opera', 'vivaldi') -ErrorAction SilentlyContinue)");
                sb.AppendLine(@"    if ($stillRunning.Count -eq 0) { break }");
                sb.AppendLine(@"    foreach ($rp in $stillRunning) { try { Stop-Process -Id $rp.Id -Force -ErrorAction SilentlyContinue } catch {} }");
                sb.AppendLine(@"    Start-Sleep -Milliseconds 400");
                sb.AppendLine(@"}");
                sb.AppendLine(@"Start-Sleep -Milliseconds 300");
                sb.AppendLine();
            }

            sb.AppendLine(@"# DANH SÁCH TỆP CẤU HÌNH, MẬT KHẨU, AUTOFILL, BOOKMARKS ĐƯỢC BẢO VỆ TUYỆT ĐỐI");
            sb.AppendLine(@"$protectedFilesRegex = '^(Login Data|Login Data-journal|Login Data-wal|Login Data-shm|Login Data For Account|Login Data For Account-journal|Web Data|Web Data-journal|Web Data-wal|Account Web Data|Account Web Data-journal|Local State|Preferences|Secure Preferences|Bookmarks|bookmarks\.bak|AccountBookmarks|EncryptedBookmarks2|EncryptedAccountBookmarks2|Extension Cookies|Extension Cookies-journal|logins\.json|key4\.db|key3\.db|cert9\.db|permissions\.sqlite|content-prefs\.sqlite|addons\.json|extensions\.json|prefs\.js)$'");
            sb.AppendLine();
            sb.AppendLine(@"# Hàm chỉ xóa đúng mục dữ liệu duyệt web hoặc làm rỗng thư mục Cache/Storage chỉ định");
            sb.AppendLine(@"function Clear-BrowsingDataTarget {");
            sb.AppendLine(@"    param(");
            sb.AppendLine(@"        [string]$TargetPath,");
            sb.AppendLine(@"        $CutoffTime = $null,");
            sb.AppendLine(@"        [switch]$IsDirectoryContent");
            sb.AppendLine(@"    )");
            sb.AppendLine(@"    if (-not (Test-Path -LiteralPath $TargetPath)) { return @{ Count = 0; Bytes = 0 } }");
            sb.AppendLine(@"    $deletedCount = 0");
            sb.AppendLine(@"    $deletedBytes = 0");
            sb.AppendLine();
            sb.AppendLine(@"    try {");
            sb.AppendLine(@"        if ($IsDirectoryContent -or (Test-Path -LiteralPath $TargetPath -PathType Container)) {");
            sb.AppendLine(@"            # Chỉ xóa các tệp và thư mục con bên trong đúng thư mục Cache / Site Storage được chỉ định");
            sb.AppendLine(@"            $allFiles = @(Get-ChildItem -LiteralPath $TargetPath -File -Recurse -Force -ErrorAction SilentlyContinue)");
            sb.AppendLine(@"            foreach ($f in $allFiles) {");
            sb.AppendLine(@"                if ($f.Name -match $protectedFilesRegex) { continue }");
            sb.AppendLine(@"                if ($CutoffTime -and ($f.LastWriteTime -lt $CutoffTime)) { continue }");
            sb.AppendLine(@"                try {");
            sb.AppendLine(@"                    $len = $f.Length");
            sb.AppendLine(@"                    $f.Attributes = 'Normal'");
            sb.AppendLine(@"                    Remove-Item -LiteralPath $f.FullName -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"                    if (-not (Test-Path -LiteralPath $f.FullName)) {");
            sb.AppendLine(@"                        $deletedCount++");
            sb.AppendLine(@"                        $deletedBytes += $len");
            sb.AppendLine(@"                    }");
            sb.AppendLine(@"                } catch {}");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"            # Nếu xóa toàn bộ (All time), dọn sạch luôn các thư mục con rỗng của origin trong IndexedDB/CacheStorage/ServiceWorker");
            sb.AppendLine(@"            if (-not $CutoffTime) {");
            sb.AppendLine(@"                $subDirs = @(Get-ChildItem -LiteralPath $TargetPath -Directory -Force -ErrorAction SilentlyContinue)");
            sb.AppendLine(@"                foreach ($sd in $subDirs) {");
            sb.AppendLine(@"                    try { Remove-Item -LiteralPath $sd.FullName -Recurse -Force -ErrorAction SilentlyContinue } catch {}");
            sb.AppendLine(@"                }");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        } else {");
            sb.AppendLine(@"            $leaf = Split-Path -Path $TargetPath -Leaf");
            sb.AppendLine(@"            if ($leaf -notmatch $protectedFilesRegex) {");
            sb.AppendLine(@"                $item = Get-Item -LiteralPath $TargetPath -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"                # Với cơ sở dữ liệu SQLite (History, Cookies...), luôn xóa để trình duyệt tạo lại bảng trống sạch");
            sb.AppendLine(@"                if ($item) {");
            sb.AppendLine(@"                    $len = $item.Length");
            sb.AppendLine(@"                    try { $item.Attributes = 'Normal' } catch {}");
            sb.AppendLine(@"                    Remove-Item -LiteralPath $TargetPath -Force -ErrorAction SilentlyContinue");
            sb.AppendLine(@"                    if (-not (Test-Path -LiteralPath $TargetPath)) {");
            sb.AppendLine(@"                        $deletedCount++");
            sb.AppendLine(@"                        $deletedBytes += $len");
            sb.AppendLine(@"                    }");
            sb.AppendLine(@"                }");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        }");
            sb.AppendLine(@"    } catch {}");
            sb.AppendLine(@"    return @{ Count = $deletedCount; Bytes = $deletedBytes }");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"# Thu thập tất cả thư mục người dùng trên máy (đảm bảo xóa đúng cả khi Agent chạy quyền SYSTEM hoặc Admin)");
            sb.AppendLine(@"$userDirs = @(Get-ChildItem -LiteralPath 'C:\Users' -Directory -Force -ErrorAction SilentlyContinue | Where-Object {");
            sb.AppendLine(@"    $_.Name -notmatch '^(Public|Default|Default User|All Users)$'");
            sb.AppendLine(@"})");
            sb.AppendLine(@"if ($userDirs.Count -eq 0 -and $env:USERPROFILE) {");
            sb.AppendLine(@"    $userDirs = @(Get-Item -LiteralPath $env:USERPROFILE -ErrorAction SilentlyContinue)");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"Write-Output '[BƯỚC 2] QUÉT & XÓA DỮ LIỆU DUYỆT WEB TRÊN TẤT CẢ TRÌNH DUYỆT:'");
            sb.AppendLine(@"$totalFreedBytes = 0");
            sb.AppendLine(@"$totalFreedFiles = 0");
            sb.AppendLine(@"$totalBrowsersCleaned = 0");
            sb.AppendLine();
            sb.AppendLine(@"foreach ($uDir in $userDirs) {");
            sb.AppendLine(@"    $localApp = Join-Path $uDir.FullName 'AppData\Local'");
            sb.AppendLine(@"    $roamApp  = Join-Path $uDir.FullName 'AppData\Roaming'");
            sb.AppendLine(@"    if (-not (Test-Path -LiteralPath $localApp)) { continue }");
            sb.AppendLine();
            sb.AppendLine(@"    $browserTargets = @(");
            sb.AppendLine(@"        @{ Name = 'Google Chrome';   Type = 'ChromiumMulti'; Root = (Join-Path $localApp 'Google\Chrome\User Data') },");
            sb.AppendLine(@"        @{ Name = 'Microsoft Edge';  Type = 'ChromiumMulti'; Root = (Join-Path $localApp 'Microsoft\Edge\User Data') },");
            sb.AppendLine(@"        @{ Name = 'Cốc Cốc Browser'; Type = 'ChromiumMulti'; Root = (Join-Path $localApp 'CocCoc\Browser\User Data') },");
            sb.AppendLine(@"        @{ Name = 'Brave Browser';   Type = 'ChromiumMulti'; Root = (Join-Path $localApp 'BraveSoftware\Brave-Browser\User Data') },");
            sb.AppendLine(@"        @{ Name = 'Vivaldi Browser'; Type = 'ChromiumMulti'; Root = (Join-Path $localApp 'Vivaldi\User Data') },");
            sb.AppendLine(@"        @{ Name = 'Opera Browser';   Type = 'ChromiumMulti'; Root = (Join-Path $roamApp  'Opera Software\Opera Stable'); LocalCacheRoot = (Join-Path $localApp 'Opera Software\Opera Stable') },");
            sb.AppendLine(@"        @{ Name = 'Opera GX';        Type = 'ChromiumMulti'; Root = (Join-Path $roamApp  'Opera Software\Opera GX Stable'); LocalCacheRoot = (Join-Path $localApp 'Opera Software\Opera GX Stable') },");
            sb.AppendLine(@"        @{ Name = 'Mozilla Firefox'; Type = 'Firefox';       Root = (Join-Path $roamApp  'Mozilla\Firefox\Profiles'); LocalCacheRoot = (Join-Path $localApp 'Mozilla\Firefox\Profiles') }");
            sb.AppendLine(@"    )");
            sb.AppendLine();
            sb.AppendLine(@"    foreach ($b in $browserTargets) {");
            sb.AppendLine(@"        if (-not (Test-Path -LiteralPath $b.Root)) { continue }");
            sb.AppendLine(@"        $totalBrowsersCleaned++");
            sb.AppendLine(@"        $bFiles = 0; $bBytes = 0");
            sb.AppendLine(@"        Write-Output ''");
            sb.AppendLine(@"        Write-Output ""  [+] Đang xóa dữ liệu duyệt web: $($b.Name) (User: $($uDir.Name))""");
            sb.AppendLine();
            sb.AppendLine(@"        if ($b.Type -eq 'ChromiumMulti') {");
            sb.AppendLine(@"            $uPath = $b.Root");
            if (clearCache)
            {
                sb.AppendLine(@"            # 1. Xóa Shader Cache chung của trình duyệt (không đụng chạm các thư mục hệ thống/tiện ích khác)");
                sb.AppendLine(@"            foreach ($rootCache in @('ShaderCache', 'GrShaderCache', 'GraphiteDawnCache', 'GPUPersistentCache')) {");
                sb.AppendLine(@"                $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $uPath $rootCache) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"            }");
                sb.AppendLine(@"            if ($b.LocalCacheRoot -and (Test-Path -LiteralPath $b.LocalCacheRoot)) {");
                sb.AppendLine(@"                foreach ($opCache in @('Cache', 'Code Cache', 'GPUCache', 'ShaderCache', 'System Cache')) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $b.LocalCacheRoot $opCache) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"            }");
            }
            sb.AppendLine(@"            # Xác định danh sách các Profile trong trình duyệt Chromium (Default, Profile 1, Profile 2... hoặc Opera Root)");
            sb.AppendLine(@"            $profiles = @()");
            sb.AppendLine(@"            if ((Test-Path (Join-Path $uPath 'Preferences')) -or (Test-Path (Join-Path $uPath 'History'))) {");
            sb.AppendLine(@"                $profiles += (Get-Item -LiteralPath $uPath -Force)");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"            $subProfiles = @(Get-ChildItem -LiteralPath $uPath -Directory -Force -ErrorAction SilentlyContinue | Where-Object {");
            sb.AppendLine(@"                $_.Name -match '^(Default|Profile \d+|Guest Profile)$' -or (Test-Path (Join-Path $_.FullName 'Preferences')) -or (Test-Path (Join-Path $_.FullName 'History'))");
            sb.AppendLine(@"            })");
            sb.AppendLine(@"            if ($subProfiles.Count -gt 0) { $profiles += $subProfiles }");
            sb.AppendLine();
            sb.AppendLine(@"            foreach ($prof in $profiles) {");
            sb.AppendLine(@"                Write-Output ""      * Profile: $($prof.Name)""");
            if (clearCache)
            {
                sb.AppendLine(@"                # --- MỤC 1: CACHED IMAGES AND FILES (Bộ nhớ đệm tệp & hình ảnh) ---");
                sb.AppendLine(@"                $cacheDirs = @(");
                sb.AppendLine(@"                    'Cache', 'Cache\Cache_Data', 'Code Cache', 'GPUCache', 'Media Cache',");
                sb.AppendLine(@"                    'DawnCache', 'DawnGraphiteCache', 'DawnWebGPUCache', 'Shared Dictionary\cache',");
                sb.AppendLine(@"                    'JumpListIcons', 'JumpListIconsOld', 'JumpListIconsMostVisited', 'JumpListIconsRecentClosed',");
                sb.AppendLine(@"                    'Application Cache'");
                sb.AppendLine(@"                )");
                sb.AppendLine(@"                foreach ($cd in $cacheDirs) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName $cd) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            if (clearHistory)
            {
                sb.AppendLine(@"                # --- MỤC 2: BROWSING HISTORY (Lịch sử duyệt web & Phiên duyệt web) ---");
                sb.AppendLine(@"                $historyFiles = @(");
                sb.AppendLine(@"                    'History', 'History-journal', 'History-wal', 'History-shm', 'History Provider Cache',");
                sb.AppendLine(@"                    'Visited Links', 'Top Sites', 'Top Sites-journal', 'Top Sites-wal', 'Top Sites-shm',");
                sb.AppendLine(@"                    'Shortcuts', 'Shortcuts-journal', 'Shortcuts-wal', 'Shortcuts-shm',");
                sb.AppendLine(@"                    'Network Action Predictor', 'Network Action Predictor-journal', 'Network Action Predictor-wal',");
                sb.AppendLine(@"                    'Favicons', 'Favicons-journal', 'Favicons-wal', 'Favicons-shm'");
                sb.AppendLine(@"                )");
                sb.AppendLine(@"                foreach ($hf in $historyFiles) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName $hf) -CutoffTime $cutoff");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"                foreach ($sessDir in @('Sessions', 'Sessions_Encrypted', 'Sync Data\LevelDB')) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName $sessDir) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            if (clearDownloads)
            {
                sb.AppendLine(@"                # --- MỤC 3: DOWNLOAD HISTORY (Chỉ xóa lịch sử tải trên trình duyệt, giữ nguyên file trong ổ đĩa) ---");
                sb.AppendLine(@"                foreach ($df in @('DownloadMetadata')) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName $df) -CutoffTime $cutoff");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"                $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName 'Download Service') -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
            }
            if (clearCookies)
            {
                sb.AppendLine(@"                # --- MỤC 4: COOKIES AND OTHER SITE DATA (Cookies & dữ liệu lưu trữ của các trang web) ---");
                sb.AppendLine(@"                $cookieFiles = @(");
                sb.AppendLine(@"                    'Network\Cookies', 'Network\Cookies-journal', 'Network\Cookies-wal', 'Network\Cookies-shm',");
                sb.AppendLine(@"                    'Cookies', 'Cookies-journal', 'Cookies-wal', 'Cookies-shm',");
                sb.AppendLine(@"                    'Network\Trust Tokens', 'Network\Trust Tokens-journal', 'Network\Trust Tokens-wal',");
                sb.AppendLine(@"                    'Network\Reporting and Nel', 'Network\Reporting and Nel-journal', 'Network\Reporting and Nel-wal',");
                sb.AppendLine(@"                    'Network\Network Persistent State', 'Network\TransportSecurity', 'Network\SCT Auditing Pending Reports',");
                sb.AppendLine(@"                    'DIPS', 'DIPS-journal', 'DIPS-wal', 'DIPS-shm',");
                sb.AppendLine(@"                    'SharedStorage', 'SharedStorage-journal', 'SharedStorage-wal', 'SharedStorage-shm',");
                sb.AppendLine(@"                    'BrowsingTopicsSiteData', 'BrowsingTopicsSiteData-journal', 'BrowsingTopicsState',");
                sb.AppendLine(@"                    'QuotaManager', 'QuotaManager-journal', 'QuotaManager-wal', 'QuotaManager-shm',");
                sb.AppendLine(@"                    'MediaDeviceSalts', 'MediaDeviceSalts-journal', 'MediaDeviceSalts-wal'");
                sb.AppendLine(@"                )");
                sb.AppendLine(@"                foreach ($cf in $cookieFiles) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName $cf) -CutoffTime $cutoff");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"                $siteDataDirs = @(");
                sb.AppendLine(@"                    'Local Storage\leveldb', 'Session Storage', 'IndexedDB',");
                sb.AppendLine(@"                    'Service Worker\CacheStorage', 'Service Worker\ScriptCache', 'Service Worker\Database',");
                sb.AppendLine(@"                    'File System', 'WebStorage', 'Storage\ext', 'blob_storage', 'Site Characteristics Database'");
                sb.AppendLine(@"                )");
                sb.AppendLine(@"                foreach ($sdd in $siteDataDirs) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $prof.FullName $sdd) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        }");
            sb.AppendLine(@"        elseif ($b.Type -eq 'Firefox') {");
            if (clearCache)
            {
                sb.AppendLine(@"            if ($b.LocalCacheRoot -and (Test-Path -LiteralPath $b.LocalCacheRoot)) {");
                sb.AppendLine(@"                $ffLocalProfs = @(Get-ChildItem -LiteralPath $b.LocalCacheRoot -Directory -Force -ErrorAction SilentlyContinue)");
                sb.AppendLine(@"                foreach ($lp in $ffLocalProfs) {");
                sb.AppendLine(@"                    foreach ($sub in @('cache2', 'startupCache', 'jumpListCache', 'thumbnails', 'OfflineCache', 'shader-cache')) {");
                sb.AppendLine(@"                        $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $lp.FullName $sub) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                        $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                    }");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"            }");
            }
            sb.AppendLine(@"            $ffRoamProfs = @(Get-ChildItem -LiteralPath $b.Root -Directory -Force -ErrorAction SilentlyContinue)");
            sb.AppendLine(@"            foreach ($rp in $ffRoamProfs) {");
            if (clearHistory)
            {
                sb.AppendLine(@"                foreach ($hf in @('sessionstore.jsonlz4', 'sessionCheckpoints.json', 'places.sqlite-wal', 'places.sqlite-shm')) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $rp.FullName $hf) -CutoffTime $cutoff");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"                $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $rp.FullName 'sessionstore-backups') -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                $bFiles += $r.Count; $bBytes += $r.Bytes");
            }
            if (clearCookies)
            {
                sb.AppendLine(@"                foreach ($cf in @('cookies.sqlite', 'cookies.sqlite-wal', 'cookies.sqlite-shm', 'webappsstore.sqlite', 'webappsstore.sqlite-wal', 'webappsstore.sqlite-shm')) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $rp.FullName $cf) -CutoffTime $cutoff");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
                sb.AppendLine(@"                foreach ($sd in @('storage\default', 'storage\temporary')) {");
                sb.AppendLine(@"                    $r = Clear-BrowsingDataTarget -TargetPath (Join-Path $rp.FullName $sd) -CutoffTime $cutoff -IsDirectoryContent");
                sb.AppendLine(@"                    $bFiles += $r.Count; $bBytes += $r.Bytes");
                sb.AppendLine(@"                }");
            }
            sb.AppendLine(@"            }");
            sb.AppendLine(@"        }");
            sb.AppendLine();
            sb.AppendLine(@"        $mbFreed = [math]::Round($bBytes / 1MB, 2)");
            sb.AppendLine(@"        Write-Output ""      => Đã xóa sạch $bFiles tệp dữ liệu duyệt web ($mbFreed MB). Giữ nguyên toàn bộ thư mục khác của $($b.Name).""");
            sb.AppendLine(@"        $totalFreedBytes += $bBytes");
            sb.AppendLine(@"        $totalFreedFiles += $bFiles");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");
            sb.AppendLine();
            sb.AppendLine(@"$totalMB = [math]::Round($totalFreedBytes / 1MB, 2)");
            sb.AppendLine(@"Write-Output ''");
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output '             KẾT QUẢ XÓA DỮ LIỆU DUYỆT WEB & CACHE TRÌNH DUYỆT                  '");
            sb.AppendLine(@"Write-Output '================================================================================'");
            sb.AppendLine(@"Write-Output ""Máy trạm:                    $($env:COMPUTERNAME)""");
            sb.AppendLine(@"Write-Output ""Số trình duyệt đã dọn dẹp:   $totalBrowsersCleaned trình duyệt""");
            sb.AppendLine(@"Write-Output ""Tổng số mục duyệt web đã xóa: $totalFreedFiles mục ($totalMB MB)""");
            sb.AppendLine(@"Write-Output 'Mật khẩu, Autofill, Tiện ích & Thư mục khác: 🛡️ ĐƯỢC GIỮ NGUYÊN 100%'");
            sb.AppendLine(@"Write-Output 'Trạng thái:                  ✅ ĐÃ XÓA DỮ LIỆU DUYỆT WEB THÀNH CÔNG'");
            sb.AppendLine(@"Write-Output '================================================================================'");

            return sb.ToString();
        }
    }
}
