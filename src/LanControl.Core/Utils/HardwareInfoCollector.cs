using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using LanControl.Core.Models;

namespace LanControl.Core.Utils
{
    public static class HardwareInfoCollector
    {
        public static SystemAuditReport CollectFullAuditReport(string agentId, string hostname, string ip, string vlan)
        {
            SystemAuditReport report = new SystemAuditReport
            {
                AgentId = agentId,
                Hostname = string.IsNullOrEmpty(hostname) ? Environment.MachineName : hostname,
                IpAddress = ip,
                VlanTag = vlan,
                CollectedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            try { CollectOperatingSystem(report); } catch { }
            try { CollectCpu(report); } catch { }
            try { CollectMotherboardAndBios(report); } catch { }
            try { CollectMemory(report); } catch { }
            try { CollectDisks(report); } catch { }
            try { CollectGpu(report); } catch { }
            try { CollectNetworkAdapters(report); } catch { }
            try { CollectPrinters(report); } catch { }
            try { CollectPeripherals(report); } catch { }
            try { CollectInstalledSoftware(report); } catch { }

            // Calculate summary statistics
            report.TotalSoftwareCount = report.InstalledSoftware.Count;
            int crack = 0, comm = 0, free = 0;
            foreach (var sw in report.InstalledSoftware)
            {
                if (sw.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack) crack++;
                else if (sw.LicenseCategory == SoftwareLicenseCategory.Commercial) comm++;
                else if (sw.LicenseCategory == SoftwareLicenseCategory.Freeware) free++;
            }
            report.CrackCount = crack;
            report.CommercialCount = comm;
            report.FreewareCount = free;

            return report;
        }

        private static void CollectOperatingSystem(SystemAuditReport report)
        {
            report.OS.ComputerName = Environment.MachineName;
            report.OS.RegisteredUser = Environment.UserName;
            report.OS.Architecture = Environment.Is64BitOperatingSystem ? "64-bit (x64)" : "32-bit (x86)";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        report.OS.OsName = GetProp(obj, "Caption");
                        report.OS.Version = GetProp(obj, "Version");
                        report.OS.BuildNumber = GetProp(obj, "BuildNumber");
                        string rawDate = GetProp(obj, "InstallDate");
                        if (!string.IsNullOrEmpty(rawDate) && rawDate.Length >= 8)
                        {
                            try
                            {
                                report.OS.InstallDate = string.Format("{0}/{1}/{2}", rawDate.Substring(6, 2), rawDate.Substring(4, 2), rawDate.Substring(0, 4));
                            }
                            catch { report.OS.InstallDate = rawDate; }
                        }
                        break;
                    }
                }
            }
            catch { }

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string domain = GetProp(obj, "Domain");
                        bool partOfDomain = false;
                        bool.TryParse(GetProp(obj, "PartOfDomain"), out partOfDomain);
                        report.OS.DomainOrWorkgroup = (partOfDomain ? "Domain: " : "Workgroup: ") + domain;
                        break;
                    }
                }
            }
            catch { }

            // Windows Activation & Crack Analysis
            AnalyzeWindowsActivation(report.OS);
        }

        private static void AnalyzeWindowsActivation(OperatingSystemInfo os)
        {
            os.WindowsActivationStatus = "Chưa kích hoạt / Không xác định";
            os.WindowsLicenseChannel = "Không xác định";
            os.IsSuspectedCrack = false;
            os.CrackWarningDetails = "";

            try
            {
                // Query WMI SoftwareLicensingProduct for Windows product
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, Description, LicenseStatus, PartialProductKey FROM SoftwareLicensingProduct WHERE ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059b' AND PartialProductKey IS NOT NULL"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        int licStatus = -1;
                        int.TryParse(GetProp(obj, "LicenseStatus"), out licStatus);
                        string desc = GetProp(obj, "Description") ?? "";
                        string name = GetProp(obj, "Name") ?? "";
                        string partialKey = GetProp(obj, "PartialProductKey") ?? "";

                        if (licStatus == 1) // 1 = Licensed
                        {
                            os.WindowsActivationStatus = "Đã kích hoạt bản quyền vĩnh viễn (Licensed)";
                            os.PartialProductKey = partialKey;

                            if (desc.IndexOf("RETAIL", StringComparison.OrdinalIgnoreCase) >= 0)
                                os.WindowsLicenseChannel = "Bản quyền Bán lẻ (Retail)";
                            else if (desc.IndexOf("OEM", StringComparison.OrdinalIgnoreCase) >= 0)
                                os.WindowsLicenseChannel = "Bản quyền Theo máy (OEM_DM / OEM_COA)";
                            else if (desc.IndexOf("VOLUME_MAK", StringComparison.OrdinalIgnoreCase) >= 0)
                                os.WindowsLicenseChannel = "Bản quyền Doanh nghiệp (Volume:MAK)";
                            else if (desc.IndexOf("VOLUME_KMSCLIENT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     desc.IndexOf("GVLK", StringComparison.OrdinalIgnoreCase) >= 0)
                                os.WindowsLicenseChannel = "Kích hoạt Máy chủ KMS (Volume:KMSClient)";
                            else
                                os.WindowsLicenseChannel = "Kỹ thuật số (Digital License)";

                            break;
                        }
                        else if (licStatus == 2 || licStatus == 3 || licStatus == 4 || licStatus == 6)
                        {
                            os.WindowsActivationStatus = "Đang trong thời gian dùng thử (Grace Period)";
                            os.PartialProductKey = partialKey;
                        }
                        else if (licStatus == 5)
                        {
                            os.WindowsActivationStatus = "Chưa kích hoạt / Hết hạn (Notification)";
                            os.PartialProductKey = partialKey;
                        }
                    }
                }
            }
            catch { }

            // Detect KMS Crack Emulators or Piracy Activators
            List<string> crackNotes = new List<string>();

            // 1. Check for known KMS crack services or tools installed
            string[] suspiciousServices = new string[] { "AutoKMS", "KmsService", "KMSELDI", "SECOMN64", "AActService" };
            foreach (string svc in suspiciousServices)
            {
                try
                {
                    using (RegistryKey svcKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + svc))
                    {
                        if (svcKey != null)
                        {
                            crackNotes.Add("Phát hiện dịch vụ kích hoạt ngầm: " + svc);
                        }
                    }
                }
                catch { }
            }

            // 2. Check for typical crack file paths
            string[] suspiciousPaths = new string[]
            {
                @"C:\Windows\AutoKMS\AutoKMS.exe",
                @"C:\Program Files\KMSpico\KMSELDI.exe",
                @"C:\Program Files (x86)\KMSpico\KMSELDI.exe",
                @"C:\Windows\KMS-R@1n.exe",
                @"C:\Windows\KMS-R@1nH.exe",
                @"C:\Windows\System32\SppExtComObjHook.dll",
                @"C:\Windows\System32\SppExtComObjPatcher.dll"
            };
            foreach (string p in suspiciousPaths)
            {
                if (File.Exists(p))
                {
                    crackNotes.Add("Phát hiện tệp tin kích hoạt crack: " + Path.GetFileName(p));
                }
            }

            // 3. Check hosts file for activation server redirection
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (File.Exists(hostsPath))
                {
                    string content = File.ReadAllText(hostsPath);
                    if (content.IndexOf("kms.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        content.IndexOf("msftconnecttest", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        crackNotes.Add("Tệp hosts có can thiệp chuyển hướng máy chủ kích hoạt");
                    }
                }
            }
            catch { }

            if (crackNotes.Count > 0)
            {
                os.IsSuspectedCrack = true;
                os.CrackWarningDetails = string.Join("; ", crackNotes.ToArray());
                os.WindowsActivationStatus = "⚠️ Kích Hoạt Qua Tool Giả Lập KMS (Nghi Vấn Crack)";
            }
        }

        private static void CollectCpu(SystemAuditReport report)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        report.CPU.Name = (GetProp(obj, "Name") ?? "").Trim();
                        report.CPU.Manufacturer = GetProp(obj, "Manufacturer");
                        int cores = 0, threads = 0;
                        int.TryParse(GetProp(obj, "NumberOfCores"), out cores);
                        int.TryParse(GetProp(obj, "NumberOfLogicalProcessors"), out threads);
                        report.CPU.Cores = cores;
                        report.CPU.LogicalProcessors = threads;
                        uint clock = 0;
                        uint.TryParse(GetProp(obj, "MaxClockSpeed"), out clock);
                        report.CPU.MaxClockSpeedMhz = clock;
                        report.CPU.ProcessorId = GetProp(obj, "ProcessorId");
                        report.CPU.Architecture = Environment.Is64BitOperatingSystem ? "x64 (64-bit)" : "x86 (32-bit)";
                        break;
                    }
                }
            }
            catch { }
        }

        private static void CollectMotherboardAndBios(SystemAuditReport report)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        report.Motherboard.Manufacturer = GetProp(obj, "Manufacturer");
                        report.Motherboard.Product = GetProp(obj, "Product");
                        report.Motherboard.SerialNumber = GetProp(obj, "SerialNumber");
                        break;
                    }
                }
            }
            catch { }

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        report.Motherboard.BiosVendor = GetProp(obj, "Manufacturer");
                        report.Motherboard.BiosVersion = GetProp(obj, "SMBIOSBIOSVersion") ?? GetProp(obj, "Version");
                        string rawDate = GetProp(obj, "ReleaseDate");
                        if (!string.IsNullOrEmpty(rawDate) && rawDate.Length >= 8)
                        {
                            try
                            {
                                report.Motherboard.BiosReleaseDate = string.Format("{0}/{1}/{2}", rawDate.Substring(6, 2), rawDate.Substring(4, 2), rawDate.Substring(0, 4));
                            }
                            catch { report.Motherboard.BiosReleaseDate = rawDate; }
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        private static void CollectMemory(SystemAuditReport report)
        {
            long totalFromApi, avail;
            SystemInfoCollector.GetMemoryInfo(out totalFromApi, out avail);
            report.TotalRamMb = totalFromApi;

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong cap = 0;
                        ulong.TryParse(GetProp(obj, "Capacity"), out cap);
                        uint speed = 0;
                        uint.TryParse(GetProp(obj, "Speed"), out speed);

                        report.RamSticks.Add(new RamStickInfo
                        {
                            DeviceLocator = GetProp(obj, "DeviceLocator") ?? GetProp(obj, "BankLabel") ?? "Slot",
                            CapacityMb = (long)(cap / (1024 * 1024)),
                            SpeedMhz = speed,
                            Manufacturer = (GetProp(obj, "Manufacturer") ?? "").Trim(),
                            PartNumber = (GetProp(obj, "PartNumber") ?? "").Trim(),
                            SerialNumber = (GetProp(obj, "SerialNumber") ?? "").Trim()
                        });
                    }
                }
            }
            catch { }
        }

        private static void CollectDisks(SystemAuditReport report)
        {
            // Physical drives
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        int idx = 0;
                        int.TryParse(GetProp(obj, "Index"), out idx);
                        ulong size = 0;
                        ulong.TryParse(GetProp(obj, "Size"), out size);

                        report.PhysicalDisks.Add(new PhysicalDiskInfo
                        {
                            Index = idx,
                            Model = (GetProp(obj, "Model") ?? "Hard Disk").Trim(),
                            InterfaceType = GetProp(obj, "InterfaceType") ?? "SATA/NVMe",
                            SizeGb = Math.Round((double)size / (1024.0 * 1024.0 * 1024.0), 1),
                            SerialNumber = (GetProp(obj, "SerialNumber") ?? "").Trim(),
                            MediaType = GetProp(obj, "MediaType") ?? "Fixed hard disk"
                        });
                    }
                }
            }
            catch { }

            // Logical partitions
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo d in drives)
                {
                    if (!d.IsReady) continue;
                    try
                    {
                        double totalGb = Math.Round((double)d.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                        double freeGb = Math.Round((double)d.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
                        double freePct = totalGb > 0 ? Math.Round((freeGb / totalGb) * 100.0, 1) : 0;

                        report.LogicalDrives.Add(new LogicalDiskInfo
                        {
                            DriveLetter = d.Name,
                            VolumeName = string.IsNullOrEmpty(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel,
                            FileSystem = d.DriveFormat,
                            TotalSizeGb = totalGb,
                            FreeSpaceGb = freeGb,
                            FreePercent = freePct
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void CollectGpu(SystemAuditReport report)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        long ram = 0;
                        long.TryParse(GetProp(obj, "AdapterRAM"), out ram);

                        report.Gpus.Add(new GpuInfo
                        {
                            Name = (GetProp(obj, "Name") ?? "Generic Video Controller").Trim(),
                            AdapterRamMb = ram / (1024 * 1024),
                            DriverVersion = GetProp(obj, "DriverVersion") ?? "",
                            CurrentResolution = string.Format("{0} x {1} @ {2}Hz",
                                GetProp(obj, "CurrentHorizontalResolution") ?? "1920",
                                GetProp(obj, "CurrentVerticalResolution") ?? "1080",
                                GetProp(obj, "CurrentRefreshRate") ?? "60")
                        });
                    }
                }
            }
            catch { }
        }

        private static void CollectNetworkAdapters(SystemAuditReport report)
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    IPInterfaceProperties props = ni.GetIPProperties();
                    List<string> ips = new List<string>();
                    foreach (UnicastIPAddressInformation u in props.UnicastAddresses)
                    {
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                            ips.Add(u.Address.ToString());
                    }

                    string gw = "";
                    foreach (GatewayIPAddressInformation g in props.GatewayAddresses)
                    {
                        if (g.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            gw = g.Address.ToString();
                            break;
                        }
                    }

                    PhysicalAddress mac = ni.GetPhysicalAddress();
                    string macStr = mac != null ? string.Join(":", Regex.Split(mac.ToString(), "(?<=\\G.{2})(?!$)")) : "";

                    report.NetworkAdapters.Add(new NetworkDetailInfo
                    {
                        Name = ni.Name + " (" + ni.Description + ")",
                        MacAddress = macStr,
                        IpAddresses = string.Join(", ", ips.ToArray()),
                        DefaultGateway = gw,
                        Status = ni.OperationalStatus.ToString(),
                        SpeedMbps = ni.Speed > 0 ? (ni.Speed / 1000000) : 0
                    });
                }
            }
            catch { }
        }

        private static void CollectPrinters(SystemAuditReport report)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        bool isDef = false, isNet = false;
                        bool.TryParse(GetProp(obj, "Default"), out isDef);
                        bool.TryParse(GetProp(obj, "Network"), out isNet);

                        report.Printers.Add(new PrinterInfo
                        {
                            Name = GetProp(obj, "Name") ?? "Unknown Printer",
                            DriverName = GetProp(obj, "DriverName") ?? "",
                            PortName = GetProp(obj, "PortName") ?? "",
                            IsDefault = isDef,
                            IsNetwork = isNet,
                            Status = GetProp(obj, "Status") ?? "OK"
                        });
                    }
                }
            }
            catch { }
        }

        private static void CollectPeripherals(SystemAuditReport report)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, Manufacturer, DeviceID, Status, Description, PNPClass FROM Win32_PnPEntity WHERE Status = 'OK' AND Name IS NOT NULL"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = (GetProp(obj, "Name") ?? "").Trim();
                        string pnpClass = (GetProp(obj, "PNPClass") ?? "").Trim();
                        string devId = (GetProp(obj, "DeviceID") ?? "").Trim();
                        string mfg = (GetProp(obj, "Manufacturer") ?? "").Trim();
                        string desc = (GetProp(obj, "Description") ?? "").Trim();

                        if (string.IsNullOrEmpty(name)) continue;

                        string category = null;

                        // 1. Máy quét / Scan / Camera
                        if (pnpClass.Equals("Image", StringComparison.OrdinalIgnoreCase) ||
                            name.IndexOf("Scanner", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("ScanSnap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Webcam", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            category = "📷 Máy Quét / Camera (Imaging)";
                        }
                        // 2. Máy chấm công / Máy vân tay / Sinh trắc học
                        else if (pnpClass.Equals("Biometric", StringComparison.OrdinalIgnoreCase) ||
                                 name.IndexOf("Fingerprint", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("Biometric", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("Vân tay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("TouchChip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("Validity", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 name.IndexOf("Synaptics FP", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            category = "👆 Máy Vân Tay / Sinh Trắc (Biometric)";
                        }
                        // 3. Máy in
                        else if (pnpClass.Equals("Printer", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("PrintQueue", StringComparison.OrdinalIgnoreCase))
                        {
                            category = "🖨️ Máy In (Printer)";
                        }
                        // 4. Thiết bị ngoại vi USB kết nối
                        else if (devId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
                                 devId.StartsWith("USBSTOR\\", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("USB", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("HIDClass", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("Mouse", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("Keyboard", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("Ports", StringComparison.OrdinalIgnoreCase))
                        {
                            // Filter out generic internal controllers to keep list clean
                            if (name.IndexOf("Root Hub", StringComparison.OrdinalIgnoreCase) < 0 &&
                                name.IndexOf("Host Controller", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                category = "🔌 Thiết Bị Ngoại Vi USB / PnP";
                            }
                        }
                        // 5. Audio devices
                        else if (pnpClass.Equals("Media", StringComparison.OrdinalIgnoreCase) ||
                                 pnpClass.Equals("AudioEndpoint", StringComparison.OrdinalIgnoreCase))
                        {
                            category = "🎧 Thiết Bị Âm Thanh (Audio)";
                        }

                        if (category != null)
                        {
                            report.Peripherals.Add(new PeripheralDevice
                            {
                                Name = name,
                                Category = category,
                                Manufacturer = string.IsNullOrEmpty(mfg) ? "Standard PnP Device" : mfg,
                                DeviceID = devId,
                                Status = GetProp(obj, "Status") ?? "OK",
                                Description = desc
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private static void CollectInstalledSoftware(SystemAuditReport report)
        {
            Dictionary<string, InstalledSoftwareInfo> softDict = new Dictionary<string, InstalledSoftwareInfo>(StringComparer.OrdinalIgnoreCase);

            // Read HKLM 64-bit & 32-bit & HKCU
            ReadUninstallRegistry(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", softDict);
            ReadUninstallRegistry(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32), @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", softDict);
            ReadUninstallRegistry(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default), @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", softDict);

            // Classify software licenses
            foreach (var item in softDict.Values)
            {
                ClassifySoftwareLicense(item);
                report.InstalledSoftware.Add(item);
            }

            // Sort: Suspected cracks first, then Commercial, then Freeware/Standard
            report.InstalledSoftware.Sort((a, b) =>
            {
                int catComp = a.LicenseCategory.CompareTo(b.LicenseCategory);
                if (catComp != 0)
                {
                    // Sort order: SuspectedCrack (1) > Commercial (2) > Freeware (3) > Standard (0)
                    int rankA = a.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack ? 0 :
                                a.LicenseCategory == SoftwareLicenseCategory.Commercial ? 1 :
                                a.LicenseCategory == SoftwareLicenseCategory.Freeware ? 2 : 3;
                    int rankB = b.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack ? 0 :
                                b.LicenseCategory == SoftwareLicenseCategory.Commercial ? 1 :
                                b.LicenseCategory == SoftwareLicenseCategory.Freeware ? 2 : 3;
                    return rankA.CompareTo(rankB);
                }
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static void ReadUninstallRegistry(RegistryKey root, string subKey, Dictionary<string, InstalledSoftwareInfo> dict)
        {
            if (root == null) return;
            try
            {
                using (RegistryKey uKey = root.OpenSubKey(subKey))
                {
                    if (uKey == null) return;
                    foreach (string name in uKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey appKey = uKey.OpenSubKey(name))
                            {
                                if (appKey == null) continue;

                                string displayName = appKey.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(displayName)) continue;
                                displayName = displayName.Trim();

                                // Filter out Windows hotfixes and language packs
                                if (displayName.StartsWith("KB", StringComparison.OrdinalIgnoreCase) && displayName.Length > 2 && char.IsDigit(displayName[2]))
                                    continue;

                                int sysComponent = 0;
                                object scVal = appKey.GetValue("SystemComponent");
                                if (scVal is int) sysComponent = (int)scVal;
                                if (sysComponent == 1) continue;

                                string parentKey = appKey.GetValue("ParentKeyName") as string;
                                if (!string.IsNullOrEmpty(parentKey)) continue;

                                if (!dict.ContainsKey(displayName))
                                {
                                    string ver = (appKey.GetValue("DisplayVersion") as string ?? "").Trim();
                                    string pub = (appKey.GetValue("Publisher") as string ?? "").Trim();
                                    string date = (appKey.GetValue("InstallDate") as string ?? "").Trim();
                                    string loc = (appKey.GetValue("InstallLocation") as string ?? "").Trim();

                                    if (!string.IsNullOrEmpty(date) && date.Length == 8 && char.IsDigit(date[0]))
                                    {
                                        date = string.Format("{0}/{1}/{2}", date.Substring(6, 2), date.Substring(4, 2), date.Substring(0, 4));
                                    }

                                    dict[displayName] = new InstalledSoftwareInfo
                                    {
                                        DisplayName = displayName,
                                        DisplayVersion = ver,
                                        Publisher = pub,
                                        InstallDate = date,
                                        InstallLocation = loc
                                    };
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void ClassifySoftwareLicense(InstalledSoftwareInfo sw)
        {
            string name = sw.DisplayName.ToLowerInvariant();
            string pub = (sw.Publisher ?? "").ToLowerInvariant();

            // 1. Crack / Activator / Keygen Detection (Highest Priority)
            string[] crackPatterns = new string[]
            {
                "kmspico", "kmsauto", "heu kms", "aact", "autokms", "microsoft-activation-scripts",
                "mas 1.", "mas 2.", "crack", "keygen", "patcher", "patch ", "activator", "repack",
                "pre-activated", "unlocked", "cracked", "daz loader", "kms-vl-all", "idm trial reset",
                "genp", "ccmaker", "cheathappens", "cheathappens", "hack", "trainer"
            };

            foreach (string p in crackPatterns)
            {
                if (name.Contains(p))
                {
                    sw.LicenseCategory = SoftwareLicenseCategory.SuspectedCrack;
                    sw.CrackNotes = "⚠️ Phát hiện công cụ kích hoạt / bản phần mềm bẻ khóa (Từ khóa: '" + p + "')";
                    return;
                }
            }

            // 2. Commercial / Paid Software Detection
            string[] commercialPatterns = new string[]
            {
                "microsoft office", "office 365", "microsoft 365", "adobe photoshop", "adobe illustrator",
                "adobe premiere", "adobe after effects", "adobe acrobat pro", "adobe creative cloud",
                "autodesk", "autocad", "coreldraw", "solidworks", "matlab", "vmware workstation",
                "sketchup", "lumion", "3ds max", "maya", "cyberlink", "vegas pro", "camtasia", "snagit"
            };

            foreach (string p in commercialPatterns)
            {
                if (name.Contains(p))
                {
                    sw.LicenseCategory = SoftwareLicenseCategory.Commercial;
                    sw.CrackNotes = "💼 Phần mềm thương mại có bản quyền";
                    return;
                }
            }

            // 3. Known Freeware / Open Source
            string[] freewarePatterns = new string[]
            {
                "google chrome", "mozilla firefox", "7-zip", "vlc media player", "notepad++", "winrar",
                "libreoffice", "git", "python", "visual studio code", "vs code", "zoom", "zalo", "unikey",
                "acrobat reader", "teamviewer", "anydesk", "lancontrol", "brave", "edge", "telegram",
                "audacity", "gimp", "inkscape", "blender", "obs studio", "filezilla", "cpu-z", "gpu-z"
            };

            foreach (string p in freewarePatterns)
            {
                if (name.Contains(p))
                {
                    sw.LicenseCategory = SoftwareLicenseCategory.Freeware;
                    sw.CrackNotes = "🆓 Phần mềm miễn phí / Mã nguồn mở";
                    return;
                }
            }

            // Default
            sw.LicenseCategory = SoftwareLicenseCategory.Standard;
            sw.CrackNotes = "ℹ️ Ứng dụng tiêu chuẩn";
        }

        private static string GetProp(ManagementObject obj, string propName)
        {
            try
            {
                object val = obj[propName];
                return val != null ? val.ToString() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
