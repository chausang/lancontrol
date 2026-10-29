using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using LanControl.Core.Models;

namespace LanControl.Core.Utils
{
    public class SubnetScanner
    {
        public event Action<ScanHostResult> HostScanned;
        public event Action<int, int> ProgressChanged; // (scannedCount, totalCount)

        public List<ScanHostResult> Scan(ScanConfig config)
        {
            List<IPAddress> targetIps = GenerateIpList(config);
            List<int> ports = ParsePorts(config.PortList);
            List<ScanHostResult> results = new List<ScanHostResult>();

            int total = targetIps.Count;
            int scanned = 0;
            object syncLock = new object();

            int concurrency = config.MaxConcurrency > 0 ? config.MaxConcurrency : 50;
            using (Semaphore semaphore = new Semaphore(concurrency, concurrency))
            {
                List<Thread> threads = new List<Thread>();

                foreach (IPAddress ip in targetIps)
                {
                    semaphore.WaitOne();

                    IPAddress currentIp = ip;
                    Thread t = new Thread(new ThreadStart(delegate()
                    {
                        try
                        {
                            ScanHostResult hostResult = ProbeHost(currentIp, ports, config.TimeoutMs);
                            if (hostResult.IsOnline)
                            {
                                lock (syncLock)
                                {
                                    results.Add(hostResult);
                                }
                            }

                            if (HostScanned != null)
                            {
                                HostScanned(hostResult);
                            }
                        }
                        catch { }
                        finally
                        {
                            int currentScanned = Interlocked.Increment(ref scanned);
                            if (ProgressChanged != null)
                            {
                                ProgressChanged(currentScanned, total);
                            }
                            semaphore.Release();
                        }
                    }))
                    {
                        IsBackground = true
                    };

                    threads.Add(t);
                    t.Start();
                }

                foreach (Thread t in threads)
                {
                    t.Join();
                }
            }

            return results;
        }

        private ScanHostResult ProbeHost(IPAddress ip, List<int> ports, int timeoutMs)
        {
            ScanHostResult result = new ScanHostResult
            {
                IpAddress = ip.ToString(),
                IsOnline = false,
                PingMs = -1,
                HasAgent = false
            };

            // 1. Ping check
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(ip, timeoutMs);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        result.IsOnline = true;
                        result.PingMs = reply.RoundtripTime;
                    }
                }
            }
            catch { }

            // 2. Port check (even if ICMP ping is blocked by Windows Firewall, TCP port might be open)
            List<string> openPortsList = new List<string>();
            foreach (int port in ports)
            {
                if (CheckPort(ip, port, timeoutMs))
                {
                    result.IsOnline = true;
                    openPortsList.Add(port.ToString());
                    if (port == 8443)
                    {
                        result.HasAgent = true;
                    }
                }
            }

            if (openPortsList.Count > 0)
            {
                result.OpenPorts = string.Join(", ", openPortsList.ToArray());
            }

            // 3. DNS Hostname resolution
            if (result.IsOnline)
            {
                try
                {
                    IPHostEntry entry = Dns.GetHostEntry(ip);
                    if (entry != null && !string.IsNullOrEmpty(entry.HostName))
                    {
                        result.Hostname = entry.HostName;
                    }
                }
                catch
                {
                    result.Hostname = "Unknown";
                }
            }

            return result;
        }

        private bool CheckPort(IPAddress ip, int port, int timeoutMs)
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    IAsyncResult asyncResult = socket.BeginConnect(ip, port, null, null);
                    bool success = asyncResult.AsyncWaitHandle.WaitOne(timeoutMs, true);
                    if (success && socket.Connected)
                    {
                        socket.EndConnect(asyncResult);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static List<IPAddress> GenerateIpList(ScanConfig config)
        {
            List<IPAddress> list = new List<IPAddress>();

            if (!string.IsNullOrEmpty(config.Cidr))
            {
                string[] parts = config.Cidr.Trim().Split('/');
                if (parts.Length == 2)
                {
                    IPAddress baseIp;
                    int cidrBits;
                    if (IPAddress.TryParse(parts[0], out baseIp) && int.TryParse(parts[1], out cidrBits))
                    {
                        uint ipUint = IpToUint(baseIp);
                        uint mask = ~(uint.MaxValue >> cidrBits);
                        uint netStart = ipUint & mask;
                        uint netEnd = netStart | ~mask;

                        for (uint i = netStart + 1; i < netEnd; i++)
                        {
                            list.Add(UintToIp(i));
                        }
                        return list;
                    }
                }
            }

            IPAddress startIp, endIp;
            if (IPAddress.TryParse(config.StartIp, out startIp) && IPAddress.TryParse(config.EndIp, out endIp))
            {
                uint startUint = IpToUint(startIp);
                uint endUint = IpToUint(endIp);

                if (startUint > endUint)
                {
                    uint temp = startUint;
                    startUint = endUint;
                    endUint = temp;
                }

                // Prevent accidentally scanning millions of IPs
                uint count = endUint - startUint + 1;
                if (count > 2048) count = 2048;

                for (uint i = 0; i < count; i++)
                {
                    list.Add(UintToIp(startUint + i));
                }
            }

            return list;
        }

        private static List<int> ParsePorts(string portStr)
        {
            List<int> ports = new List<int>();
            if (string.IsNullOrEmpty(portStr))
            {
                ports.Add(8443);
                return ports;
            }

            string[] tokens = portStr.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in tokens)
            {
                int p;
                if (int.TryParse(t.Trim(), out p) && p > 0 && p <= 65535)
                {
                    if (!ports.Contains(p)) ports.Add(p);
                }
            }

            if (ports.Count == 0) ports.Add(8443);
            return ports;
        }

        private static uint IpToUint(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static IPAddress UintToIp(uint val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            Array.Reverse(bytes);
            return new IPAddress(bytes);
        }
    }
}
