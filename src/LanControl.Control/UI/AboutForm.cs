using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LanControl.Core.Utils;

namespace LanControl.Control.UI
{
    public class AboutForm : Form
    {
        private string _aboutFilePath;
        private string _changelogFilePath;
        private WebBrowser _wbPreview;
        private Button _btnTabAbout;
        private Button _btnTabChangelog;
        private Button _btnEditChangelog;
        private Button _btnReload;
        private bool _showingChangelog = false;

        public AboutForm(bool openChangelogTab = false)
        {
            _showingChangelog = openChangelogTab;
            _aboutFilePath = ResolveAboutFilePath();
            _changelogFilePath = AppVersion.ResolveChangelogFilePath();
            InitializeComponent();
            UpdateTabButtonStyles();
            if (_showingChangelog)
            {
                LoadChangelogContent();
            }
            else
            {
                LoadAboutContent();
            }
        }

        public static string ResolveAboutFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[]
            {
                Path.Combine(baseDir, "about.md"),
                Path.Combine(baseDir, "..", "about.md"),
                Path.Combine(baseDir, "..", "..", "about.md"),
                Path.Combine(Environment.CurrentDirectory, "about.md"),
                @"d:\lancontrol\about.md"
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch { }
            }

            // Fallback: create in base directory
            return Path.Combine(baseDir, "about.md");
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("Thông Tin Phần Mềm, Phiên Bản ({0}) & Lịch Sử Thay Đổi (About & Changelog)", AppVersion.FullVersion);
            this.Size = new Size(980, 750);
            this.MinimumSize = new Size(820, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };
            this.Icon = this.Owner != null ? this.Owner.Icon : null;

            // TOP HEADER BANNER
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(16, 12, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = string.Format("ℹ️ LAN CONTROL MULTI-VLAN MANAGEMENT SUITE — {0}", AppVersion.FullVersion.ToUpperInvariant()),
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(16, 12)
            };

            Label lblSubtitle = new Label
            {
                Text = string.Format("Phiên bản {0} (Phát hành: {1}) — Giải pháp Quản trị Máy trạm, Giám sát Đa VLAN & Hỗ trợ Y tế - Doanh nghiệp", AppVersion.FullVersion, AppVersion.ReleaseDate),
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(18, 38)
            };

            // Quick Info Badges
            FlowLayoutPanel pnlBadges = new FlowLayoutPanel
            {
                Location = new Point(18, 64),
                Size = new Size(940, 36),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            pnlBadges.Controls.Add(CreateBadge("🏷️ Phiên bản: " + AppVersion.FullVersion + " (" + AppVersion.ReleaseDate + ")", Color.FromArgb(22, 160, 133)));
            pnlBadges.Controls.Add(CreateBadge("👨‍💻 Tác giả: Bùi Quang Sáng", Color.FromArgb(41, 128, 185)));
            pnlBadges.Controls.Add(CreateBadge("🏥 Phòng CNTT - BV ĐK Châu Đốc", Color.FromArgb(39, 174, 96)));
            pnlBadges.Controls.Add(CreateBadge("📧 tocnttbkdkkvtinh@gmail.com", Color.FromArgb(142, 68, 173)));
            pnlBadges.Controls.Add(CreateBadge("⚡ Vibe coding từ: mRemoteNG", Color.FromArgb(230, 126, 34)));

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(pnlBadges);

            // NAVIGATION & VERSION MANAGEMENT BAR
            Panel pnlNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(16, 7, 16, 7)
            };

            _btnTabAbout = new Button
            {
                Text = "📖 Giới Thiệu & Tính Năng",
                Location = new Point(16, 7),
                Size = new Size(210, 32)
            };
            _btnTabAbout.Click += (s, e) =>
            {
                _showingChangelog = false;
                UpdateTabButtonStyles();
                LoadAboutContent();
            };

            _btnTabChangelog = new Button
            {
                Text = "🕒 Lịch Sử Phiên Bản (" + AppVersion.FullVersion + ")",
                Location = new Point(234, 7),
                Size = new Size(235, 32)
            };
            _btnTabChangelog.Click += (s, e) =>
            {
                _showingChangelog = true;
                UpdateTabButtonStyles();
                LoadChangelogContent();
            };

            _btnEditChangelog = new Button
            {
                Text = "📝 Mở / Sửa File Lịch Sử (CHANGELOG.md)",
                Location = new Point(477, 7),
                Size = new Size(275, 32)
            };
            ModernTheme.ApplyToButton(_btnEditChangelog, ModernTheme.PanelDark, ModernTheme.AccentYellow);
            _btnEditChangelog.Click += OnEditChangelogClicked;

            _btnReload = new Button
            {
                Text = "🔄 Tải Lại",
                Location = new Point(760, 7),
                Size = new Size(100, 32)
            };
            ModernTheme.ApplyToButton(_btnReload, ModernTheme.PanelDark, ModernTheme.TextPrimary);
            _btnReload.Click += (s, e) =>
            {
                if (_showingChangelog) LoadChangelogContent();
                else LoadAboutContent();
            };

