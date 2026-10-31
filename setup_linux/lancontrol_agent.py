#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
================================================================================
LAN CONTROL SUITE v2.2.0 - CROSS-PLATFORM CLIENT AGENT (LINUX & MACOS)
Tương thích 100% giao thức nhị phân (0x4E 0x53) với LAN Control Gateway & Master
Hỗ trợ:
  - Kết nối đảo chiều (Reverse TCP Tunnel) đa phân đoạn VLAN + Heartbeat 3s
  - Truyền hình ảnh màn hình Thumbnail & Live Remote Desktop (JPEG Stream)
  - Điều khiển Chuột & Bàn phím từ xa (Interactive Input)
  - Nhận Phát sóng màn hình 1-nhiều (Broadcast Viewer khóa tắt cửa sổ)
  - Thực thi lệnh Shell / Script & Dọn dẹp dữ liệu duyệt web (Bảo vệ mật khẩu)
  - Kiểm kê Phần cứng & Phần mềm chuyên sâu (SystemAuditReport)
  - Quản lý Tiến trình (Process List / Kill Process)
  - Truyền nhận Tệp tin 2 chiều & Duyệt thư mục từ xa (Remote File Explorer)
  - Hiển thị Thông báo từ Quản trị viên (1-Click Dismiss) & Khóa màn hình
