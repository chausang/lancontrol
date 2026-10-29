using System;
using System.Collections.Generic;

namespace LanControl.Core.Models
{
    [Serializable]
    public class OperatingSystemInfo
    {
        public string OsName { get; set; }
        public string Version { get; set; }
        public string BuildNumber { get; set; }
        public string Architecture { get; set; }
        public string InstallDate { get; set; }
        public string RegisteredUser { get; set; }
        public string ComputerName { get; set; }
        public string DomainOrWorkgroup { get; set; }

        // License & Activation
        public string WindowsActivationStatus { get; set; }
        public string WindowsLicenseChannel { get; set; }
        public string PartialProductKey { get; set; }
        public bool IsSuspectedCrack { get; set; }
        public string CrackWarningDetails { get; set; }
    }

    [Serializable]
    public class CpuInfo
    {
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public int Cores { get; set; }
        public int LogicalProcessors { get; set; }
        public uint MaxClockSpeedMhz { get; set; }
        public string Architecture { get; set; }
        public string ProcessorId { get; set; }
    }

    [Serializable]
    public class MotherboardInfo
    {
        public string Manufacturer { get; set; }
        public string Product { get; set; }
        public string SerialNumber { get; set; }
        public string BiosVendor { get; set; }
        public string BiosVersion { get; set; }
        public string BiosReleaseDate { get; set; }
    }

    [Serializable]
    public class RamStickInfo
    {
        public string DeviceLocator { get; set; }
        public long CapacityMb { get; set; }
        public uint SpeedMhz { get; set; }
        public string Manufacturer { get; set; }
        public string PartNumber { get; set; }
        public string SerialNumber { get; set; }
    }

    [Serializable]
    public class PhysicalDiskInfo
    {
        public int Index { get; set; }
        public string Model { get; set; }
        public string InterfaceType { get; set; }
        public double SizeGb { get; set; }
        public string SerialNumber { get; set; }
        public string MediaType { get; set; }
    }

    [Serializable]
    public class LogicalDiskInfo
    {
        public string DriveLetter { get; set; }
        public string VolumeName { get; set; }
        public string FileSystem { get; set; }
        public double TotalSizeGb { get; set; }
        public double FreeSpaceGb { get; set; }
        public double FreePercent { get; set; }
    }

    [Serializable]
    public class GpuInfo
    {
        public string Name { get; set; }
        public long AdapterRamMb { get; set; }
        public string DriverVersion { get; set; }
        public string CurrentResolution { get; set; }
    }

    [Serializable]
    public class NetworkDetailInfo
    {
        public string Name { get; set; }
        public string MacAddress { get; set; }
        public string IpAddresses { get; set; }
        public string DefaultGateway { get; set; }
        public string Status { get; set; }
        public long SpeedMbps { get; set; }
    }

    [Serializable]
    public class PrinterInfo
    {
        public string Name { get; set; }
        public string DriverName { get; set; }
        public string PortName { get; set; }
        public bool IsDefault { get; set; }
        public bool IsNetwork { get; set; }
        public string Status { get; set; }
    }

    [Serializable]
    public class PeripheralDevice
    {
        public string Name { get; set; }
        public string Category { get; set; } // Máy In, Máy Scan, Máy Vân Tay, USB, Audio, etc.
        public string Manufacturer { get; set; }
        public string DeviceID { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }

    public enum SoftwareLicenseCategory
    {
        Standard = 0,
        SuspectedCrack = 1,
        Commercial = 2,
        Freeware = 3
    }

    [Serializable]
    public class InstalledSoftwareInfo
    {
        public string DisplayName { get; set; }
        public string DisplayVersion { get; set; }
        public string Publisher { get; set; }
        public string InstallDate { get; set; }
        public string InstallLocation { get; set; }
        public SoftwareLicenseCategory LicenseCategory { get; set; }
        public string CrackNotes { get; set; }
    }

    [Serializable]
    public class SystemAuditReport
    {
        public string AgentId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string VlanTag { get; set; }
        public string CollectedTime { get; set; }

        public OperatingSystemInfo OS { get; set; }
        public CpuInfo CPU { get; set; }
        public MotherboardInfo Motherboard { get; set; }
        public long TotalRamMb { get; set; }
        public List<RamStickInfo> RamSticks { get; set; }
        public List<PhysicalDiskInfo> PhysicalDisks { get; set; }
        public List<LogicalDiskInfo> LogicalDrives { get; set; }
        public List<GpuInfo> Gpus { get; set; }
        public List<NetworkDetailInfo> NetworkAdapters { get; set; }
        public List<PrinterInfo> Printers { get; set; }
        public List<PeripheralDevice> Peripherals { get; set; }
        public List<InstalledSoftwareInfo> InstalledSoftware { get; set; }

        public int TotalSoftwareCount { get; set; }
        public int CrackCount { get; set; }
        public int CommercialCount { get; set; }
        public int FreewareCount { get; set; }

        public SystemAuditReport()
        {
            OS = new OperatingSystemInfo();
            CPU = new CpuInfo();
            Motherboard = new MotherboardInfo();
            RamSticks = new List<RamStickInfo>();
            PhysicalDisks = new List<PhysicalDiskInfo>();
            LogicalDrives = new List<LogicalDiskInfo>();
            Gpus = new List<GpuInfo>();
            NetworkAdapters = new List<NetworkDetailInfo>();
            Printers = new List<PrinterInfo>();
            Peripherals = new List<PeripheralDevice>();
            InstalledSoftware = new List<InstalledSoftwareInfo>();
        }
    }
}