            pnlNav.Controls.Add(_btnTabAbout);
            pnlNav.Controls.Add(_btnTabChangelog);
            pnlNav.Controls.Add(_btnEditChangelog);
            pnlNav.Controls.Add(_btnReload);

            // MAIN PREVIEW BROWSER
            _wbPreview = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = true
            };
            _wbPreview.Navigating += OnBrowserNavigating;

            this.Controls.Add(_wbPreview);
            this.Controls.Add(pnlNav);
            this.Controls.Add(pnlHeader);
        }

        private void UpdateTabButtonStyles()
        {
            if (!_showingChangelog)
            {
                ModernTheme.ApplyToButton(_btnTabAbout, ModernTheme.AccentBlue, Color.White);
                ModernTheme.ApplyToButton(_btnTabChangelog, ModernTheme.PanelDark, ModernTheme.AccentCyan);
            }
            else
            {
                ModernTheme.ApplyToButton(_btnTabAbout, ModernTheme.PanelDark, ModernTheme.TextPrimary);
                ModernTheme.ApplyToButton(_btnTabChangelog, ModernTheme.AccentBlue, Color.White);
            }
        }

        private void LoadChangelogContent()
        {
            string content = AppVersion.GetChangelogMarkdown();
            RenderMarkdownHtml(content);
        }

        private void OnEditChangelogClicked(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(_changelogFilePath))
                {
                    File.WriteAllText(_changelogFilePath, AppVersion.DefaultChangelogMarkdown, Encoding.UTF8);
                }
                Process.Start(new ProcessStartInfo("notepad.exe", "\"" + _changelogFilePath + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể mở tệp CHANGELOG.md: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private Label CreateBadge(string text, Color bg)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                Padding = new Padding(6, 3, 6, 3),
                Margin = new Padding(0, 0, 8, 0)
            };
        }

        private void LoadAboutContent()
        {
            string content = "";
            try
            {
                if (File.Exists(_aboutFilePath))
                {
                    content = File.ReadAllText(_aboutFilePath, Encoding.UTF8);
                }
                else
                {
                    content = GetDefaultAboutMarkdown();
                }
            }
            catch (Exception ex)
            {
                content = "# Lỗi Đọc File\n\nKhông thể đọc tệp `about.md`: " + ex.Message + "\n\n" + GetDefaultAboutMarkdown();
            }

            RenderMarkdownHtml(content);
        }

        private void OnBrowserNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url != null)
            {
                string urlStr = e.Url.ToString();
                if (urlStr.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    urlStr.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    urlStr.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    try
                    {
                        Process.Start(new ProcessStartInfo(urlStr) { UseShellExecute = true });
                    }
                    catch { }
                }
            }
        }

        private void RenderMarkdownHtml(string markdown)
        {
            string bodyHtml = ConvertMarkdownToHtml(markdown);
            string fullHtml = string.Format(@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
    body {{
        background-color: #141820;
        color: #e6ebf2;
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        font-size: 13.5px;
        line-height: 1.6;
        padding: 20px 28px;
        margin: 0;
    }}
    h1 {{
        color: #1abc9c;
        font-size: 22px;
        border-bottom: 2px solid #2a3548;
        padding-bottom: 8px;
        margin-top: 5px;
    }}
    h2 {{
        color: #5dade2;
        font-size: 17px;
        border-bottom: 1px solid #2a3548;
        padding-bottom: 6px;
        margin-top: 22px;
    }}
    h3 {{
        color: #f39c12;
        font-size: 15px;
        margin-top: 16px;
    }}
    a {{
        color: #1abc9c;
        text-decoration: none;
        font-weight: bold;
    }}
    a:hover {{
        text-decoration: underline;
        color: #48c9b0;
    }}
    ul {{
        padding-left: 22px;
        margin: 8px 0;
    }}
    li {{
        margin-bottom: 6px;
    }}
    hr {{
        border: 0;
        border-top: 1px solid #283344;
        margin: 20px 0;
    }}
    .alert-box {{
        background-color: #182230;
        border-left: 4px solid #1abc9c;
        padding: 12px 16px;
        margin: 14px 0;
        border-radius: 4px;
    }}
    code {{
        background-color: #242c3d;
        color: #f1c40f;
        padding: 2px 6px;
        border-radius: 3px;
        font-family: Consolas, monospace;
        font-size: 12.5px;
    }}
    strong {{
        color: #ffffff;
    }}
</style>
</head>
<body>
{0}
</body>
</html>", bodyHtml);

            _wbPreview.DocumentText = fullHtml;
        }

        private string ConvertMarkdownToHtml(string md)
        {
            if (string.IsNullOrEmpty(md)) return "";

            StringBuilder sb = new StringBuilder();
            string[] lines = md.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inList = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // Alert block
                if (trimmed.StartsWith("> [!NOTE]") || trimmed.StartsWith("> [!TIP]") || trimmed.StartsWith("> [!IMPORTANT]"))
                {
                    if (inList) { sb.AppendLine("</ul>"); inList = false; }
                    sb.AppendLine("<div class='alert-box'>");
                    continue;
                }
                if (trimmed.StartsWith(">"))
                {
                    string content = trimmed.Substring(1).Trim();
                    sb.AppendLine(FormatInline(content));
                    if (i + 1 >= lines.Length || !lines[i + 1].Trim().StartsWith(">"))
                    {
                        sb.AppendLine("</div>");
                    }
                    continue;
                }

                // Horizontal Rule
                if (trimmed == "---" || trimmed == "***" || trimmed == "___")
                {
                    if (inList) { sb.AppendLine("</ul>"); inList = false; }
                    sb.AppendLine("<hr/>");
                    continue;
                }

                // Headings
                if (trimmed.StartsWith("### "))
                {
                    if (inList) { sb.AppendLine("</ul>"); inList = false; }
                    sb.AppendLine("<h3>" + FormatInline(trimmed.Substring(4)) + "</h3>");
                    continue;
                }
                if (trimmed.StartsWith("## "))
                {
                    if (inList) { sb.AppendLine("</ul>"); inList = false; }
                    sb.AppendLine("<h2>" + FormatInline(trimmed.Substring(3)) + "</h2>");
                    continue;
                }
                if (trimmed.StartsWith("# "))
                {
                    if (inList) { sb.AppendLine("</ul>"); inList = false; }
                    sb.AppendLine("<h1>" + FormatInline(trimmed.Substring(2)) + "</h1>");
                    continue;
                }

                // Unordered List
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    if (!inList)
                    {
                        sb.AppendLine("<ul>");
                        inList = true;
                    }
                    sb.AppendLine("<li>" + FormatInline(trimmed.Substring(2)) + "</li>");
                    continue;
                }
                else
                {
                    if (inList)
                    {
                        sb.AppendLine("</ul>");
                        inList = false;
                    }
                }

                if (string.IsNullOrEmpty(trimmed))
                {
                    sb.AppendLine("<br/>");
                    continue;
                }

                // Regular Paragraph
                sb.AppendLine("<p>" + FormatInline(line) + "</p>");
            }

            if (inList)
            {
                sb.AppendLine("</ul>");
            }

            return sb.ToString();
        }

        private string FormatInline(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Escape HTML entities safely
            text = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

            // Convert Markdown links [text](url) -> <a href='$2'>$1</a>
            text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "<a href='$2'>$1</a>");

            // Convert bold **text** -> <strong>text</strong>
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<strong>$1</strong>");

            // Convert inline code `text` -> <code>text</code>
            text = Regex.Replace(text, @"`(.*?)`", "<code>$1</code>");

            // Convert italic *text* -> <em>text</em>
            text = Regex.Replace(text, @"\*(.*?)\*", "<em>$1</em>");

            return text;
        }

        private string GetDefaultAboutMarkdown()
        {
            return @"# LAN Control Multi-VLAN Management & Monitoring Suite

> [!NOTE]
> Giải pháp quản trị máy trạm, giám sát đa VLAN tập trung cho mạng doanh nghiệp và y tế.

---

## 📌 Thông Tin Tác Giả & Bản Quyền

- **👨‍💻 Tác giả**: **Bùi Quang Sáng**
- **🏥 Đơn vị công tác**: **Phòng CNTT - Bệnh viện Đa khoa Khu vực Châu Đốc (BV ĐK Châu Đốc)**
- **📧 Email liên hệ**: [tocnttbkdkkvtinh@gmail.com](mailto:tocnttbkdkkvtinh@gmail.com)
- **⚡ Nền tảng phát triển**: Phần mềm được **vibe coding** dựa trên mã nguồn [https://github.com/mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG).
- **📄 Giấy phép**: Mã nguồn mở phục vụ quản trị mạng và ứng dụng y tế.

---

## 🚀 Các Tính Năng Nổi Bật

- **🖥️ Điều Khiển Từ Xa (Remote Desktop)**: Truyền hình ảnh tốc độ cao, hỗ trợ thao tác xác nhận quyền Admin UAC.
- **📡 Phát Sóng Màn Hình (Screen Broadcast)**: Phát màn hình giảng dạy từ máy chủ tới hàng loạt máy trạm.
- **📁 Truyền Tập Tin Hàng Loạt (File Distribution)**: Gửi file cài đặt, tài liệu đồng thời cho nhiều máy khác VLAN.
- **⚡ Chạy Lệnh / Script Từ Xa**: Thực thi PowerShell / Command Prompt tức thì.
- **🔍 Kiểm Kê Cấu Hình & Bản Quyền**: Quét toàn bộ thông tin phần cứng, RAM chi tiết từng thanh, thiết bị ngoại vi và bản quyền Windows.
- **🕒 Lịch Sử Biến Động Cấu Hình**: Theo dõi phát hiện linh kiện bị tháo, phần mềm mới cài, xuất báo cáo ra file Markdown (.md).
";
        }
    }
}