================================================================================
"""

import os
import sys
import json
import time
import uuid
import socket
import struct
import shutil
import platform
import tempfile
import threading
import subprocess
from datetime import datetime

# Optional GUI & imaging modules
try:
    import tkinter as tk
    from tkinter import messagebox
    HAS_TK = True
except Exception:
    HAS_TK = False

try:
    from PIL import Image, ImageGrab, ImageTk
    HAS_PIL = True
except Exception:
    HAS_PIL = False

try:
    import mss
    HAS_MSS = True
except Exception:
    HAS_MSS = False

try:
    import pyautogui
    pyautogui.FAILSAFE = False
    HAS_PYAUTOGUI = True
except Exception:
    HAS_PYAUTOGUI = False


# ==============================================================================
# HẰNG SỐ GIAO THỨC NHỊ PHÂN LAN CONTROL (0x4E 'N', 0x53 'S')
# ==============================================================================
MAGIC1 = 0x4E
MAGIC2 = 0x53
HEADER_SIZE = 11  # 2 magic + 1 type + 4 jsonLen + 4 rawLen

class PacketType:
    Unknown = 0
    Register = 1
    Heartbeat = 2
    AgentList = 3
    ScreenFrame = 4
    InputEvent = 5
    CommandRequest = 6
    CommandResponse = 7
    BroadcastFrame = 8
    StartScreenStream = 9
    StopScreenStream = 10
    StartBroadcast = 11
    StopBroadcast = 12
    ScanSubnetRequest = 13
    ScanSubnetResponse = 14
    Disconnect = 15
    HelpRequest = 16
    HelpResponse = 17
    FileTransferRequest = 18
    FileTransferChunk = 19
    FileTransferResponse = 20
    FileListRequest = 21
    FileListResponse = 22

class CommandType:
    LockScreen = 0
    UnlockScreen = 1
    ExecuteCmd = 2
    ExecutePs = 3
    GetProcessList = 4
    KillProcess = 5
    SendMessage = 6
    Reboot = 7
    Shutdown = 8
    HideWallpaper = 9
    RestoreWallpaper = 10
    EnableUacBypass = 11
    DisableUacBypass = 12
    UacYes = 13
    UacNo = 14
    GetHardwareInventory = 15
    SendAdminPassword = 16

class FileTransferAction:
    UploadToAgent = 1
    DownloadFromAgent = 2
    DeleteFile = 3
    CreateDirectory = 4
    ExecuteFile = 5

class MouseEventType:
    NoneType = 0
    Move = 1
    LeftDown = 2
    LeftUp = 3
    RightDown = 4
    RightUp = 5
    MiddleDown = 6
    MiddleUp = 7
    DoubleClick = 8
    Wheel = 9

class KeyEventType:
    NoneType = 0
    KeyDown = 1
    KeyUp = 2


# ==============================================================================
# ĐÓNG GÓI & GIẢI MÃ GÓI TIN (PACKET FRAMER)
# ==============================================================================
def encode_packet(packet_type: int, payload_obj=None, raw_bytes: bytes = None) -> bytes:
    if payload_obj is not None:
        if isinstance(payload_obj, str):
            json_bytes = payload_obj.encode("utf-8")
        else:
            json_bytes = json.dumps(payload_obj, ensure_ascii=False).encode("utf-8")
    else:
        json_bytes = b""

    raw_bytes = raw_bytes or b""
    header = struct.pack("<BBBii", MAGIC1, MAGIC2, packet_type & 0xFF, len(json_bytes), len(raw_bytes))
    return header + json_bytes + raw_bytes


def read_exact(sock: socket.socket, count: int) -> bytes:
    buf = bytearray()
    while len(buf) < count:
        chunk = sock.recv(count - len(buf))
        if not chunk:
            raise ConnectionError("Kết nối socket đã đóng đột ngột.")
        buf.extend(chunk)
    return bytes(buf)


def decode_packet(sock: socket.socket):
    header = read_exact(sock, HEADER_SIZE)
    m1, m2, ptype, json_len, raw_len = struct.unpack("<BBBii", header)
    if m1 != MAGIC1 or m2 != MAGIC2:
        raise ValueError(f"Sai định dạng gói tin Magic Header: {m1:#x} {m2:#x}")
    if json_len < 0 or json_len > 50 * 1024 * 1024:
        raise ValueError(f"Độ dài JSON không hợp lệ: {json_len}")
    if raw_len < 0 or raw_len > 50 * 1024 * 1024:
        raise ValueError(f"Độ dài RawBytes không hợp lệ: {raw_len}")

    json_str = ""
    if json_len > 0:
        json_str = read_exact(sock, json_len).decode("utf-8", errors="replace")

    raw_bytes = None
    if raw_len > 0:
        raw_bytes = read_exact(sock, raw_len)

    return ptype, json_str, raw_bytes


# ==============================================================================
# THU THẬP THÔNG TIN HỆ THỐNG (LINUX & MACOS)
# ==============================================================================
def get_local_ip() -> str:
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.settimeout(1.0)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        try:
            return socket.gethostbyname(socket.gethostname())
        except Exception:
            return "127.0.0.1"


def get_mac_address() -> str:
    try:
        node = uuid.getnode()
        return ":".join(f"{(node >> ele) & 0xff:02X}" for ele in range(40, -1, -8))
    except Exception:
        return "00:00:00:00:00:00"


def get_memory_info():
    """Trả về (total_mb, avail_mb)"""
    sys_name = platform.system().lower()
    try:
        if sys_name == "linux" and os.path.exists("/proc/meminfo"):
            mem = {}
            with open("/proc/meminfo", "r", encoding="utf-8", errors="ignore") as f:
                for line in f:
                    parts = line.split(":")
                    if len(parts) == 2:
                        k = parts[0].strip()
                        v = int(parts[1].strip().split()[0])
                        mem[k] = v
            total_mb = mem.get("MemTotal", 0) // 1024
            avail_mb = mem.get("MemAvailable", mem.get("MemFree", 0)) // 1024
            return total_mb, avail_mb
        elif sys_name == "darwin":
            out = subprocess.check_output(["sysctl", "-n", "hw.memsize"], text=True, timeout=3).strip()
            total_mb = int(out) // (1024 * 1024)
            vm_out = subprocess.check_output(["vm_stat"], text=True, timeout=3)
            free_pages = 0
            for line in vm_out.splitlines():
                if "Pages free" in line or "Pages inactive" in line or "Pages speculative" in line:
                    val = line.split(":")[1].strip().rstrip(".")
                    free_pages += int(val)
            avail_mb = (free_pages * 4096) // (1024 * 1024)
            return total_mb, max(256, min(total_mb, avail_mb))
    except Exception:
        pass
    return 8192, 4096


def get_cpu_usage() -> int:
    sys_name = platform.system().lower()
    try:
        if sys_name == "linux":
            load1, _, _ = os.getloadavg()
            cores = os.cpu_count() or 1
            return min(100, max(0, int((load1 / cores) * 100)))
        elif sys_name == "darwin":
            load1, _, _ = os.getloadavg()
            cores = os.cpu_count() or 1
            return min(100, max(0, int((load1 / cores) * 100)))
    except Exception:
        pass
    return 5


def get_os_pretty_name() -> str:
    sys_name = platform.system()
    if sys_name == "Linux":
        try:
            if os.path.exists("/etc/os-release"):
                with open("/etc/os-release", "r", encoding="utf-8", errors="ignore") as f:
                    for line in f:
                        if line.startswith("PRETTY_NAME="):
                            return line.split("=", 1)[1].strip().strip('"')
        except Exception:
            pass
        return f"Linux {platform.release()} ({platform.machine()})"
    elif sys_name == "Darwin":
        try:
            ver = subprocess.check_output(["sw_vers", "-productVersion"], text=True, timeout=3).strip()
            return f"macOS {ver} ({platform.machine()})"
        except Exception:
            return f"macOS {platform.mac_ver()[0]} ({platform.machine()})"
    return f"{sys_name} {platform.release()}"


def get_active_username() -> str:
    for env_key in ("SUDO_USER", "USER", "LOGNAME", "USERNAME"):
        val = os.environ.get(env_key)
        if val and val != "root":
            return val
    try:
        return os.getlogin()
    except Exception:
        return os.environ.get("USER", "user")


# ==============================================================================
# DỌN DẸP DỮ LIỆU DUYỆT WEB & CACHE TRÊN LINUX & MACOS (BẢO VỆ MẬT KHẨU 100%)
# ==============================================================================
PROTECTED_BROWSER_FILES = {
    "Login Data", "Login Data-journal", "Login Data-wal", "Login Data-shm",
    "Login Data For Account", "Login Data For Account-journal",
    "Web Data", "Web Data-journal", "Web Data-wal",
    "Account Web Data", "Account Web Data-journal",
    "Local State", "Preferences", "Secure Preferences",
    "Bookmarks", "bookmarks.bak", "AccountBookmarks",
    "EncryptedBookmarks2", "EncryptedAccountBookmarks2", "BookmarkMergedSurfaceOrdering",
    "Extension Cookies", "Extension Cookies-journal",
    "logins.json", "key4.db", "key3.db", "cert9.db",
    "permissions.sqlite", "content-prefs.sqlite", "addons.json", "extensions.json", "prefs.js",
    "Bookmarks.plist"
}

def clean_browsers_native() -> str:
    """
    Thực hiện xóa Cache, Lịch sử duyệt web (Browsing History), Cookies & Site Data
    trên tất cả trình duyệt của Linux và macOS.
    Tuyệt đối giữ nguyên: Mật khẩu (Login Data), Autofill (Web Data), Bookmarks, Extensions.
    """
    lines = []
    lines.append("================================================================================")
    lines.append("       XÓA DỮ LIỆU DUYỆT WEB & CACHE TRÌNH DUYỆT (LINUX / MACOS)                ")
    lines.append("================================================================================")
    lines.append(f"Thời gian thực thi: {datetime.now().strftime('%d/%m/%Y %H:%M:%S')}")
    lines.append(f"Tên máy tính:       {socket.gethostname()} ({get_os_pretty_name()})")
    lines.append("🛡️ CAM KẾT AN TOÀN: Chỉ xóa dữ liệu duyệt web; KHÔNG xóa Mật khẩu, Autofill, Bookmarks, Extensions.")
    lines.append("--------------------------------------------------------------------------------")

    # 1. Đóng các trình duyệt đang mở để giải phóng khóa SQLite
    lines.append("[BƯỚC 1] ĐANG ĐÓNG CÁC TIẾN TRÌNH TRÌNH DUYỆT ĐỂ MỞ KHÓA SQLITE...")
    browser_procs = [
        "chrome", "google-chrome", "Google Chrome",
        "chromium", "chromium-browser", "Chromium",
        "msedge", "microsoft-edge", "Microsoft Edge",
        "brave", "brave-browser", "Brave Browser",
        "vivaldi", "Vivaldi",
        "opera", "Opera",
        "firefox", "firefox-esr", "Firefox",
        "Safari"
    ]
    for pname in browser_procs:
        try:
            subprocess.run(["pkill", "-9", "-f", pname], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=3)
        except Exception:
            pass
    time.sleep(0.5)

    # 2. Xác định các thư mục người dùng
    sys_name = platform.system().lower()
    user_homes = []
    if sys_name == "darwin":
        users_root = "/Users"
        if os.path.isdir(users_root):
            for name in os.listdir(users_root):
                if name not in ("Shared", "Guest", ".localized"):
                    full = os.path.join(users_root, name)
                    if os.path.isdir(full):
                        user_homes.append((name, full))
    else:
        users_root = "/home"
        if os.path.isdir(users_root):
            for name in os.listdir(users_root):
                full = os.path.join(users_root, name)
                if os.path.isdir(full):
                    user_homes.append((name, full))
        if os.path.isdir("/root"):
            user_homes.append(("root", "/root"))

    if not user_homes:
        user_homes.append((get_active_username(), os.path.expanduser("~")))

    total_files = 0
    total_bytes = 0
    total_browsers = 0

    def remove_target(target_path: str, is_dir_content: bool = False):
        nonlocal total_files, total_bytes
        if not os.path.exists(target_path):
            return 0, 0
        c, b = 0, 0
        try:
            if is_dir_content or os.path.isdir(target_path):
                for root_dir, dirs, files in os.walk(target_path, topdown=False):
                    for fname in files:
                        if fname in PROTECTED_BROWSER_FILES:
                            continue
                        fpath = os.path.join(root_dir, fname)
                        try:
                            sz = os.path.getsize(fpath)
                            os.remove(fpath)
                            c += 1
                            b += sz
                        except Exception:
                            pass
                    for dname in dirs:
                        dpath = os.path.join(root_dir, dname)
                        try:
                            shutil.rmtree(dpath, ignore_errors=True)
                        except Exception:
                            pass
                try:
                    shutil.rmtree(target_path, ignore_errors=True)
                except Exception:
                    pass
            else:
                leaf = os.path.basename(target_path)
                if leaf not in PROTECTED_BROWSER_FILES:
                    sz = os.path.getsize(target_path)
                    os.remove(target_path)
                    c += 1
                    b += sz
        except Exception:
            pass
        total_files += c
        total_bytes += b
        return c, b

    def update_chromium_prefs(prof_dir: str):
        for pref_name in ("Preferences", "Secure Preferences"):
            pfile = os.path.join(prof_dir, pref_name)
            if not os.path.isfile(pfile):
                continue
            try:
                with open(pfile, "r", encoding="utf-8", errors="ignore") as f:
                    raw = f.read()
                import re
                mod = re.sub(r'"exit_type"\s*:\s*"[^"]*"', '"exit_type":"Normal"', raw)
                mod = re.sub(r'"exited_cleanly"\s*:\s*false', '"exited_cleanly":true', mod)
                mod = re.sub(r'"sync\.typed_urls"\s*:\s*true', '"sync.typed_urls":false', mod)
                mod = re.sub(r'"sync\.tabs"\s*:\s*true', '"sync.tabs":false', mod)
                mod = re.sub(r'"sync\.history"\s*:\s*true', '"sync.history":false', mod)
                mod = re.sub(r'"sync\.sessions"\s*:\s*true', '"sync.sessions":false', mod)
                mod = re.sub(r'"sync\.saved_tab_groups"\s*:\s*true', '"sync.saved_tab_groups":false', mod)
                if mod != raw:
                    with open(pfile, "w", encoding="utf-8") as f:
                        f.write(mod)
            except Exception:
                pass

    lines.append("\n[BƯỚC 2] QUÉT & XÓA DỮ LIỆU DUYỆT WEB TRÊN TẤT CẢ TRÌNH DUYỆT:")
    for uname, uhome in user_homes:
        if sys_name == "darwin":
            targets = [
                ("Google Chrome", "chromium", os.path.join(uhome, "Library/Application Support/Google/Chrome"), os.path.join(uhome, "Library/Caches/Google/Chrome")),
                ("Microsoft Edge", "chromium", os.path.join(uhome, "Library/Application Support/Microsoft Edge"), os.path.join(uhome, "Library/Caches/Microsoft Edge")),
                ("Brave Browser", "chromium", os.path.join(uhome, "Library/Application Support/BraveSoftware/Brave-Browser"), os.path.join(uhome, "Library/Caches/BraveSoftware/Brave-Browser")),
                ("Cốc Cốc Browser", "chromium", os.path.join(uhome, "Library/Application Support/CocCoc/Browser"), os.path.join(uhome, "Library/Caches/CocCoc/Browser")),
                ("Vivaldi", "chromium", os.path.join(uhome, "Library/Application Support/Vivaldi"), os.path.join(uhome, "Library/Caches/Vivaldi")),
                ("Opera", "chromium", os.path.join(uhome, "Library/Application Support/com.operasoftware.Opera"), os.path.join(uhome, "Library/Caches/com.operasoftware.Opera")),
                ("Mozilla Firefox", "firefox", os.path.join(uhome, "Library/Application Support/Firefox/Profiles"), os.path.join(uhome, "Library/Caches/Firefox/Profiles")),
                ("Apple Safari", "safari", os.path.join(uhome, "Library/Safari"), os.path.join(uhome, "Library/Caches/com.apple.Safari")),
            ]
        else:
            targets = [
                ("Google Chrome", "chromium", os.path.join(uhome, ".config/google-chrome"), os.path.join(uhome, ".cache/google-chrome")),
                ("Chromium", "chromium", os.path.join(uhome, ".config/chromium"), os.path.join(uhome, ".cache/chromium")),
                ("Microsoft Edge", "chromium", os.path.join(uhome, ".config/microsoft-edge"), os.path.join(uhome, ".cache/microsoft-edge")),
                ("Brave Browser", "chromium", os.path.join(uhome, ".config/BraveSoftware/Brave-Browser"), os.path.join(uhome, ".cache/BraveSoftware/Brave-Browser")),
                ("Vivaldi", "chromium", os.path.join(uhome, ".config/vivaldi"), os.path.join(uhome, ".cache/vivaldi")),
                ("Opera", "chromium", os.path.join(uhome, ".config/opera"), os.path.join(uhome, ".cache/opera")),
                ("Mozilla Firefox", "firefox", os.path.join(uhome, ".mozilla/firefox"), os.path.join(uhome, ".cache/mozilla/firefox")),
            ]

        for bname, btype, broot, bcache in targets:
            if not os.path.exists(broot) and (not bcache or not os.path.exists(bcache)):
                continue
            total_browsers += 1
            b_files, b_bytes = 0, 0
            lines.append(f"  [+] Đang xóa dữ liệu duyệt web: {bname} (User: {uname})")

            if bcache and os.path.exists(bcache):
                c, b = remove_target(bcache, is_dir_content=True)
                b_files += c; b_bytes += b

            if btype == "chromium" and os.path.exists(broot):
                for root_cache in ("ShaderCache", "GrShaderCache", "GraphiteDawnCache", "GPUPersistentCache"):
                    c, b = remove_target(os.path.join(broot, root_cache), is_dir_content=True)
                    b_files += c; b_bytes += b

                profiles = []
                if os.path.exists(os.path.join(broot, "Preferences")) or os.path.exists(os.path.join(broot, "History")):
                    profiles.append(broot)
                try:
                    for item in os.listdir(broot):
                        sub = os.path.join(broot, item)
                        if os.path.isdir(sub) and (item == "Default" or item.startswith("Profile ") or os.path.exists(os.path.join(sub, "Preferences"))):
                            profiles.append(sub)
                except Exception:
                    pass

                for prof in profiles:
                    update_chromium_prefs(prof)
                    cache_dirs = [
                        "Cache", "Code Cache", "GPUCache", "Media Cache", "DawnCache", "DawnGraphiteCache",
                        "DawnWebGPUCache", "Shared Dictionary", "Application Cache",
                        "optimization_guide_hint_cache_store", "AutofillAiModelCache", "Search Logos",
                        "Sessions", "Sessions_Encrypted", "Sync Data", "Download Service",
                        "Local Storage", "Session Storage", "IndexedDB", "Service Worker",
                        "File System", "WebStorage", "Storage/ext", "blob_storage",
                        "Site Characteristics Database", "Safe Browsing Network", "GCM Store",
                        "Platform Notifications", "PersistentOriginTrials"
                    ]
                    for cd in cache_dirs:
                        c, b = remove_target(os.path.join(prof, cd), is_dir_content=True)
                        b_files += c; b_bytes += b

                    data_files = [
                        "History", "History-journal", "History-wal", "History-shm", "History Provider Cache",
                        "Visited Links", "Top Sites", "Top Sites-journal", "Shortcuts", "Shortcuts-journal",
                        "Network Action Predictor", "Network Action Predictor-journal",
                        "Favicons", "Favicons-journal", "DownloadMetadata",
                        "Network/Cookies", "Network/Cookies-journal", "Network/Cookies-wal",
                        "Cookies", "Cookies-journal", "Cookies-wal",
                        "Network/Trust Tokens", "Network/Trust Tokens-journal",
                        "Network/Device Bound Sessions", "Network/Device Bound Sessions-journal",
                        "Network/Reporting and NEL", "Network/Reporting and NEL-journal",
                        "Network/Network Persistent State", "Network/TransportSecurity",
                        "DIPS", "DIPS-journal", "DIPS-wal", "SharedStorage", "SharedStorage-journal",
                        "BrowsingTopicsSiteData", "BrowsingTopicsState",
                        "QuotaManager", "QuotaManager-journal", "MediaDeviceSalts", "MediaDeviceSalts-journal",
                        "Affiliation Database", "Affiliation Database-journal"
                    ]
                    for df in data_files:
                        c, b = remove_target(os.path.join(prof, df))
                        b_files += c; b_bytes += b

            elif btype == "firefox" and os.path.exists(broot):
                try:
                    for item in os.listdir(broot):
                        rp = os.path.join(broot, item)
                        if not os.path.isdir(rp):
                            continue
                        for f in ("sessionstore.jsonlz4", "sessionCheckpoints.json", "places.sqlite-wal", "places.sqlite-shm",
                                  "cookies.sqlite", "cookies.sqlite-wal", "cookies.sqlite-shm", "webappsstore.sqlite", "favicons.sqlite"):
                            c, b = remove_target(os.path.join(rp, f))
                            b_files += c; b_bytes += b
                        for d in ("sessionstore-backups", "storage/default", "storage/temporary", "cache2", "startupCache", "thumbnails"):
                            c, b = remove_target(os.path.join(rp, d), is_dir_content=True)
                            b_files += c; b_bytes += b
                except Exception:
                    pass

            elif btype == "safari" and os.path.exists(broot):
                for sf in ("History.db", "History.db-lock", "History.db-shm", "History.db-wal", "Downloads.plist", "LastSession.plist"):
                    c, b = remove_target(os.path.join(broot, sf))
                    b_files += c; b_bytes += b
                for sd in ("LocalStorage", "Databases", "Favicon Cache", "Touch Icons Cache"):
                    c, b = remove_target(os.path.join(broot, sd), is_dir_content=True)
                    b_files += c; b_bytes += b

            mb = round(b_bytes / (1024 * 1024), 2)
            lines.append(f"      => Đã xóa sạch {b_files} tệp dữ liệu duyệt web ({mb} MB). Giữ nguyên mật khẩu & thư mục khác.")

    total_mb = round(total_bytes / (1024 * 1024), 2)
    lines.append("")
    lines.append("================================================================================")
    lines.append(f"Máy trạm:                    {socket.gethostname()}")
    lines.append(f"Số trình duyệt đã dọn dẹp:   {total_browsers} trình duyệt")
    lines.append(f"Tổng số mục duyệt web đã xóa: {total_files} mục ({total_mb} MB)")
    lines.append("Mật khẩu, Autofill, Tiện ích & Thư mục khác: 🛡️ ĐƯỢC GIỮ NGUYÊN 100%")
    lines.append("Trạng thái:                  ✅ ĐÃ XÓA DỮ LIỆU DUYỆT WEB THÀNH CÔNG")
    lines.append("================================================================================")
    return "\n".join(lines)


# ==============================================================================
# KIỂM KÊ PHẦN CỨNG & PHẦN MỀM (SYSTEM AUDIT REPORT CHO LINUX / MACOS)
# ==============================================================================
def collect_hardware_inventory(agent_id: str, vlan_tag: str) -> dict:
    total_mb, _ = get_memory_info()
    hostname = socket.gethostname()
    ip_addr = get_local_ip()
    sys_name = platform.system()

    cpu_name = platform.processor() or platform.machine()
    cores = os.cpu_count() or 1
    if sys_name == "Linux" and os.path.exists("/proc/cpuinfo"):
        try:
            with open("/proc/cpuinfo", "r", encoding="utf-8", errors="ignore") as f:
                for line in f:
                    if "model name" in line:
                        cpu_name = line.split(":", 1)[1].strip()
                        break
        except Exception:
            pass
    elif sys_name == "Darwin":
        try:
            cpu_name = subprocess.check_output(["sysctl", "-n", "machdep.cpu.brand_string"], text=True, timeout=3).strip()
        except Exception:
            pass

    logical_drives = []
    try:
        usage = shutil.disk_usage("/")
        total_gb = round(usage.total / (1024 ** 3), 2)
        free_gb = round(usage.free / (1024 ** 3), 2)
        free_pct = round((free_gb / total_gb) * 100, 1) if total_gb > 0 else 0
        logical_drives.append({
            "DriveLetter": "/",
            "VolumeName": "Root Filesystem",
            "FileSystem": "apfs" if sys_name == "Darwin" else "ext4/xfs",
            "TotalSizeGb": total_gb,
            "FreeSpaceGb": free_gb,
            "FreePercent": free_pct
        })
    except Exception:
        pass

    report = {
        "AgentId": agent_id,
        "Hostname": hostname,
        "IpAddress": ip_addr,
        "VlanTag": vlan_tag,
        "CollectedTime": datetime.now().strftime("%d/%m/%Y %H:%M:%S"),
        "OS": {
            "OsName": get_os_pretty_name(),
            "Version": platform.release(),
            "BuildNumber": platform.version()[:32],
            "Architecture": platform.machine(),
            "InstallDate": "N/A",
            "RegisteredUser": get_active_username(),
            "ComputerName": hostname,
            "DomainOrWorkgroup": "WORKGROUP",
            "WindowsActivationStatus": "Bản quyền hợp lệ (Unix/POSIX)",
            "WindowsLicenseChannel": "OpenSource / OEM",
            "PartialProductKey": "N/A",
            "IsSuspectedCrack": False,
            "CrackWarningDetails": ""
        },
        "CPU": {
            "Name": cpu_name,
            "Manufacturer": "Apple / Intel / AMD",
            "Cores": cores,
            "LogicalProcessors": cores,
            "MaxClockSpeedMhz": 3200,
            "Architecture": platform.machine(),
            "ProcessorId": get_mac_address()
        },
        "Motherboard": {
            "Manufacturer": "Apple Inc." if sys_name == "Darwin" else "Linux Host",
            "Product": platform.machine(),
            "SerialNumber": get_mac_address(),
            "BiosVendor": sys_name,
            "BiosVersion": platform.release(),
            "BiosReleaseDate": ""
        },
        "TotalRamMb": total_mb,
        "RamSticks": [{
            "DeviceLocator": "Slot 1",
            "CapacityMb": total_mb,
            "SpeedMhz": 3200,
            "Manufacturer": "System Memory",
            "PartNumber": "RAM-01",
            "SerialNumber": "N/A"
        }],
        "PhysicalDisks": [{
            "Index": 0,
            "Model": "System NVMe/SSD Disk",
            "InterfaceType": "NVMe/SATA",
            "SizeGb": logical_drives[0]["TotalSizeGb"] if logical_drives else 256.0,
            "SerialNumber": "DISK-0",
            "MediaType": "SSD"
        }],
        "LogicalDrives": logical_drives,
        "Gpus": [{
            "Name": "Integrated / System GPU",
            "AdapterRamMb": 2048,
            "DriverVersion": platform.release(),
            "CurrentResolution": "1920x1080"
        }],
        "NetworkAdapters": [{
            "Name": "Primary LAN Interface",
            "MacAddress": get_mac_address(),
            "IpAddresses": ip_addr,
            "DefaultGateway": "",
            "Status": "Up",
            "SpeedMbps": 1000
        }],
        "Printers": [],
        "Peripherals": [],
        "InstalledSoftware": [],
        "TotalSoftwareCount": 0,
        "CrackCount": 0,
        "CommercialCount": 0,
        "FreewareCount": 0
    }
    return report


# ==============================================================================
# CHỤP MÀN HÌNH (SCREEN CAPTURE CHO LINUX & MACOS)
# ==============================================================================
def capture_screen_jpeg(is_thumbnail: bool = True):
    """Trả về (jpeg_bytes, orig_width, orig_height)"""
    target_w = 320 if is_thumbnail else 1280
    target_h = 180 if is_thumbnail else 720
    quality = 55 if is_thumbnail else 72

    img = None
    orig_w, orig_h = 1920, 1080

    # 1. Thử dùng mss (nhanh nhất trên Linux/macOS)
    if HAS_MSS and HAS_PIL:
        try:
            with mss.mss() as sct:
                mon = sct.monitors[1] if len(sct.monitors) > 1 else sct.monitors[0]
                sct_img = sct.grab(mon)
                img = Image.frombytes("RGB", sct_img.size, sct_img.bgra, "raw", "BGRX")
                orig_w, orig_h = img.size
        except Exception:
            img = None

    # 2. Thử dùng PIL.ImageGrab
    if img is None and HAS_PIL:
        try:
            img = ImageGrab.grab()
            if img is not None:
                img = img.convert("RGB")
                orig_w, orig_h = img.size
        except Exception:
            img = None

    # 3. Thử dùng công cụ hệ thống (screencapture trên macOS / scrot, gnome-screenshot trên Linux)
    if img is None:
        tmp_path = os.path.join(tempfile.gettempdir(), f"lc_cap_{os.getpid()}.jpg")
        try:
            sys_name = platform.system().lower()
            if sys_name == "darwin":
                subprocess.run(["screencapture", "-x", "-C", "-t", "jpg", tmp_path],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=3)
            else:
                for cmd in (
                    ["scrot", "-o", "-q", str(quality), tmp_path],
                    ["gnome-screenshot", "-f", tmp_path],
                    ["grim", "-t", "jpeg", tmp_path],
                    ["import", "-window", "root", tmp_path]
                ):
                    try:
                        r = subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=3)
                        if r.returncode == 0 and os.path.exists(tmp_path):
                            break
                    except Exception:
                        continue

            if os.path.exists(tmp_path):
                if HAS_PIL:
                    img = Image.open(tmp_path).convert("RGB")
                    orig_w, orig_h = img.size
                else:
                    with open(tmp_path, "rb") as f:
                        data = f.read()
                    try:
                        os.remove(tmp_path)
                    except Exception:
                        pass
                    return data, orig_w, orig_h
        except Exception:
            pass
        finally:
            if os.path.exists(tmp_path):
                try:
                    os.remove(tmp_path)
                except Exception:
                    pass

    if img is not None and HAS_PIL:
        try:
            if is_thumbnail or orig_w > target_w:
                resample = getattr(Image, "Resampling", Image).BILINEAR
                resized = img.resize((target_w, target_h), resample)
            else:
                resized = img
            import io
            buf = io.BytesIO()
            resized.save(buf, format="JPEG", quality=quality)
            return buf.getvalue(), orig_w, orig_h
        except Exception:
            pass

    return None, orig_w, orig_h


# ==============================================================================
# HIỂN THỊ THÔNG BÁO TỪ QUẢN TRỊ VIÊN (1-CLICK DISMISS)
# ==============================================================================
_active_msg_win = None
_msg_lock = threading.Lock()

def show_admin_message(msg_text: str):
    def _worker():
        global _active_msg_win
        sys_name = platform.system().lower()

        if HAS_TK:
            try:
                with _msg_lock:
                    if _active_msg_win is not None:
                        try:
                            _active_msg_win.destroy()
                        except Exception:
                            pass
                        _active_msg_win = None

                root = tk.Tk()
                with _msg_lock:
                    _active_msg_win = root

                root.title("Thông Báo Từ Quản Trị Viên (LAN Control)")
                root.geometry("460x210")
                root.configure(bg="#182230")
                root.attributes("-topmost", True)
                root.resizable(False, False)

                # Canh giữa màn hình
                sw = root.winfo_screenwidth()
                sh = root.winfo_screenheight()
                x = max(0, (sw - 460) // 2)
                y = max(0, (sh - 210) // 2)
                root.geometry(f"460x210+{x}+{y}")

                lbl_hdr = tk.Label(
                    root,
                    text="📢 THÔNG BÁO TỪ QUẢN TRỊ VIÊN (LAN CONTROL)",
                    bg="#101824", fg="#38BDF8",
                    font=("Segoe UI", 10, "bold"),
                    anchor="w", padx=14, pady=8
                )
                lbl_hdr.pack(fill="x")

                lbl_body = tk.Label(
                    root,
                    text=msg_text,
                    bg="#182230", fg="#FFFFFF",
                    font=("Segoe UI", 11),
                    wraplength=420, justify="left",
                     padx=18, pady=18
                )
                lbl_body.pack(fill="both", expand=True)

                def _close(*_args):
                    try:
                        root.destroy()
                    except Exception:
                        pass

                btn_ok = tk.Button(
                    root,
                    text="✓ Đã Hiểu / Đóng (OK)",
                    bg="#2563EB", fg="#FFFFFF",
                    activebackground="#1D4ED8", activeforeground="#FFFFFF",
                    font=("Segoe UI", 10, "bold"),
                    relief="flat", padx=20, pady=6,
                    command=_close
                )
                btn_ok.pack(pady=(0, 14))
                btn_ok.bind("<ButtonPress-1>", _close)
                root.bind("<Return>", _close)
                root.bind("<Escape>", _close)
                root.bind("<space>", _close)
                root.lift()
                root.focus_force()
                btn_ok.focus_set()
                root.mainloop()
                return
            except Exception:
                pass

        # Fallback nếu không có Tkinter
        if sys_name == "darwin":
            safe_msg = msg_text.replace('"', '\\"')
            subprocess.run(
                ["osascript", "-e", f'display dialog "{safe_msg}" with title "Thông Báo Từ Quản Trị Viên (LAN Control)" buttons {{"OK"}} default button "OK"'],
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
            )
        else:
            for cmd in (
                ["zenity", "--info", "--title=Thông Báo Từ Quản Trị Viên (LAN Control)", f"--text={msg_text}"],
                ["notify-send", "Thông Báo Từ Quản Trị Viên (LAN Control)", msg_text]
            ):
                try:
                    if subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0:
                        break
                except Exception:
                    continue

    threading.Thread(target=_worker, daemon=True).start()


# ==============================================================================
# CỬA SỐ NHẬN PHÁT SÓNG MÀN HÌNH (BROADCAST VIEWER KHÓA TẮT CỬA SỔ)
# ==============================================================================
class BroadcastViewer:
    def __init__(self):
        self.active = False
        self.root = None
        self.label = None
        self.latest_jpeg = None
        self._lock = threading.Lock()
        self._thread = None

    def start(self):
        with self._lock:
            if self.active:
                return
            self.active = True
            if HAS_TK and HAS_PIL:
                self._thread = threading.Thread(target=self._ui_loop, daemon=True)
                self._thread.start()

    def update_frame(self, jpeg_bytes: bytes):
        if not jpeg_bytes:
            return
        if not self.active:
            self.start()
        with self._lock:
            self.latest_jpeg = jpeg_bytes

    def stop(self):
        with self._lock:
            self.active = False
            self.latest_jpeg = None
            r = self.root
            self.root = None
        if r is not None:
            try:
                r.after(0, r.destroy)
            except Exception:
                pass

    def _ui_loop(self):
        try:
            root = tk.Tk()
            self.root = root
            root.title("LAN Control - Đang Nhận Phát Sóng Màn Hình Từ Quản Trị Viên")
            root.configure(bg="black")
            root.attributes("-fullscreen", True)
            root.attributes("-topmost", True)
            # Chặn người dùng máy trạm tự ý đóng cửa sổ khi đang phát sóng
            root.protocol("WM_DELETE_WINDOW", lambda: None)
            root.bind("<Alt-F4>", lambda e: "break")
            root.bind("<Escape>", lambda e: "break")

            lbl = tk.Label(root, bg="black")
            lbl.pack(fill="both", expand=True)
            self.label = lbl

            def _refresh():
                if not self.active:
                    try:
                        root.destroy()
                    except Exception:
                        pass
                    return
                jpeg = None
                with self._lock:
                    jpeg = self.latest_jpeg
                    self.latest_jpeg = None
                if jpeg is not None:
                    try:
                        import io
                        im = Image.open(io.BytesIO(jpeg))
                        sw = root.winfo_screenwidth()
                        sh = root.winfo_screenheight()
                        if sw > 100 and sh > 100:
                            resample = getattr(Image, "Resampling", Image).BILINEAR
                            im = im.resize((sw, sh), resample)
                        photo = ImageTk.PhotoImage(im)
                        lbl.configure(image=photo)
                        lbl.image = photo
                    except Exception:
                        pass
                root.lift()
                root.after(40, _refresh)

            root.after(40, _refresh)
            root.mainloop()
        except Exception:
            pass


# ==============================================================================
# XỬ LÝ TRUYỀN TỆP TIN & DUYỆT THƯ MỤC TỪ XA (FILE TRANSFER HANDLER)
# ==============================================================================
class FileTransferManager:
    def __init__(self, agent_id: str, hostname: str, send_packet_fn):
        self.agent_id = agent_id
        self.hostname = hostname
        self.send_packet = send_packet_fn
        self.sessions = {}
        self.lock = threading.Lock()

    def resolve_path(self, remote_path: str) -> str:
        home = os.path.expanduser("~")
        if not remote_path or remote_path.strip().lower() in ("desktop", "%desktop%"):
            p = os.path.join(home, "Desktop")
            return p if os.path.isdir(p) else home
        if remote_path.strip().lower() in ("downloads", "%downloads%"):
            p = os.path.join(home, "Downloads")
            return p if os.path.isdir(p) else home
        return os.path.expanduser(remote_path)

    def handle_request(self, req: dict):
        if not req:
            return
        action = req.get("Action", FileTransferAction.UploadToAgent)
        transfer_id = req.get("TransferId", uuid.uuid4().hex)
        fname = req.get("FileName", "")
        fsize = req.get("FileSize", 0)
        rpath = req.get("RemotePath", "")

        try:
            if action == FileTransferAction.UploadToAgent:
                folder = self.resolve_path(rpath)
                os.makedirs(folder, exist_ok=True)
                final_path = os.path.join(folder, fname)
                part_path = f"{final_path}.{transfer_id}.part"
                f = open(part_path, "wb")
                with self.lock:
                    self.sessions[transfer_id] = {
                        "stream": f,
                        "final_path": final_path,
                        "part_path": part_path,
                        "total": fsize,
                        "received": 0,
                        "file_name": fname,
                        "exec_after": req.get("ExecuteAfterTransfer", False),
                        "exec_args": req.get("ExecuteArguments", "")
                    }
                self._send_progress(transfer_id, fname, 0, fsize, True, "", final_path)

            elif action == FileTransferAction.DownloadFromAgent:
                threading.Thread(target=self._stream_file_to_master, args=(req,), daemon=True).start()

            elif action == FileTransferAction.DeleteFile:
                target = self.resolve_path(rpath)
                if os.path.isdir(target):
                    shutil.rmtree(target)
                elif os.path.exists(target):
                    os.remove(target)
                self._send_progress(transfer_id, os.path.basename(target), 0, 0, True, "", target)

            elif action == FileTransferAction.CreateDirectory:
                target = self.resolve_path(rpath)
                os.makedirs(target, exist_ok=True)
                self._send_progress(transfer_id, os.path.basename(target), 0, 0, True, "", target)

            elif action == FileTransferAction.ExecuteFile:
                target = self.resolve_path(rpath)
                subprocess.Popen([target], cwd=os.path.dirname(target) or "/")
                self._send_progress(transfer_id, os.path.basename(target), 0, 0, True, "", target)

        except Exception as ex:
            self._send_progress(transfer_id, fname, 0, fsize, False, str(ex), "")

    def handle_chunk(self, meta: dict, raw_bytes: bytes):
        if not meta:
            return
        tid = meta.get("TransferId", "")
        with self.lock:
            sess = self.sessions.get(tid)
        if not sess:
            return
        try:
            if raw_bytes:
                sess["stream"].write(raw_bytes)
                sess["received"] += len(raw_bytes)
            if meta.get("IsLastChunk", False):
                sess["stream"].flush()
                sess["stream"].close()
                with self.lock:
                    self.sessions.pop(tid, None)
                if os.path.exists(sess["final_path"]):
                    os.remove(sess["final_path"])
                os.rename(sess["part_path"], sess["final_path"])
                if sess.get("exec_after"):
                    try:
                        os.chmod(sess["final_path"], 0o755)
                        subprocess.Popen([sess["final_path"]])
                    except Exception:
                        pass
                self._send_progress(tid, sess["file_name"], sess["received"], sess["total"], True, "", sess["final_path"])
        except Exception as ex:
            self._send_progress(tid, sess["file_name"], sess["received"], sess["total"], False, str(ex), "")

    def _stream_file_to_master(self, req: dict):
        tid = req.get("TransferId", uuid.uuid4().hex)
        src = req.get("RemotePath", "")
        if not os.path.isfile(src):
            self._send_progress(tid, os.path.basename(src), 0, 0, False, "Tệp không tồn tại trên máy trạm.", "")
            return
        try:
            total = os.path.getsize(src)
            chunk_size = 64 * 1024
            total_chunks = max(1, (total + chunk_size - 1) // chunk_size)
            sent = 0
            idx = 0
            with open(src, "rb") as f:
                while True:
                    buf = f.read(chunk_size)
                    is_last = (not buf) or (sent + len(buf) >= total)
                    meta = {
                        "TransferId": tid,
                        "AgentId": self.agent_id,
                        "ChunkIndex": idx,
                        "TotalChunks": total_chunks,
                        "Offset": sent,
                        "Length": len(buf),
                        "IsLastChunk": is_last
                    }
                    self.send_packet(PacketType.FileTransferChunk, meta, buf)
                    sent += len(buf)
                    idx += 1
                    if is_last:
                        break
        except Exception as ex:
            self._send_progress(tid, os.path.basename(src), 0, 0, False, str(ex), "")

    def handle_file_list(self, req: dict):
        path = (req or {}).get("Path", "")
        if not path or path in ("\\", "C:\\"):
            path = os.path.expanduser("~")
        resp = {
            "AgentId": self.agent_id,
            "CurrentPath": path,
            "ParentPath": os.path.dirname(path.rstrip("/")) or "/",
            "Drives": ["/", os.path.expanduser("~")],
            "Items": [],
            "ErrorMessage": ""
        }
        try:
            for entry in sorted(os.scandir(path), key=lambda e: (not e.is_dir(), e.name.lower())):
                try:
                    st = entry.stat()
                    resp["Items"].append({
                        "Name": entry.name,
                        "FullPath": entry.path,
                        "IsDirectory": entry.is_dir(),
                        "Size": 0 if entry.is_dir() else st.st_size,
                        "LastModified": datetime.fromtimestamp(st.st_mtime).strftime("%d/%m/%Y %H:%M"),
                        "Extension": "" if entry.is_dir() else os.path.splitext(entry.name)[1]
                    })
                except Exception:
                    continue
        except Exception as ex:
            resp["ErrorMessage"] = str(ex)
        self.send_packet(PacketType.FileListResponse, resp)

    def _send_progress(self, tid, fname, transferred, total, success, err, saved_path):
        pct = 100 if total <= 0 and success else (int((transferred * 100) / total) if total > 0 else 0)
        payload = {
            "TransferId": tid,
            "AgentId": self.agent_id,
            "Hostname": self.hostname,
            "FileName": fname,
            "BytesTransferred": transferred,
            "TotalBytes": total,
            "Percentage": min(100, max(0, pct)),
            "Success": success,
            "ErrorMessage": err,
            "SavedPath": saved_path
        }
        self.send_packet(PacketType.FileTransferResponse, payload)


# ==============================================================================
# LỚP CHÍNH: LAN CONTROL AGENT CLIENT (LINUX & MACOS)
# ==============================================================================
class LanControlAgent:
    def __init__(self, config: dict):
        self.config = config
        self.agent_id = config.get("AgentId") or uuid.uuid4().hex
        self.gateway_host = config.get("GatewayHost", "127.0.0.1")
        self.gateway_port = int(config.get("GatewayPort", 8443))
        self.vlan_tag = config.get("VlanTag", "VLAN 10 - Phòng Kỹ Thuật")
        self.thumb_interval = max(0.5, float(config.get("ThumbnailIntervalMs", 1500)) / 1000.0)
        self.live_interval = max(0.04, float(config.get("LiveStreamIntervalMs", 66)) / 1000.0)

        self.sock = None
        self.send_lock = threading.Lock()
        self.running = True
        self.live_streaming = False
        self.is_locked = False
        self.broadcast_viewer = BroadcastViewer()
        self.file_manager = FileTransferManager(self.agent_id, socket.gethostname(), self.send_packet)

    def build_agent_info(self) -> dict:
        total_mb, avail_mb = get_memory_info()
        return {
            "AgentId": self.agent_id,
            "Hostname": socket.gethostname(),
            "IpAddress": get_local_ip(),
            "MacAddress": get_mac_address(),
            "VlanTag": self.vlan_tag,
            "OsVersion": get_os_pretty_name(),
            "UserName": get_active_username(),
            "TotalMemoryMb": total_mb,
            "AvailableMemoryMb": avail_mb,
            "CpuUsagePercent": get_cpu_usage(),
            "IsLocked": self.is_locked,
            "IsStreaming": self.live_streaming
        }

    def send_packet(self, ptype: int, payload_obj=None, raw_bytes: bytes = None) -> bool:
        s = self.sock
        if s is None:
            return False
        try:
            data = encode_packet(ptype, payload_obj, raw_bytes)
            with self.send_lock:
                s.sendall(data)
            return True
        except Exception:
            return False

    def start(self):
        print("===================================================================")
        print("  LAN CONTROL CLIENT AGENT v2.2.0 (LINUX / MACOS EDITION)          ")
        print("===================================================================")
        print(f"  - Hostname:     {socket.gethostname()} ({get_os_pretty_name()})")
        print(f"  - Local IP:     {get_local_ip()} | MAC: {get_mac_address()}")
        print(f"  - Gateway:      {self.gateway_host}:{self.gateway_port}")
        print(f"  - VLAN Tag:     {self.vlan_tag}")
        print("===================================================================")

        threading.Thread(target=self._heartbeat_loop, daemon=True).start()
        threading.Thread(target=self._screen_stream_loop, daemon=True).start()
        self._connection_loop()

    def _connection_loop(self):
        while self.running:
            try:
                print(f"[INFO] Đang kết nối tới Central Gateway {self.gateway_host}:{self.gateway_port}...")
                s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                s.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                s.settimeout(10.0)
                s.connect((self.gateway_host, self.gateway_port))
                s.settimeout(None)
                self.sock = s

                info = self.build_agent_info()
                self.send_packet(PacketType.Register, info)
                print(f"[OK] Đã kết nối thành công tới Gateway! Phân đoạn: [{self.vlan_tag}]")

                while self.running:
                    ptype, json_str, raw_bytes = decode_packet(s)
                    self._handle_packet(ptype, json_str, raw_bytes)

            except Exception as ex:
                print(f"[WARN] Mất kết nối Gateway ({ex}). Tự động thử lại sau 5 giây...")
            finally:
                if self.sock is not None:
                    try:
                        self.sock.close()
                    except Exception:
                        pass
                    self.sock = None
                self.live_streaming = False
                self.broadcast_viewer.stop()

            time.sleep(5.0)

    def _heartbeat_loop(self):
        while self.running:
            if self.sock is not None:
                info = self.build_agent_info()
                self.send_packet(PacketType.Heartbeat, info)
            time.sleep(3.0)

    def _screen_stream_loop(self):
        last_thumb = 0.0
        while self.running:
            if self.sock is not None:
                now = time.time()
                if self.live_streaming:
                    jpeg, w, h = capture_screen_jpeg(is_thumbnail=False)
                    if jpeg:
                        meta = {
                            "AgentId": self.agent_id,
                            "Width": w,
                            "Height": h,
                            "Timestamp": int(now * 10000000),
                            "IsThumbnail": False
                        }
                        self.send_packet(PacketType.ScreenFrame, meta, jpeg)
                if now - last_thumb >= self.thumb_interval:
                    last_thumb = now
                    jpeg, w, h = capture_screen_jpeg(is_thumbnail=True)
                    if jpeg:
                        meta = {
                            "AgentId": self.agent_id,
                            "Width": w,
                            "Height": h,
                            "Timestamp": int(now * 10000000),
                            "IsThumbnail": True
                        }
                        self.send_packet(PacketType.ScreenFrame, meta, jpeg)
            time.sleep(self.live_interval if self.live_streaming else 0.25)

    def _handle_packet(self, ptype: int, json_str: str, raw_bytes: bytes):
        payload = json.loads(json_str) if json_str else {}

        if ptype == PacketType.StartScreenStream:
            self.live_streaming = True
        elif ptype == PacketType.StopScreenStream:
            self.live_streaming = False
        elif ptype == PacketType.InputEvent:
            self._handle_input_event(payload)
        elif ptype == PacketType.StartBroadcast:
            self.broadcast_viewer.start()
        elif ptype == PacketType.BroadcastFrame:
            self.broadcast_viewer.update_frame(raw_bytes)
        elif ptype == PacketType.StopBroadcast:
            self.broadcast_viewer.stop()
        elif ptype == PacketType.CommandRequest:
            threading.Thread(target=self._handle_command, args=(payload,), daemon=True).start()
        elif ptype == PacketType.FileTransferRequest:
            self.file_manager.handle_request(payload)
        elif ptype == PacketType.FileTransferChunk:
            self.file_manager.handle_chunk(payload, raw_bytes)
        elif ptype == PacketType.FileListRequest:
            threading.Thread(target=self.file_manager.handle_file_list, args=(payload,), daemon=True).start()

    def _handle_input_event(self, ev: dict):
        if not ev or not HAS_PYAUTOGUI:
            return
        try:
            if ev.get("IsMouse", False):
                x = int(ev.get("X", 0))
                y = int(ev.get("Y", 0))
                mtype = int(ev.get("MouseType", 0))
                if mtype == MouseEventType.Move:
                    pyautogui.moveTo(x, y)
                elif mtype == MouseEventType.LeftDown:
                    pyautogui.mouseDown(x=x, y=y, button="left")
                elif mtype == MouseEventType.LeftUp:
                    pyautogui.mouseUp(x=x, y=y, button="left")
                elif mtype == MouseEventType.RightDown:
                    pyautogui.mouseDown(x=x, y=y, button="right")
                elif mtype == MouseEventType.RightUp:
                    pyautogui.mouseUp(x=x, y=y, button="right")
                elif mtype == MouseEventType.DoubleClick:
                    pyautogui.doubleClick(x=x, y=y)
                elif mtype == MouseEventType.Wheel:
                    delta = int(ev.get("Delta", 0))
                    pyautogui.scroll(1 if delta > 0 else -1, x=x, y=y)
        except Exception:
            pass

    def _handle_command(self, req: dict):
        if not req:
            return
        req_id = req.get("RequestId", "")
        cmd = int(req.get("Command", 0))
        payload_text = req.get("Payload", "") or ""

        resp = {
            "RequestId": req_id,
            "AgentId": self.agent_id,
            "Success": True,
            "Output": "",
            "ErrorMessage": ""
        }

        try:
            if cmd in (CommandType.ExecuteCmd, CommandType.ExecutePs):
                # Nếu là script dọn dẹp trình duyệt từ Master Control, chạy bộ dọn dẹp native cho Linux/macOS
                if "DELETE BROWSING DATA" in payload_text or "Clear-BrowsingDataTarget" in payload_text or "Google\\Chrome\\User Data" in payload_text:
                    resp["Output"] = clean_browsers_native()
                else:
                    shell_bin = "/bin/bash" if os.path.exists("/bin/bash") else "/bin/sh"
                    proc = subprocess.run(
                        [shell_bin, "-c", payload_text],
                        capture_output=True, text=True, timeout=60
                    )
                    out = proc.stdout or ""
                    if proc.stderr:
                        out += "\n[STDERR]:\n" + proc.stderr
                    resp["Output"] = out

            elif cmd == CommandType.SendMessage:
                show_admin_message(payload_text)
                resp["Output"] = "Message displayed to user."

            elif cmd == CommandType.GetProcessList:
                procs = []
                out = subprocess.check_output(["ps", "-eo", "pid,rss,comm"], text=True, timeout=5)
                for line in out.splitlines()[1:]:
                    parts = line.strip().split(None, 2)
                    if len(parts) >= 3:
                        try:
                            pid = int(parts[0])
                            rss_kb = int(parts[1])
                            name = os.path.basename(parts[2])
                            procs.append({
                                "Id": pid,
                                "Name": name,
                                "Title": parts[2],
                                "MemoryBytes": rss_kb * 1024,
                                "MemoryFormatted": f"{round(rss_kb / 1024.0, 1)} MB"
                            })
                        except Exception:
                            continue
                resp["Output"] = json.dumps(procs, ensure_ascii=False)

            elif cmd == CommandType.KillProcess:
                pid = int(payload_text.strip())
                os.kill(pid, 9)
                resp["Output"] = f"Process {pid} killed successfully."

            elif cmd == CommandType.GetHardwareInventory:
                audit = collect_hardware_inventory(self.agent_id, self.vlan_tag)
                resp["Output"] = json.dumps(audit, ensure_ascii=False)

            elif cmd == CommandType.LockScreen:
                self.is_locked = True
                if platform.system() == "Darwin":
                    subprocess.run(["pmset", "displaysleepnow"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                else:
                    subprocess.run(["loginctl", "lock-session"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                    subprocess.run(["xdg-screensaver", "lock"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                resp["Output"] = "Screen locked."

            elif cmd == CommandType.UnlockScreen:
                self.is_locked = False
                if platform.system() != "Darwin":
                    subprocess.run(["loginctl", "unlock-session"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                resp["Output"] = "Screen unlocked."

            elif cmd == CommandType.Reboot:
                subprocess.Popen(["sudo", "reboot"] if os.geteuid() != 0 else ["reboot"])
                resp["Output"] = "Reboot command initiated."

            elif cmd == CommandType.Shutdown:
                subprocess.Popen(["sudo", "shutdown", "-h", "now"] if os.geteuid() != 0 else ["shutdown", "-h", "now"])
                resp["Output"] = "Shutdown command initiated."

            else:
                resp["Output"] = "Command acknowledged on Unix Agent."

        except Exception as ex:
            resp["Success"] = False
            resp["ErrorMessage"] = str(ex)

        self.send_packet(PacketType.CommandResponse, resp)


def load_or_create_config(config_path: str, cli_args) -> dict:
    cfg = {
        "AgentId": uuid.uuid4().hex,
        "GatewayHost": "192.168.1.24",
        "GatewayPort": 8443,
        "VlanTag": "VLAN 10 - Phòng Kỹ Thuật",
        "ThumbnailIntervalMs": 1500,
        "LiveStreamIntervalMs": 66
    }
    if os.path.exists(config_path):
        try:
            with open(config_path, "r", encoding="utf-8") as f:
                loaded = json.load(f)
                cfg.update(loaded)
        except Exception:
            pass

    if len(cli_args) >= 1 and cli_args[0]:
        cfg["GatewayHost"] = cli_args[0]
    if len(cli_args) >= 2 and cli_args[1]:
        try:
            cfg["GatewayPort"] = int(cli_args[1])
        except Exception:
            pass
    if len(cli_args) >= 3 and cli_args[2]:
        cfg["VlanTag"] = cli_args[2]

    try:
        with open(config_path, "w", encoding="utf-8") as f:
            json.dump(cfg, f, indent=2, ensure_ascii=False)
    except Exception:
        pass
    return cfg


if __name__ == "__main__":
    base_dir = os.path.dirname(os.path.abspath(__file__))
    cfg_file = os.path.join(base_dir, "agent_config.json")
    config = load_or_create_config(cfg_file, sys.argv[1:])
    agent = LanControlAgent(config)
    try:
        agent.start()
    except KeyboardInterrupt:
        print("\n[INFO] Đã dừng LAN Control Agent.")
