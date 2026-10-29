using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    public class FileTransferHandler
    {
        private class TransferSession
        {
            public string TransferId { get; set; }
            public string FinalPath { get; set; }
            public string PartPath { get; set; }
            public FileStream Stream { get; set; }
            public long TotalBytes { get; set; }
            public long BytesReceived { get; set; }
            public bool ExecuteAfterTransfer { get; set; }
            public string ExecuteArguments { get; set; }
            public string FileName { get; set; }
            public DateTime StartTime { get; set; }
        }

        private readonly AgentInfo _agentInfo;
        private readonly NetworkChannel _channel;
        private readonly Dictionary<string, TransferSession> _activeSessions = new Dictionary<string, TransferSession>();
        private readonly object _sessionLock = new object();

        public FileTransferHandler(AgentInfo agentInfo, NetworkChannel channel)
        {
            _agentInfo = agentInfo;
            _channel = channel;
        }

        public void ProcessFileTransferRequest(FileTransferRequest req)
        {
            if (req == null) return;

            try
            {
                switch (req.Action)
                {
                    case FileTransferAction.UploadToAgent:
                        HandleUploadRequest(req);
                        break;

                    case FileTransferAction.DownloadFromAgent:
                        HandleDownloadRequest(req);
                        break;

                    case FileTransferAction.DeleteFile:
                        HandleDeleteRequest(req);
                        break;

                    case FileTransferAction.CreateDirectory:
                        HandleCreateDirectoryRequest(req);
                        break;

                    case FileTransferAction.ExecuteFile:
                        HandleExecuteRequest(req);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("FileTransferHandler error: {0}", ex.Message);
                SendProgressResponse(req.TransferId, req.FileName, 0, req.FileSize, false, ex.Message, "");
            }
        }

        private void HandleUploadRequest(FileTransferRequest req)
        {
            string targetFolder = ResolveTargetFolder(req.RemotePath);
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string finalPath = Path.Combine(targetFolder, req.FileName);
            string partPath = finalPath + "." + req.TransferId + ".part";

            lock (_sessionLock)
            {
                if (_activeSessions.ContainsKey(req.TransferId))
                {
                    try { _activeSessions[req.TransferId].Stream.Dispose(); } catch { }
                    _activeSessions.Remove(req.TransferId);
                }

                if (File.Exists(partPath))
                {
                    try { File.Delete(partPath); } catch { }
                }

                FileStream fs = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                TransferSession session = new TransferSession
                {
                    TransferId = req.TransferId,
                    FinalPath = finalPath,
                    PartPath = partPath,
                    Stream = fs,
                    TotalBytes = req.FileSize,
                    BytesReceived = 0,
                    ExecuteAfterTransfer = req.ExecuteAfterTransfer,
                    ExecuteArguments = req.ExecuteArguments,
                    FileName = req.FileName,
                    StartTime = DateTime.Now
                };

                _activeSessions[req.TransferId] = session;
            }

            SendProgressResponse(req.TransferId, req.FileName, 0, req.FileSize, true, "", finalPath);
        }

        public void ProcessFileChunk(Packet packet)
        {
            FileChunkMeta meta = packet.GetPayload<FileChunkMeta>();
            if (meta == null) return;

            TransferSession session = null;
            lock (_sessionLock)
            {
                _activeSessions.TryGetValue(meta.TransferId, out session);
            }

            if (session == null)
            {
                Logger.Warn("Chunk received for unknown transfer session: {0}", meta.TransferId);
                return;
            }

            try
            {
                if (packet.RawBytes != null && packet.RawBytes.Length > 0)
                {
                    session.Stream.Write(packet.RawBytes, 0, packet.RawBytes.Length);
                    session.BytesReceived += packet.RawBytes.Length;
                }

                if (meta.IsLastChunk)
                {
                    lock (_sessionLock)
                    {
                        try { session.Stream.Flush(); } catch { }
                        try { session.Stream.Dispose(); } catch { }
                        _activeSessions.Remove(meta.TransferId);
                    }

                    if (File.Exists(session.FinalPath))
                    {
                        try { File.Delete(session.FinalPath); } catch { }
                    }

                    File.Move(session.PartPath, session.FinalPath);

                    // Execute if requested
                    if (session.ExecuteAfterTransfer)
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo(session.FinalPath)
                            {
                                UseShellExecute = true
                            };
                            if (!string.IsNullOrEmpty(session.ExecuteArguments))
                            {
                                psi.Arguments = session.ExecuteArguments;
                            }
                            Process.Start(psi);
                            Logger.Info("Executed file after transfer: {0} {1}", session.FinalPath, session.ExecuteArguments ?? "");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn("Could not execute transferred file {0}: {1}", session.FinalPath, ex.Message);
                        }
                    }

                    SendProgressResponse(session.TransferId, session.FileName, session.BytesReceived, session.TotalBytes, true, "", session.FinalPath);
                }
                else
                {
                    // Report progress periodically (every 5% or 500KB)
                    int percent = session.TotalBytes > 0 ? (int)((session.BytesReceived * 100) / session.TotalBytes) : 0;
                    if (meta.ChunkIndex % 5 == 0 || percent == 100)
                    {
                        SendProgressResponse(session.TransferId, session.FileName, session.BytesReceived, session.TotalBytes, true, "", session.FinalPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error writing file chunk: {0}", ex.Message);
                lock (_sessionLock)
                {
                    try { session.Stream.Dispose(); } catch { }
                    _activeSessions.Remove(meta.TransferId);
                }
                SendProgressResponse(meta.TransferId, session.FileName, session.BytesReceived, session.TotalBytes, false, ex.Message, session.FinalPath);
            }
        }

        private void HandleDownloadRequest(FileTransferRequest req)
        {
            if (!File.Exists(req.RemotePath))
            {
                SendProgressResponse(req.TransferId, req.FileName, 0, 0, false, "Tập tin không tồn tại trên máy trạm: " + req.RemotePath, "");
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    FileInfo fi = new FileInfo(req.RemotePath);
                    long totalSize = fi.Length;
                    int chunkSize = 256 * 1024; // 256 KB
                    int totalChunks = totalSize == 0 ? 1 : (int)Math.Ceiling((double)totalSize / chunkSize);

                    using (FileStream fs = new FileStream(req.RemotePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        byte[] buffer = new byte[chunkSize];
                        long offset = 0;

                        for (int i = 0; i < totalChunks; i++)
                        {
                            int read = fs.Read(buffer, 0, chunkSize);
                            bool isLast = (i == totalChunks - 1) || read <= 0;

                            byte[] chunkData = new byte[read];
                            if (read > 0)
                            {
                                Buffer.BlockCopy(buffer, 0, chunkData, 0, read);
                            }

                            FileChunkMeta meta = new FileChunkMeta
                            {
                                TransferId = req.TransferId,
                                AgentId = _agentInfo.AgentId,
                                ChunkIndex = i,
                                TotalChunks = totalChunks,
                                Offset = offset,
                                Length = read,
                                IsLastChunk = isLast
                            };

                            Packet chunkPacket = Packet.Create(PacketType.FileTransferChunk, meta, chunkData);
                            _channel.Send(chunkPacket);

                            offset += read;

                            if (isLast) break;
                            Thread.Sleep(5); // Throttle slightly to prevent network buffer overflow
                        }
                    }

                    SendProgressResponse(req.TransferId, req.FileName, totalSize, totalSize, true, "", req.RemotePath);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error streaming file download: {0}", ex.Message);
                    SendProgressResponse(req.TransferId, req.FileName, 0, 0, false, ex.Message, req.RemotePath);
                }
            });
        }

        private void HandleDeleteRequest(FileTransferRequest req)
        {
            try
            {
                if (File.Exists(req.RemotePath))
                {
                    File.Delete(req.RemotePath);
                    SendProgressResponse(req.TransferId, req.FileName, 0, 0, true, "Đã xóa tệp tin thành công.", req.RemotePath);
                }
                else if (Directory.Exists(req.RemotePath))
                {
                    Directory.Delete(req.RemotePath, true);
                    SendProgressResponse(req.TransferId, req.FileName, 0, 0, true, "Đã xóa thư mục thành công.", req.RemotePath);
                }
                else
                {
                    SendProgressResponse(req.TransferId, req.FileName, 0, 0, false, "Đường dẫn không tồn tại trên máy trạm.", req.RemotePath);
                }
            }
            catch (Exception ex)
            {
                SendProgressResponse(req.TransferId, req.FileName, 0, 0, false, "Lỗi khi xóa: " + ex.Message, req.RemotePath);
            }
        }

        private void HandleCreateDirectoryRequest(FileTransferRequest req)
        {
            try
            {
                Directory.CreateDirectory(req.RemotePath);
                SendProgressResponse(req.TransferId, req.FileName, 0, 0, true, "Đã tạo thư mục thành công.", req.RemotePath);
            }
            catch (Exception ex)
            {
                SendProgressResponse(req.TransferId, req.FileName, 0, 0, false, "Lỗi khi tạo thư mục: " + ex.Message, req.RemotePath);
            }
        }

        private void HandleExecuteRequest(FileTransferRequest req)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(req.RemotePath)
                {
                    UseShellExecute = true
                };
                if (!string.IsNullOrEmpty(req.ExecuteArguments))
                {
                    psi.Arguments = req.ExecuteArguments;
                }
                Process.Start(psi);
                SendProgressResponse(req.TransferId, req.FileName, 0, 0, true, "Đã khởi chạy tệp từ xa thành công.", req.RemotePath);
            }
            catch (Exception ex)
            {
                SendProgressResponse(req.TransferId, req.FileName, 0, 0, false, "Lỗi khởi chạy: " + ex.Message, req.RemotePath);
            }
        }

        public void ProcessFileListRequest(RemoteDirectoryListRequest req)
        {
            if (req == null) return;

            RemoteDirectoryListResponse resp = new RemoteDirectoryListResponse
            {
                AgentId = _agentInfo.AgentId
            };

            try
            {
                // Populate drives
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (d.IsReady)
                    {
                        resp.Drives.Add(string.Format("{0} ({1} - {2} GB trống)", d.Name, d.VolumeLabel, d.TotalFreeSpace / (1024 * 1024 * 1024)));
                    }
                    else
                    {
                        resp.Drives.Add(d.Name);
                    }
                }

                string targetPath = req.Path;
                if (string.IsNullOrEmpty(targetPath))
                {
                    // Default to C:\ or Desktop
                    targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                targetPath = ResolveTargetFolder(targetPath);

                if (Directory.Exists(targetPath))
                {
                    DirectoryInfo di = new DirectoryInfo(targetPath);
                    resp.CurrentPath = di.FullName;
                    resp.ParentPath = di.Parent != null ? di.Parent.FullName : null;

                    // Add Subdirectories
                    foreach (DirectoryInfo sub in di.GetDirectories())
                    {
                        try
                        {
                            resp.Items.Add(new RemoteFileSystemItem
                            {
                                Name = sub.Name,
                                FullPath = sub.FullName,
                                IsDirectory = true,
                                Size = 0,
                                LastModified = sub.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                                Extension = "<Folder>"
                            });
                        }
                        catch { }
                    }

                    // Add Files
                    foreach (FileInfo f in di.GetFiles())
                    {
                        try
                        {
                            resp.Items.Add(new RemoteFileSystemItem
                            {
                                Name = f.Name,
                                FullPath = f.FullName,
                                IsDirectory = false,
                                Size = f.Length,
                                LastModified = f.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                                Extension = f.Extension
                            });
                        }
                        catch { }
                    }
                }
                else
                {
                    resp.ErrorMessage = "Thư mục không tồn tại: " + targetPath;
                }
            }
            catch (Exception ex)
            {
                resp.ErrorMessage = ex.Message;
            }

            Packet respPacket = Packet.Create(PacketType.FileListResponse, resp);
            _channel.Send(respPacket);
        }

        private void SendProgressResponse(string transferId, string fileName, long transferred, long total, bool success, string error, string savedPath)
        {
            int percent = total > 0 ? (int)((transferred * 100) / total) : (success ? 100 : 0);
            if (percent > 100) percent = 100;

            FileTransferProgressResponse resp = new FileTransferProgressResponse
            {
                TransferId = transferId,
                AgentId = _agentInfo.AgentId,
                Hostname = _agentInfo.Hostname,
                FileName = fileName,
                BytesTransferred = transferred,
                TotalBytes = total,
                Percentage = percent,
                Success = success,
                ErrorMessage = error,
                SavedPath = savedPath
            };

            Packet p = Packet.Create(PacketType.FileTransferResponse, resp);
            _channel.Send(p);
        }

        public static string ResolveTargetFolder(string inputPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            string p = inputPath.Trim();

            if (p.Equals("%Desktop%", StringComparison.OrdinalIgnoreCase) || p.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            if (p.Equals("%Downloads%", StringComparison.OrdinalIgnoreCase) || p.Equals("Downloads", StringComparison.OrdinalIgnoreCase))
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userProfile, "Downloads");
            }

            if (p.Equals("%Temp%", StringComparison.OrdinalIgnoreCase) || p.Equals("Temp", StringComparison.OrdinalIgnoreCase))
            {
                return @"C:\Temp";
            }

            if (p.Contains("%"))
            {
                p = Environment.ExpandEnvironmentVariables(p);
            }

            return p;
        }
    }
}
